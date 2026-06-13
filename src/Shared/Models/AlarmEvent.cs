namespace Shared.Models;

public class AlarmEvent
{
    public Guid Id { get; set; }
    public Guid SensorId { get; set; }
    public double Value { get; set; }
    public int Priority { get; set; }
    public DateTime Timestamp { get; set; }

    public Sensor Sensor { get; set; } = null!;
}
