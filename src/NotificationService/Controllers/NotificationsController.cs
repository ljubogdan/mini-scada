using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using Shared.DTOs;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/alarms")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<NotificationsHub> _hub;

    public NotificationsController(IHubContext<NotificationsHub> hub)
    {
        _hub = hub;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAlarm([FromBody] AlarmNotificationDto dto)
    {
        await _hub.Clients.All.SendAsync(
            "AlarmReceived",
             dto.SensorId,
             dto.Value,
             dto.Priority
        );

        return Ok();
    }
}