using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace IngestionService.Services;

public class SensorMonitorService(IServiceScopeFactory scopeFactory, ILogger<SensorMonitorService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckSensors();
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckSensors()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScadaDbContext>();

        var now = DateTime.UtcNow;
        var sensors = await db.Sensors.ToListAsync();

        foreach (var sensor in sensors)
        {
            if (sensor.IsBlocked && sensor.BlockedUntil.HasValue && now >= sensor.BlockedUntil.Value)
            {
                sensor.IsBlocked = false;
                sensor.BlockedUntil = null;
                logger.LogInformation("Sensor {Name} unblocked.", sensor.Name);
            }

            if (!sensor.IsBlocked && sensor.LastSeenAt.HasValue)
            {
                var inactive = now - sensor.LastSeenAt.Value > InactivityTimeout;
                if (inactive && sensor.IsActive)
                {
                    sensor.IsActive = false;
                    logger.LogWarning("Sensor {Name} marked inactive (no message for 10s).", sensor.Name);
                }
                else if (!inactive && !sensor.IsActive)
                {
                    sensor.IsActive = true;
                    logger.LogInformation("Sensor {Name} is active again.", sensor.Name);
                }
            }
        }

        await db.SaveChangesAsync();

        var activeCount = sensors.Count(s => s.IsActive && !s.IsBlocked);
        if (activeCount < 5)
            logger.LogWarning("Active sensor count is {Count}/5.", activeCount);
    }
}
