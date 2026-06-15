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
        try
        {
            var sensor = await db.Sensors.FindAsync(message.SensorId);
            if (sensor is null)
            {
                logger.LogWarning("Sensor not found: {SensorId}", message.SensorId);
                return NotFound("Sensor not found.");
            }

            if (sensor.IsBlocked)
            {
                logger.LogWarning("Blocked sensor attempted request: {SensorId}", sensor.Id);
                return StatusCode(403, "Sensor is blocked.");
            }

            if (rateLimiter.Exceeded(sensor.Id))
            {
                sensor.IsBlocked = true;
                sensor.BlockedUntil = DateTime.UtcNow.AddSeconds(30);
                await db.SaveChangesAsync();

                logger.LogWarning("RATE LIMIT: Sensor {SensorId} blocked for 30s", sensor.Id);
                return StatusCode(429, "Sensor exceeded rate limit.");
            }

            if (string.IsNullOrEmpty(sensor.PublicKey))
            {
                logger.LogError("Sensor {SensorId} missing public key", sensor.Id);
                return BadRequest("Sensor has no public key.");
            }

            var isValid = CryptoService.RsaVerify(message.EncryptedPayload, message.Signature, sensor.PublicKey);
            if (!isValid)
            {
                logger.LogWarning("INVALID SIGNATURE from sensor {SensorId}", sensor.Id);
                return StatusCode(401, "Invalid signature.");
            }

            var aesKeyString = config["AesKey"];

            if (string.IsNullOrEmpty(aesKeyString))
            {
                logger.LogCritical("AES key is missing in configuration!");
                return StatusCode(500, "Server misconfigured.");
            }

            var aesKey = Convert.FromBase64String(aesKeyString);

            logger.LogDebug("AES key length = {Length}", aesKey.Length);

            SensorReadingPayload payload;

            try
            {
                payload = CryptoService.AesDecrypt<SensorReadingPayload>(
                    message.EncryptedPayload,
                    message.IV,
                    aesKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AES decryption failed for sensor {SensorId}", sensor.Id);
                return StatusCode(500, "Decryption failed.");
            }

            if (!antiReplay.Validate(sensor.Id, payload.MessageId, payload.Timestamp, out var reason))
            {
                logger.LogWarning("Replay attack detected: {Reason} Sensor={SensorId}", reason, sensor.Id);
                return StatusCode(409, reason);
            }

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

            logger.LogInformation(
                "OK Sensor={SensorId} Value={Value} Priority={Priority}",
                sensor.Id, payload.Value, priority);

            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in Ingest endpoint");
            return StatusCode(500, "Internal server error");
        }
    }

    private static int CalculateAlarmPriority(Sensor sensor, double value)
    {
        if (sensor.AlarmThreshold3.HasValue && value >= sensor.AlarmThreshold3.Value) return 3;
        if (sensor.AlarmThreshold2.HasValue && value >= sensor.AlarmThreshold2.Value) return 2;
        if (sensor.AlarmThreshold1.HasValue && value >= sensor.AlarmThreshold1.Value) return 1;
        return 0;
    }
}
