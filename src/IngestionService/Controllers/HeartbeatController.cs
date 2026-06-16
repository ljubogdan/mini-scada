using Microsoft.AspNetCore.Mvc;
using Shared.Data;
using Shared.DTOs;
using Shared.Models;


[ApiController]
[Route("api/heartbeat")]
public class HeartbeatController(ScadaDbContext db, ILogger<HeartbeatController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] HeartbeatDto dto)
    {
        var sensor = await db.Sensors.FindAsync(dto.SensorId);
        if (sensor is null)
            return NotFound("Sensor not found.");

        db.Heartbeats.Add(new Heartbeat
        {
            SensorId = sensor.Id,
            Timestamp = DateTime.UtcNow
        });

        sensor.LastSeenAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var isActive = !sensor.IsBlocked && sensor.IsActive;

        logger.LogInformation("Heartbeat from {SensorId}, IsActive={IsActive}", sensor.Id, isActive);

        return Ok(new { IsActive = isActive });
    }
}