public sealed class EvaluationRuleset
{
    public int riskSoftThreshold = 60;
    public int riskHardThreshold = 80;

    public int promiseStage2 = 2;
    public int promiseStage4 = 4;
    public int promiseStage6 = 6;

    public int contractZoneTargetDelta = 10; // 기본: 방송당 +10 목표

    public EvaluationResult Evaluate(BroadcastSaveState state, BroadcastEventLog log, BroadcastScoreDelta deltas)
    {
        // 1) 계약 달성 여부(예: ZoneDelta가 목표 이상이면 달성)
        bool contractMet = deltas.zone >= contractZoneTargetDelta;

        // 2) Risk 임계 여부
        bool riskSoft = state.risk >= riskSoftThreshold;
        bool riskHard = state.risk >= riskHardThreshold;

        int breachDelta = 0;
        int graceDelta = 0;

        // 계약 미달이면 유예 소모
        if (!contractMet)
        {
            if (state.graceRemaining > 0)
            {
                state.graceRemaining -= 1;
                graceDelta = -1;
            }
            else
            {
                state.breachCount += 1;
                breachDelta = +1;
            }
        }

        // Risk hard면 breach로 치지 말고 “Strong SoftFail”로 가는 게 톤에 맞음
        // (원하면 breachDelta += 1로 올려도 되지만, P0에선 잠금/불길 노드로 처리 추천)

        // grade 결정
        EvalGrade grade;
        if (riskHard || state.breachCount >= 2)
            grade = EvalGrade.Critical;
        else if (riskSoft || breachDelta > 0)
            grade = EvalGrade.Breach;
        else if (!contractMet)
            grade = EvalGrade.AtRisk;
        else
            grade = EvalGrade.Success;

        // SoftFail 메커니즘: grade에 따라 잠금/불길 플래그를 박는다
        var locksAdded = LockFlags.None;
        if (grade == EvalGrade.Breach)
        {
            // Soft: 선택지/기능 일부를 흔들어준다
            locksAdded |= LockFlags.OneChoiceSealed;
            state.gloom += 1;
        }
        else if (grade == EvalGrade.Critical)
        {
            // Strong SoftFail: 불길 노드 주입 + 확정 잠금
            locksAdded |= LockFlags.NightEventDarker;
            locksAdded |= LockFlags.VoteLocked;
            locksAdded |= LockFlags.OneChoiceSealed;
            state.gloom += 2;
        }

        // PromiseDebt 단계가 높으면 추가 압박(잠금/계약 강화)
        if (state.promiseDebt >= promiseStage6)
        {
            locksAdded |= LockFlags.DmSuppressed; // “DM이 달콤하게 막히는” 방향도 가능
            state.gloom += 1;
        }

        // locks 적용
        state.locks |= locksAdded;

        string note = BuildNoteText(grade);

        return new EvaluationResult(contractMet, riskSoft || riskHard, breachDelta, graceDelta, grade, locksAdded, note);
    }

    private static string BuildNoteText(EvalGrade grade)
    {
        switch (grade)
        {
            case EvalGrade.Success: return "좋았어. 다음 방송도… 기대해도 되지?";
            case EvalGrade.AtRisk:  return "괜찮아. 이번엔 넘어가줄게. (다음엔 지켜줘.)";
            case EvalGrade.Breach:  return "약속이 어긋났어. 다음 방송은 더 불길해질 거야.";
            case EvalGrade.Critical:return "이제 숨길 수 없어. 다음 방송은 규칙이 바뀌어.";
            default: return string.Empty;
        }
    }
}

public readonly struct EvaluationResult
{
    public readonly bool contractMet;
    public readonly bool riskThresholdHit;
    public readonly int breachDelta;
    public readonly int graceDelta;
    public readonly EvalGrade grade;
    public readonly LockFlags locksAdded;
    public readonly string noteText;

    public EvaluationResult(bool contractMet, bool riskHit, int breachDelta, int graceDelta, EvalGrade grade, LockFlags locksAdded, string noteText)
    {
        this.contractMet = contractMet;
        this.riskThresholdHit = riskHit;
        this.breachDelta = breachDelta;
        this.graceDelta = graceDelta;
        this.grade = grade;
        this.locksAdded = locksAdded;
        this.noteText = noteText;
    }
}
