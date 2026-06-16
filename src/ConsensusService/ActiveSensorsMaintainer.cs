using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace ConsensusService
{
    internal class ActiveSensorsMaintainer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ActiveSensorsMaintainer> _logger;

        public ActiveSensorsMaintainer(IServiceProvider serviceProvider, ILogger<ActiveSensorsMaintainer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                
                try
                {
                    await MaintainMinimumActiveSensorsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking sensor activity.");
                }
            }
        }

        private async Task MaintainMinimumActiveSensorsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ScadaDbContext>();

            int activeSensorCount = await GetActiveSensorCountAsync(dbContext, stoppingToken);

            if (activeSensorCount < 5)
            {
                await ActivateFallbackSensorAsync(dbContext, stoppingToken);
            }
        }

        private async Task<int> GetActiveSensorCountAsync(ScadaDbContext dbContext, CancellationToken stoppingToken)
        {
            return await dbContext.Sensors
                .CountAsync(s => s.IsActive && !s.IsBlocked && (s.BlockedUntil == null || s.BlockedUntil < DateTime.UtcNow), stoppingToken);
        }

        private async Task ActivateFallbackSensorAsync(ScadaDbContext dbContext, CancellationToken stoppingToken)
        {
            var tenSecondsAgo = DateTime.UtcNow.AddSeconds(-10);

            var sensors = await dbContext.Sensors
                .Where(s => s.LastSeenAt != null && s.LastSeenAt >= tenSecondsAgo)
                .ToListAsync(stoppingToken);

            if (sensors.Count == 0) return;

            UnblockExpiredSensors(sensors);

            var candidateSensors = sensors
                .Where(s => !s.IsActive && !s.IsBlocked)
                .ToList();

            if (candidateSensors.Count > 0)
            {
                ActivateSensor(candidateSensors);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }

        private void UnblockExpiredSensors(List<Sensor> sensors)
        {
            foreach (var s in sensors)
            {
                if (s.IsBlocked && s.BlockedUntil != null && s.BlockedUntil < DateTime.UtcNow)
                {
                    s.IsBlocked = false;
                    s.BlockedUntil = null;
                }
            }
        }

        private void ActivateSensor(List<Sensor> candidateSensors)
        {
            var selectedSensor = candidateSensors.FirstOrDefault(s => s.Quality == DataQuality.Good);
            
            if (selectedSensor == null)
            {
                var random = new Random();
                selectedSensor = candidateSensors[random.Next(candidateSensors.Count)];
            }

            selectedSensor.IsActive = true;
            _logger.LogInformation("Activated sensor {SensorId} to maintain minimum active count.", selectedSensor.Id);
        }
    }
}
