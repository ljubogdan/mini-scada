using Microsoft.AspNetCore.Mvc;
using Shared.Data;
using Shared.Models;

namespace IngestionService.Controllers;

[ApiController]
[Route("api/sensors")]
public class SensorsController(ScadaDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterSensorDto dto)
    {
        var existing = await db.Sensors.FindAsync(dto.Id);
        if (existing is not null)
            return Ok(existing.Id);

        var sensor = new Sensor
        {
            Id = dto.Id,
            Name = dto.Name,
            MinRange = dto.MinRange,
            MaxRange = dto.MaxRange,
            Quality = dto.Quality,
            AlarmThreshold1 = dto.AlarmThreshold1,
            AlarmThreshold2 = dto.AlarmThreshold2,
            AlarmThreshold3 = dto.AlarmThreshold3,
            IsActive = true,
            IsBlocked = false,
            LastSeenAt = DateTime.UtcNow
        };

        db.Sensors.Add(sensor);
        await db.SaveChangesAsync();

        return Ok(sensor.Id);
    }
}

public class RegisterSensorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double MinRange { get; set; }
    public double MaxRange { get; set; }
    public Shared.Models.DataQuality Quality { get; set; }
    public double? AlarmThreshold1 { get; set; }
    public double? AlarmThreshold2 { get; set; }
    public double? AlarmThreshold3 { get; set; }
}
