public sealed class BroadcastEventSession
{
    public readonly string RunId;
    public readonly string EventId;
    public readonly int EventIndex;

    public readonly IBroadcastEventRecorder Recorder;

    public bool IsEnded { get; private set; }

    public BroadcastEventSession(string runId, string eventId, int eventIndex, IBroadcastEventRecorder recorder)
    {
        RunId = runId;
        EventId = eventId;
        EventIndex = eventIndex;
        Recorder = recorder;
    }

    public void Begin(double nowSec)
    {
        Recorder.BeginEvent(RunId, EventId, EventIndex, nowSec);
        IsEnded = false;
    }

    public BroadcastEventLog End(double nowSec)
    {
        if (IsEnded) return Recorder.BuildLog();
        Recorder.EndEvent(nowSec);
        IsEnded = true;
        return Recorder.BuildLog();
    }
}