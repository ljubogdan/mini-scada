namespace Shared.DTOs;

public class SensorReadingDto
{
    public Guid SensorId { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public long MessageId { get; set; }
}
