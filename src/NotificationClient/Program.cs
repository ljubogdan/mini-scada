using Microsoft.AspNetCore.SignalR.Client;

var hubUrl = "http://localhost:5003/hubs/alarms";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();


connection.On<Guid, double, int>("AlarmReceived", (sensorId, value, priority) =>
{
    switch (priority)
    {
        case 1:
            Console.ForegroundColor = ConsoleColor.Yellow;
            break;

        case 2:
            Console.ForegroundColor = ConsoleColor.DarkYellow; // “narandžasta”
            break;

        case 3:
            Console.ForegroundColor = ConsoleColor.Red;
            break;

        default:
            Console.ForegroundColor = ConsoleColor.Gray;
            break;
    }

    Console.WriteLine($"ALARM | Sensor={sensorId} | Value={value} | Priority={priority}");
    Console.ResetColor();
});


connection.On<string>("TestMessage", msg =>
{
    Console.WriteLine($"{msg}");
});

connection.Closed += async (error) =>
{
    Console.WriteLine("Disconnected. Reconnecting...");
    await Task.Delay(2000);
    await connection.StartAsync();
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Connected to Notification Hub");
    Console.WriteLine("Listening for alarms...");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
    return;
}

await Task.Delay(-1);