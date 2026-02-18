public interface IIdolReactor
{
    IdolReaction React(ChatTag tag, string optionId);
}

public interface IBroadcastLogRepository
{
    void Add(BroadcastEventLog log);

    // 최소 조회 (원하면 나중에 확장)
    int Count { get; }
    BroadcastEventLog GetLastOrNull();
}