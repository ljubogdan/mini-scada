namespace Shared.Models;

public class Sensor
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double MinRange { get; set; }
    public double MaxRange { get; set; }
    public DataQuality Quality { get; set; }
    public double? AlarmThreshold1 { get; set; }
    public double? AlarmThreshold2 { get; set; }
    public double? AlarmThreshold3 { get; set; }
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime? BlockedUntil { get; set; }
    public DateTime? LastSeenAt { get; set; }

    public ICollection<Measurement> Measurements { get; set; } = [];
    public ICollection<AlarmEvent> AlarmEvents { get; set; } = [];
}
