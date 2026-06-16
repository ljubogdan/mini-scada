using System.Collections.Concurrent;

namespace IngestionService.Services;

public class AntiReplayService
{
    private readonly ConcurrentDictionary<Guid, long> _lastMessageIds = new();
    private static readonly TimeSpan MaxMessageAge = TimeSpan.FromSeconds(120);

    public bool Validate(Guid sensorId, long messageId, DateTime timestamp, out string reason)
    {
        if (DateTime.UtcNow - timestamp > MaxMessageAge)
        {
            reason = "Message is too old (replay attack).";
            return false;
        }

        var lastId = _lastMessageIds.GetOrAdd(sensorId, -1);
        if (messageId <= lastId)
        {
            reason = $"MessageId {messageId} already seen (replay attack).";
            return false;
        }

        _lastMessageIds[sensorId] = messageId;
        reason = string.Empty;
        return true;
    }
}
