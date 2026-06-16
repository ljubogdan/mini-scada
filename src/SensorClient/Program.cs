using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SensorClient;
using Shared.DTOs;
using Shared.Models;
using Shared.Security;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddCommandLine(args)
    .Build();

var serverUrl = config["ServerUrl"]!;
var aesKey = Convert.FromBase64String(config["AesKey"]!);
var sensorConfig = config.GetSection("Sensor").Get<SensorConfig>()!;

var http = new HttpClient { BaseAddress = new Uri(serverUrl) };
var random = new Random();
long messageId = 0;

bool isActive = true;
CancellationTokenSource? sendingCts = new CancellationTokenSource();

using var rsa = RSA.Create(2048);
var publicKeyPem = rsa.ExportRSAPublicKeyPem();

await RegisterSensor();

Console.WriteLine($"Sensor '{sensorConfig.Name}' started. Sending data to {serverUrl}");
Console.WriteLine("Press ENTER to simulate replay attack (resend last message).");

double lastValue = 0;
long lastMessageId = 0;

_ = Task.Run(async () =>
{
    while (true)
    {
        Console.ReadLine();
        Console.WriteLine($"[REPLAY ATTACK] Resending MessageId={lastMessageId}...");
        await SendReading(lastValue, replayMessageId: lastMessageId);
    }
});

_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(10000);
        var newIsActive = await SendHeartbeat();
        if (newIsActive != isActive)
        {
            isActive = newIsActive;
            if (isActive)
            {
                sendingCts = new CancellationTokenSource();
                _ = Task.Run(() => SendingLoop(sendingCts.Token));
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {sensorConfig.Name} | ACTIVE - resumed sending.");
            }
            else
            {
                sendingCts?.Cancel();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {sensorConfig.Name} | INACTIVE - waiting for activation...");
                Console.ResetColor();
            }
        }
    }
});

_ = Task.Run(() => SendingLoop(sendingCts.Token));

await Task.Delay(-1);

async Task SendingLoop(CancellationToken token)
{
    try
    {
        while (!token.IsCancellationRequested)
        {
            var value = Math.Round(
                random.NextDouble() * (sensorConfig.MaxRange - sensorConfig.MinRange)
                + sensorConfig.MinRange, 2);
            var priority = CalculatePriority(value);

            lastValue = value;
            PrintReading(value, priority);
            await SendReading(value, cancellationToken: token);
            lastMessageId = messageId;

            var delay = random.Next(1000, 10001);
            await Task.Delay(delay, token);
        }
    }
    catch (TaskCanceledException)
    {
        // Expected cancellation
    }
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
        sensorConfig.AlarmThreshold3,
        PublicKey = publicKeyPem
    };

    var response = await http.PostAsJsonAsync("/api/sensors/register", payload);
    response.EnsureSuccessStatusCode();
    Console.WriteLine("Registered with server.");
}

async Task SendReading(double value, long? replayMessageId = null, System.Threading.CancellationToken cancellationToken = default)
{
    var innerPayload = new SensorReadingPayload
    {
        Value = value,
        Timestamp = DateTime.UtcNow,
        MessageId = replayMessageId ?? ++messageId
    };

    var (cipherText, iv) = CryptoService.AesEncrypt(innerPayload, aesKey);
    var signature = CryptoService.RsaSign(cipherText, rsa);

    var message = new SecureMessageDto
    {
        SensorId = sensorConfig.Id,
        EncryptedPayload = cipherText,
        IV = iv,
        Signature = signature
    };

    try
    {
        var response = await http.PostAsJsonAsync("/api/ingest", message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"Server returned: {response.StatusCode}");
    }
    catch (TaskCanceledException)
    {
        throw;
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

async Task<bool> SendHeartbeat()
{
    try
    {
        var response = await http.PostAsJsonAsync("/api/heartbeat", new HeartbeatDto
        {
            SensorId = sensorConfig.Id
        });
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<HeartbeatResponseDto>();
            return result?.IsActive ?? true;
        }
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Heartbeat failed: {ex.Message}");
        return true;
    }
}
