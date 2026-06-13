using IngestionService.Interfaces;

namespace IngestionService.Services;

public class StubNotificationService(ILogger<StubNotificationService> logger) : INotificationService
{
    public Task SendAlarmAsync(Guid sensorId, double value, int priority)
    {
        logger.LogInformation("ALARM | SensorId={SensorId} Value={Value} Priority={Priority}", sensorId, value, priority);
        return Task.CompletedTask;
    }
}
