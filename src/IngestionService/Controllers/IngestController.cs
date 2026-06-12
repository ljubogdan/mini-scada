using IngestionService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.DTOs;
using Shared.Models;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/ingest")]
public class IngestController(ScadaDbContext db, INotificationService notifications, ILogger<IngestController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] SensorReadingDto dto)
    {
        var sensor = await db.Sensors.FindAsync(dto.SensorId);
        if (sensor is null)
            return NotFound("Sensor not found.");

        if (sensor.IsBlocked)
            return StatusCode(403, "Sensor is blocked.");

        sensor.LastSeenAt = DateTime.UtcNow;

        var priority = CalculateAlarmPriority(sensor, dto.Value);

        var measurement = new Measurement
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            Value = dto.Value,
            Timestamp = dto.Timestamp,
            Quality = sensor.Quality,
            AlarmPriority = priority,
            IsConsensus = false
        };

        db.Measurements.Add(measurement);

        if (priority > 0)
        {
            db.AlarmEvents.Add(new AlarmEvent
            {
                Id = Guid.NewGuid(),
                SensorId = sensor.Id,
                Value = dto.Value,
                Priority = priority,
                Timestamp = dto.Timestamp
            });

            await notifications.SendAlarmAsync(sensor.Id, dto.Value, priority);
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Sensor={SensorId} Value={Value} Priority={Priority}", sensor.Id, dto.Value, priority);

        return Ok();
    }

    private static int CalculateAlarmPriority(Sensor sensor, double value)
    {
        if (sensor.AlarmThreshold3.HasValue && value >= sensor.AlarmThreshold3.Value) return 3;
        if (sensor.AlarmThreshold2.HasValue && value >= sensor.AlarmThreshold2.Value) return 2;
        if (sensor.AlarmThreshold1.HasValue && value >= sensor.AlarmThreshold1.Value) return 1;
        return 0;
    }
}
