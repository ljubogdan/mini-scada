using IngestionService.Interfaces;
using Shared.DTOs;

namespace IngestionService.Services;

public class NotificationServiceClient : INotificationService
{
    private readonly HttpClient _http;

    public NotificationServiceClient(HttpClient http)
    {
        _http = http;
    }

    public async Task SendAlarmAsync(Guid sensorId, double value, int priority)
    {
        var dto = new AlarmNotificationDto
        {
            SensorId = sensorId,
            Value = value,
            Priority = priority,
            Timestamp = DateTime.UtcNow
        };

        var response =  await _http.PostAsJsonAsync("api/alarms", dto);

        response.EnsureSuccessStatusCode();
    }
}
