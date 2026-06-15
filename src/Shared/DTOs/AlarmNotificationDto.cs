namespace Shared.DTOs;

public class AlarmNotificationDto
{
    public Guid SensorId {get; set;}
    public double Value {get; set;}
    public int Priority {get; set;}
    public DateTime Timestamp {get; set;}
}