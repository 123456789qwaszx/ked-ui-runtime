using System.Collections.Generic;

public sealed class InMemoryBroadcastStateRepository
{
    private readonly Dictionary<string, BroadcastSaveState> _map = new Dictionary<string, BroadcastSaveState>();

    public BroadcastSaveState LoadOrCreate(string runId)
    {
        if (string.IsNullOrEmpty(runId))
            runId = "run_default";

        if (_map.TryGetValue(runId, out var state))
            return state;

        state = new BroadcastSaveState
        {
            runId = runId,
            eventIndex = 0,
            zoneScore = 50,
            risk = 20,
            promiseDebt = 0,
            breachCount = 0,
            graceRemaining = 1,
            unlocks = UnlockFlags.None,
            locks = LockFlags.None,
            tokens = new List<TokenState>(16),
            gloom = 0
        };

        _map.Add(runId, state);
        return state;
    }

    public void Save(BroadcastSaveState state)
    {
        if (state == null)
            return;
        
        _map[state.runId] = state;
    }
}