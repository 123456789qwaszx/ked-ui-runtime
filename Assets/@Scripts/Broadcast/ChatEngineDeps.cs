using System;

public readonly struct ChatEngineDeps
{
    public readonly InMemoryBroadcastStore store;
    public readonly BroadcastEventLogRecorder recorder;
    public readonly SimpleIdolReactor idolReactor;
    public readonly BroadcastEndPipeline endPipeline;

    // (선택) 결과를 UI에 전달하고 싶으면 콜백
    public readonly Action<BroadcastEndResult> onEventEnded;

    public ChatEngineDeps(
        InMemoryBroadcastStore store,
        BroadcastEventLogRecorder recorder,
        SimpleIdolReactor idolReactor,
        BroadcastEndPipeline endPipeline,
        Action<BroadcastEndResult> onEventEnded)
    {
        this.store = store;
        this.recorder = recorder;
        this.idolReactor = idolReactor;
        this.endPipeline = endPipeline;
        this.onEventEnded = onEventEnded;
    }
}