namespace SensorClient;

public class SensorConfig
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double MinRange { get; set; }
    public double MaxRange { get; set; }
    public string Quality { get; set; } = "Good";
    public double? AlarmThreshold1 { get; set; }
    public double? AlarmThreshold2 { get; set; }
    public double? AlarmThreshold3 { get; set; }
}
