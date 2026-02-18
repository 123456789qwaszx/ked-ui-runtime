using System.Collections.Generic;

public sealed class InMemoryBroadcastLogRepository
{
    private readonly List<BroadcastEventLog> _logs = new List<BroadcastEventLog>(capacity: 8);

    public int Count => _logs.Count;

    public void Add(BroadcastEventLog log)
    {
        if (log == null) return;
        _logs.Add(log);
    }

    public BroadcastEventLog GetLastOrNull()
    {
        if (_logs.Count <= 0) return null;
        return _logs[_logs.Count - 1];
    }
}