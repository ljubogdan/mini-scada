namespace Shared.DTOs;

public class SensorReadingPayload
{
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public long MessageId { get; set; }
}
