using Shared.Data;
using Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsensusService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            try
            {
                await CalculateConsensusAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while computing consensus.");
            }
        }
    }

    private async Task CalculateConsensusAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ScadaDbContext>();

        var windowEnd = DateTime.UtcNow;
        var windowStart = windowEnd.AddMinutes(-1);

        var activeSensors = await GetActiveSensorsAsync(dbContext, stoppingToken);
        if (activeSensors.Count == 0) return;

        var sensorIds = activeSensors.Select(s => s.Id).ToList();

        var recentMeasurements = await GetRecentMeasurementsAsync(dbContext, sensorIds, windowStart, windowEnd, stoppingToken);
        if (recentMeasurements.Count == 0) return;

        var sensorAverages = ComputeSensorAverages(recentMeasurements);

        var excludedSensorIds = new List<Guid>();
        await EliminateOutliersAsync(dbContext, activeSensors, sensorAverages, excludedSensorIds, stoppingToken);

        var consensusValue = ComputeFinalConsensus(sensorAverages);
        if (consensusValue is null) return;

        var values = sensorAverages.Values.ToList();
        var median = CalculateMedian(values);
        var stdDev = CalculateStdDev(values, median);

        _logger.LogInformation(
            "Consensus value: {Value} based on {Count} sensor(s). Excluded: {Excluded}.",
            consensusValue, sensorAverages.Count, excludedSensorIds.Count);

        var consensus = new Consensus
        {
            Value = consensusValue.Value,
            Timestamp = windowEnd,
            ParticipantSensorCount = sensorAverages.Count,
            ExcludedSensorCount = excludedSensorIds.Count,
            ParticipantSensorIds = sensorAverages.Keys.ToList(),
            ExcludedSensorIds = excludedSensorIds,
            StandardDeviation = stdDev,
            Median = median,
            WindowStart = windowStart,
            WindowEnd = windowEnd
        };

        await SaveConsensusAsync(dbContext, consensus, stoppingToken);
    }

    private async Task<List<Sensor>> GetActiveSensorsAsync(ScadaDbContext dbContext, CancellationToken stoppingToken)
    {
        var sensors = await dbContext.Sensors
            .Where(s => s.IsActive && !s.IsBlocked && s.Quality == DataQuality.Good)
            .ToListAsync(stoppingToken);

        if (sensors.Count == 0)
            _logger.LogWarning("No active sensors with GOOD quality found for consensus calculation.");

        return sensors;
    }

    private async Task<List<Measurement>> GetRecentMeasurementsAsync(ScadaDbContext dbContext, List<Guid> sensorIds, DateTime from, DateTime to, CancellationToken stoppingToken)
    {
        var measurements = await dbContext.Measurements
            .Where(m => sensorIds.Contains(m.SensorId) &&
                        m.Timestamp >= from &&
                        m.Timestamp <= to &&
                        !m.IsConsensus &&
                        m.Quality == DataQuality.Good)
            .ToListAsync(stoppingToken);

        if (measurements.Count == 0)
            _logger.LogInformation("No measurements found in the last minute to calculate consensus.");

        return measurements;
    }

    private static Dictionary<Guid, double> ComputeSensorAverages(List<Measurement> measurements)
    {
        return measurements
            .GroupBy(m => m.SensorId)
            .ToDictionary(g => g.Key, g => g.Average(m => m.Value));
    }

    private async Task EliminateOutliersAsync(
        ScadaDbContext dbContext,
        List<Sensor> activeSensors,
        Dictionary<Guid, double> sensorAverages,
        List<Guid> excludedSensorIds,
        CancellationToken stoppingToken)
    {
        List<Guid> outliersFound;

        do
        {
            var values = sensorAverages.Values.ToList();
            var median = CalculateMedian(values);
            var stdDev = CalculateStdDev(values, median);

            outliersFound = FindOutliers(sensorAverages, median, stdDev);
            if (outliersFound.Count == 0) break;

            _logger.LogWarning("Found {Count} Byzantine sensor(s), marking as BAD and retrying.", outliersFound.Count);

            await MarkSensorsAsBadAsync(dbContext, activeSensors, sensorAverages, outliersFound, excludedSensorIds, stoppingToken);

            if (sensorAverages.Count == 0)
            {
                _logger.LogWarning("All sensors removed as outliers — cannot compute consensus.");
                return;
            }

        } while (outliersFound.Count > 0);
    }

    private static double? ComputeFinalConsensus(Dictionary<Guid, double> sensorAverages)
    {
        if (sensorAverages.Count == 0) return null;
        return sensorAverages.Values.Average();
    }

    private static List<Guid> FindOutliers(Dictionary<Guid, double> sensorAverages, double median, double stdDev)
    {
        return sensorAverages
            .Where(kv => Math.Abs(kv.Value - median) > 2 * stdDev)
            .Select(kv => kv.Key)
            .ToList();
    }

    private async Task MarkSensorsAsBadAsync(
        ScadaDbContext dbContext,
        List<Sensor> activeSensors,
        Dictionary<Guid, double> sensorAverages,
        List<Guid> outlierIds,
        List<Guid> excludedSensorIds,
        CancellationToken stoppingToken)
    {
        foreach (var sensorId in outlierIds)
        {
            var sensor = activeSensors.First(s => s.Id == sensorId);
            var avg = sensorAverages[sensorId];
            sensor.Quality = DataQuality.Bad;
            sensorAverages.Remove(sensorId);
            excludedSensorIds.Add(sensorId);
            _logger.LogWarning("Sensor {Id} marked as BAD (Byzantine outlier, avg={Avg})", sensorId, avg);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private async Task SaveConsensusAsync(ScadaDbContext dbContext, Consensus consensus, CancellationToken stoppingToken)
    {
        dbContext.Consensuses.Add(consensus);
        await dbContext.SaveChangesAsync(stoppingToken);
    }

    private static double CalculateMedian(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;
        return count % 2 == 0
            ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0
            : sorted[count / 2];
    }

    private static double CalculateStdDev(List<double> values, double mean)
    {
        if (values.Count < 2) return 0;
        var variance = values.Average(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(variance);
    }
}