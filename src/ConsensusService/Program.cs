using Shared.Data;
using Microsoft.EntityFrameworkCore;
using ConsensusService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ConsensusService.ConsensusService>();
builder.Services.AddHostedService<ActiveSensorsMaintainer>();
builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var host = builder.Build();
host.Run();
