using System;

public interface IBroadcastStateRepository
{
    BroadcastSaveState LoadOrCreate(string runId);
    void Save(BroadcastSaveState state);
}

public readonly struct ChatEngineDeps
{
    public readonly IBroadcastLogRepository repository;
    public readonly IBroadcastEventRecorder recorder;
    public readonly IIdolReactor idolReactor;

    public readonly IBroadcastStateRepository stateRepository;
    public readonly BroadcastEndPipeline endPipeline;

    // (선택) 결과를 UI에 전달하고 싶으면 콜백
    public readonly Action<BroadcastEndResult> onEventEnded;
    
    public ChatEngineDeps(
        IBroadcastLogRepository repository,
        IBroadcastEventRecorder recorder,
        IIdolReactor idolReactor,
        IBroadcastStateRepository stateRepository,
        BroadcastEndPipeline endPipeline,
        Action<BroadcastEndResult> onEventEnded)
    {
        this.repository = repository;
        this.recorder = recorder;
        this.idolReactor = idolReactor;

        this.stateRepository = stateRepository;
        this.endPipeline = endPipeline;
        this.onEventEnded = onEventEnded;
    }
}