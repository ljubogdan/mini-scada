using AspNetCoreRateLimit;
using IngestionService.Interfaces;
using IngestionService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddSingleton<AntiReplayService>();
builder.Services.AddHostedService<SensorMonitorService>();

builder.Services.AddHttpClient<INotificationService, NotificationServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["NotificationService:baseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// RATE LIMITING
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting")
);
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<SensorRateLimitService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 1000;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// RATE LIMITING
app.UseIpRateLimiting();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ScadaDbContext>();
    db.Database.Migrate();
}

app.Run();
