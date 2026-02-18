using System;

public readonly struct ChatEngineDeps
{
    public readonly InMemoryBroadcastLogRepository repository;
    public readonly BroadcastEventLogRecorder recorder;
    public readonly SimpleIdolReactor idolReactor;

    public readonly InMemoryBroadcastStateRepository stateRepository;
    public readonly BroadcastEndPipeline endPipeline;

    // (선택) 결과를 UI에 전달하고 싶으면 콜백
    public readonly Action<BroadcastEndResult> onEventEnded;
    
    public ChatEngineDeps(
        InMemoryBroadcastLogRepository repository,
        BroadcastEventLogRecorder recorder,
        SimpleIdolReactor idolReactor,
        InMemoryBroadcastStateRepository stateRepository,
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