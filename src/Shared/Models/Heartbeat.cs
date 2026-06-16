using Shared.Models;

namespace Shared.Models;

public class Heartbeat
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SensorId { get; set; }

    public Sensor? Sensor { get; set; }

    public DateTime Timestamp { get; set; }

    public Boolean IsActive { get; set; }

    public DataQuality Quality { get; set; }

}