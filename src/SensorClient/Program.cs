using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SensorClient;
using Shared.DTOs;
using Shared.Models;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var serverUrl = config["ServerUrl"]!;
var sensorConfig = config.GetSection("Sensor").Get<SensorConfig>()!;

var http = new HttpClient { BaseAddress = new Uri(serverUrl) };
var random = new Random();
long messageId = 0;

await RegisterSensor();

Console.WriteLine($"Sensor '{sensorConfig.Name}' started. Sending data to {serverUrl}");

while (true)
{
    var value = Math.Round(random.NextDouble() * (sensorConfig.MaxRange - sensorConfig.MinRange) + sensorConfig.MinRange, 2);
    var priority = CalculatePriority(value);

    PrintReading(value, priority);
    await SendReading(value);

    var delay = random.Next(1000, 10001);
    await Task.Delay(delay);
}

async Task RegisterSensor()
{
    var payload = new
    {
        sensorConfig.Id,
        sensorConfig.Name,
        sensorConfig.MinRange,
        sensorConfig.MaxRange,
        Quality = Enum.Parse<DataQuality>(sensorConfig.Quality),
        sensorConfig.AlarmThreshold1,
        sensorConfig.AlarmThreshold2,
        sensorConfig.AlarmThreshold3
    };

    var response = await http.PostAsJsonAsync("/api/sensors/register", payload);
    response.EnsureSuccessStatusCode();
    Console.WriteLine("Registered with server.");
}

async Task SendReading(double value)
{
    var dto = new SensorReadingDto
    {
        SensorId = sensorConfig.Id,
        Value = value,
        Timestamp = DateTime.UtcNow,
        MessageId = ++messageId
    };

    try
    {
        var response = await http.PostAsJsonAsync("/api/ingest", dto);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"Server returned: {response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to send: {ex.Message}");
    }
}

int CalculatePriority(double value)
{
    if (sensorConfig.AlarmThreshold3.HasValue && value >= sensorConfig.AlarmThreshold3.Value) return 3;
    if (sensorConfig.AlarmThreshold2.HasValue && value >= sensorConfig.AlarmThreshold2.Value) return 2;
    if (sensorConfig.AlarmThreshold1.HasValue && value >= sensorConfig.AlarmThreshold1.Value) return 1;
    return 0;
}

void PrintReading(double value, int priority)
{
    var timestamp = DateTime.Now.ToString("HH:mm:ss");
    Console.ForegroundColor = priority switch
    {
        1 => ConsoleColor.Yellow,
        2 => ConsoleColor.DarkYellow,
        3 => ConsoleColor.Red,
        _ => ConsoleColor.Gray
    };
    Console.WriteLine($"[{timestamp}] {sensorConfig.Name} | Temp: {value}°C | Alarm: {(priority == 0 ? "None" : $"P{priority}")}");
    Console.ResetColor();
}
