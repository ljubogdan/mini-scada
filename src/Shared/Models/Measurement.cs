namespace Shared.Models;

public class Measurement
{
    public Guid Id { get; set; }
    public Guid SensorId { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public DataQuality Quality { get; set; }
    public int AlarmPriority { get; set; }
    public bool IsConsensus { get; set; }

    public Sensor Sensor { get; set; } = null!;
}
