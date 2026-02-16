public interface IIdolReactor
{
    IdolReaction React(ChatTag tag, string optionId);
}

public interface IBroadcastEventRecorder
{
    void BeginEvent(string runId, string eventId, int eventIndex, double startedAtSec);
    void EndEvent(double endedAtSec);

    void BeginPhase(int phaseIndex, string phaseId, string profileKeyAtEnter, double startedAtSec);
    void EndPhase(double endedAtSec);

    void RecordDonation(int amount);
    void RecordEmoji(int emojiId);

    void RecordChat(ChatTag tag, IdolReaction reaction, string optionId);

    void RecordDecision(PhaseDecisionKind kind, string optionId, bool accepted);
    
    BroadcastEventLog BuildLog(); // EndEvent 후 결과물 뽑기
}

public interface IBroadcastLogRepository
{
    void Add(BroadcastEventLog log);
    // 나중에 조회 API 확장
}