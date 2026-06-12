namespace IngestionService.Interfaces;

public interface INotificationService
{
    Task SendAlarmAsync(Guid sensorId, double value, int priority);
}
