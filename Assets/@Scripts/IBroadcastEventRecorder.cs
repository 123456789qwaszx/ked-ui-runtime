using System;

public interface IIdolReactor
{
    IdolReaction React(ChatTag tag, string optionId);
}

public interface IBroadcastEventRecorder
{
    // R-01 / R-02
    void BeginEvent(string runId, string eventId, int eventIndex, double startedAtSec);
    void EndEvent(double endedAtSec);

    // R-03 / R-04
    void BeginPhase(int phaseIndex, string phaseId, string profileKeyAtEnter, double startedAtSec);
    void EndPhase(double endedAtSec);

    // R-05 / R-06 / R-07
    void RecordDonation(int amount);
    void RecordEmoji(int emojiId);
    void RecordChat(ChatTag tag, string optionId, IdolReaction reaction);

    // R-08
    void RecordDecision(PhaseDecisionKind kind, string optionId, bool accepted);

    // 결과물
    BroadcastEventLog BuildLog();
    bool IsEventActive { get; }
    bool IsPhaseActive { get; }
}

public interface IBroadcastLogRepository
{
    void Add(BroadcastEventLog log);

    // 최소 조회 (원하면 나중에 확장)
    int Count { get; }
    BroadcastEventLog GetLastOrNull();
}