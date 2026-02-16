public readonly struct ChatEngineDeps
{
    public readonly IBroadcastLogRepository repository;
    public readonly IBroadcastEventRecorder recorder;
    public readonly IIdolReactor idolReactor;

    public ChatEngineDeps(
        IBroadcastLogRepository repository,
        IBroadcastEventRecorder recorder,
        IIdolReactor idolReactor)
    {
        this.repository = repository;
        this.recorder = recorder;
        this.idolReactor = idolReactor;
    }
}