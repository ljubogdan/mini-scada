using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Hubs;

public class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("CLIENT CONNECTED");
        await Clients.All.SendAsync("TestMessage", "HELLO FROM SERVER");
        await base.OnConnectedAsync();
    }
}