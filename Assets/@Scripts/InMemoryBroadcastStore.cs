using System.Collections.Generic;

public sealed class InMemoryBroadcastStore
{
    private readonly Dictionary<string, BroadcastSaveState> _states = new Dictionary<string, BroadcastSaveState>();
    private readonly List<BroadcastEventLog> _logs;

    private readonly int _maxLogs;

    public InMemoryBroadcastStore(int maxLogs = 8)
    {
        _maxLogs = maxLogs <= 0 ? 8 : maxLogs;
        _logs = new List<BroadcastEventLog>(capacity: _maxLogs);
    }

    // ---- State ----
    public BroadcastSaveState LoadOrCreateState(string runId)
    {
        if (string.IsNullOrEmpty(runId))
            runId = "run_default";

        if (_states.TryGetValue(runId, out var state))
            return state;

        state = BroadcastSaveState.CreateNew(runId);
        _states.Add(runId, state);
        return state;
    }

    public void SaveState(BroadcastSaveState state)
    {
        if (state == null) return;
        if (string.IsNullOrEmpty(state.runId))
            state.runId = "run_default";

        _states[state.runId] = state;
    }

    // ---- Logs ----
    public int LogCount => _logs.Count;

    public void AppendLog(BroadcastEventLog log)
    {
        if (log == null) return;

        if (_logs.Count >= _maxLogs)
            _logs.RemoveAt(0); // P0: 단순 ring buffer

        _logs.Add(log);
    }

    public BroadcastEventLog GetLastLogOrNull()
    {
        if (_logs.Count <= 0) return null;
        return _logs[_logs.Count - 1];
    }

    public void ClearLogs() => _logs.Clear();
}