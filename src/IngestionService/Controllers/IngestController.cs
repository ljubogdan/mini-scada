using IngestionService.Interfaces;
using IngestionService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.DTOs;
using Shared.Models;
using Shared.Security;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/ingest")]
public class IngestController(ScadaDbContext db, INotificationService notifications, ILogger<IngestController> logger, IConfiguration config, AntiReplayService antiReplay, SensorRateLimitService rateLimiter) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ingest([FromBody] SecureMessageDto message)
    {
        var sensor = await db.Sensors.FindAsync(message.SensorId);
        if (sensor is null)
            return NotFound("Sensor not found.");
        
        if (sensor.IsBlocked)
            return StatusCode(403, "Sensor is blocked.");

        if (rateLimiter.Exceeded(sensor.Id))
        {
            sensor.IsBlocked = true;
            sensor.BlockedUntil = DateTime.UtcNow.AddSeconds(30);
            await db.SaveChangesAsync();

            logger.LogWarning("Sensor {SensorId} blocked due to rate limit.", sensor.Id);

            return StatusCode(429, "Sensor exceeded 10 messages per second.");
        }

        if (string.IsNullOrEmpty(sensor.PublicKey))
            return BadRequest("Sensor has no public key.");

        var isValid = CryptoService.RsaVerify(message.EncryptedPayload, message.Signature, sensor.PublicKey);
        if (!isValid)
            return StatusCode(401, "Invalid signature.");

        var aesKey = Convert.FromBase64String(config["AesKey"]!);
        var payload = CryptoService.AesDecrypt<SensorReadingPayload>(message.EncryptedPayload, message.IV, aesKey);

        if (!antiReplay.Validate(sensor.Id, payload.MessageId, payload.Timestamp, out var reason))
            return StatusCode(409, reason);

        sensor.LastSeenAt = DateTime.UtcNow;

        var priority = CalculateAlarmPriority(sensor, payload.Value);

        var measurement = new Measurement
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            Value = payload.Value,
            Timestamp = payload.Timestamp,
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
                Value = payload.Value,
                Priority = priority,
                Timestamp = payload.Timestamp
            });

            await notifications.SendAlarmAsync(sensor.Id, payload.Value, priority);
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Sensor={SensorId} Value={Value} Priority={Priority}", sensor.Id, payload.Value, priority);

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
