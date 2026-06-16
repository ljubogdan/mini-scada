using Shared.Models;

namespace Shared.Models;

public class Consensus
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public double Value { get; set; }

    public DateTime Timestamp { get; set; }

    public int ParticipantSensorCount { get; set; }

    public int ExcludedSensorCount { get; set; }
    public List<Guid> ParticipantSensorIds { get; set; } = new();

    public List<Guid> ExcludedSensorIds { get; set; } = new();

    public double StandardDeviation { get; set; }

    public double Median { get; set; }

    public DateTime WindowStart { get; set; }

    public DateTime WindowEnd { get; set; }
}
