using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromMemory(
        new[]
        {
            new RouteConfig
            {
                RouteId = "ingest",
                ClusterId = "ingestion",
                Match = new RouteMatch { Path = "/api/ingest/{**catch-all}" }
            },
            new RouteConfig
            {
                RouteId = "sensors",
                ClusterId = "ingestion",
                Match = new RouteMatch { Path = "/api/sensors/{**catch-all}" }
            },
            new RouteConfig
            {
                RouteId = "alarms",
                ClusterId = "notification",
                Match = new RouteMatch { Path = "/api/alarms/{**catch-all}" }
            },
            new RouteConfig
            {
                RouteId = "reports",
                ClusterId = "ingestion",
                Match = new RouteMatch { Path = "/api/reports/{**catch-all}" }
            },
            new RouteConfig
            {
                RouteId = "heartbeat",
                ClusterId = "ingestion",
                Match = new RouteMatch { Path = "/api/heartbeat/{**catch-all}" }
            }
        },
        new[]
        {
            new ClusterConfig
            {
                ClusterId = "ingestion",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "d1", new DestinationConfig { Address = "http://ingestion-service:8080/" } }
                }
            },
            new ClusterConfig
            {
                ClusterId = "notification",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "d1", new DestinationConfig { Address = "http://notification-service:8080/" } }
                }
            }
        }
    );

var app = builder.Build();

app.MapReverseProxy();

app.Run();