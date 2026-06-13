using System.Collections.Concurrent;
using Microsoft.VisualBasic;

namespace IngestionService.Services;

public class SensorRateLimitService
{
    private readonly ConcurrentDictionary<Guid, Queue<DateTime>> _requests = new();

    public bool Exceeded(Guid sensorId)
    {
        var now = DateTime.UtcNow;

        var queue = _requests.GetOrAdd(sensorId, _ => new Queue<DateTime>());

        lock (queue)
        {
            while (queue.Count > 0 && (now - queue.Peek()).TotalSeconds > 1)
            {
                queue.Dequeue();
            }

            queue.Enqueue(now);
            return queue.Count > 10;
        }
    }
}