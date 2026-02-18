using System.Collections.Generic;

public interface INightEventCatalog
{
    // 조건에 맞는 후보를 반환
    void CollectCandidates(BroadcastSaveState state, BroadcastEventLog log, Deltas deltas, EvaluationResult eval, List<NightEventCandidate> outList);
}

public struct NightEventCandidate
{
    public int priority;
    public NightEventPayload payload;

    public NightEventCandidate(int priority, NightEventPayload payload)
    {
        this.priority = priority;
        this.payload = payload;
    }
}

public sealed class DefaultNightEventCatalog : INightEventCatalog
{
    public void CollectCandidates(BroadcastSaveState state, BroadcastEventLog log, Deltas deltas, EvaluationResult eval, List<NightEventCandidate> outList)
    {
        // Critical/Breach 우선
        if (eval.grade == EvalGrade.Critical)
        {
            outList.Add(new NightEventCandidate(
                priority: 100,
                payload: new NightEventPayload
                {
                    kind = NightEventKind.Operator,
                    eventKey = "night_operator_rulechange",
                    titleText = "운영 공지",
                    teaserText = "다음 스테이지 평가 기준이 ‘조정’됐어요! 😊"
                }));
        }

        // 커뮤 불씨/클립
        if ((log.flags & BroadcastFlags.ClipSeeded) != 0 || log.clipSeededCount > 0)
        {
            outList.Add(new NightEventCandidate(
                priority: 80,
                payload: new NightEventPayload
                {
                    kind = NightEventKind.Scandal,
                    eventKey = state.locks.HasFlag(LockFlags.NightEventDarker) ? "night_clip_dark" : "night_clip",
                    titleText = "클립 업로드",
                    teaserText = "하이라이트가 올라왔어! …약속 부분만, 이상하게."
                }));
        }

        // 약속 압박(DM)
        if (state.promiseDebt >= 2 || (log.flags & BroadcastFlags.PromiseAccepted) != 0)
        {
            outList.Add(new NightEventCandidate(
                priority: 60,
                payload: new NightEventPayload
                {
                    kind = NightEventKind.Pressure,
                    eventKey = "night_dm_pressure",
                    titleText = "DM",
                    teaserText = "너만 믿어도 돼? 나… 내일도 웃을 수 있어?"
                }));
        }

        // 보상(성공)
        if (eval.grade == EvalGrade.Success)
        {
            outList.Add(new NightEventCandidate(
                priority: 30,
                payload: new NightEventPayload
                {
                    kind = NightEventKind.Reward,
                    eventKey = "night_reward_clip",
                    titleText = "특별대우",
                    teaserText = "오늘 고마워! 너한테만 보여줄게 😊"
                }));
        }
    }
}

public static class NightEventSelector
{
    public static NightEventPayload SelectOne(INightEventCatalog catalog, BroadcastSaveState state, BroadcastEventLog log, Deltas deltas, EvaluationResult eval)
    {
        var list = new List<NightEventCandidate>(8);
        catalog.CollectCandidates(state, log, deltas, eval, list);

        if (list.Count <= 0)
            return new NightEventPayload { kind = NightEventKind.None, eventKey = "none", titleText = "", teaserText = "" };

        int best = 0;
        int bestPriority = list[0].priority;

        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].priority > bestPriority)
            {
                best = i;
                bestPriority = list[i].priority;
            }
        }

        return list[best].payload;
    }
}
