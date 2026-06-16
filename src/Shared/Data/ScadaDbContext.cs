using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared.Data;

public class ScadaDbContext(DbContextOptions<ScadaDbContext> options) : DbContext(options)
{
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<Measurement> Measurements => Set<Measurement>();
    public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();
    public DbSet<Consensus> Consensuses => Set<Consensus>();
}
