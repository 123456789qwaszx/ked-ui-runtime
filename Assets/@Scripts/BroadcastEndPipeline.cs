using UnityEngine;

public sealed class BroadcastEndPipeline
{
    private readonly BroadcastScoringRules _deltaRules;
    private readonly TokenRuleset _tokenRules;
    private readonly EvaluationRuleset _evalRules;
    private readonly INightEventCatalog _nightCatalog;
    private readonly ContractBuilder _contractBuilder;

    public BroadcastEndPipeline(
        BroadcastScoringRules deltaRules,
        TokenRuleset tokenRules,
        EvaluationRuleset evalRules,
        INightEventCatalog nightCatalog,
        ContractBuilder contractBuilder)
    {
        _deltaRules = deltaRules;
        _tokenRules = tokenRules;
        _evalRules = evalRules;
        _nightCatalog = nightCatalog;
        _contractBuilder = contractBuilder;
    }

    public BroadcastEndResult ProcessEnd(BroadcastSaveState state, BroadcastEventLog log)
    {
        // 1) 로그 → 델타
        BroadcastScoreDelta deltas = _deltaRules.ComputeScoredDeltas(log);

        // 2) State 업데이트(수치)
        state.zoneScore += deltas.zone;
        state.risk += deltas.risk;
        state.promiseDebt += deltas.promise;

        // clamp(선택)
        state.zoneScore = Mathf.Max(0, state.zoneScore);
        state.risk = Mathf.Clamp(state.risk, 0, 100);
        state.promiseDebt = Mathf.Max(0, state.promiseDebt);

        // 3) 델타 → 토큰 변환 및 state에 적립
        TokenDelta[] tokenDeltas = _tokenRules.GrantTokens(state, log, deltas);

        // 4) 평가(판정 + SoftFail 잠금)
        EvaluationResult eval = _evalRules.Evaluate(state, log, deltas);

        // 5) 밤 사건 선택(1개)
        NightEventPayload night = NightEventSelector.SelectOne(_nightCatalog, state, log, deltas, eval);

        // 6) 다음 계약
        NextContractPayload contract = _contractBuilder.BuildNext(state, eval);

        // 7) Settlement payload
        var settlement = new SettlementPayload
        {
            zoneDelta = deltas.zone,
            riskDelta = deltas.risk,
            promiseDelta = deltas.promise,
            tokenDeltas = tokenDeltas,
            lockAdded = eval.locksAdded,
            lockRemoved = LockFlags.None
        };

        // 8) Recap
        RecapLine recap = RecapBuilder.Build(log, deltas, eval, tokenDeltas, eval.locksAdded);

        // 9) 최종 출력
        return new BroadcastEndResult
        {
            recap = recap,
            settlement = settlement,
            evaluation = new EvaluationPayload
            {
                grade = eval.grade,
                contractMet = eval.contractMet,
                riskThresholdHit = eval.riskThresholdHit,
                breachCountDelta = eval.breachDelta,
                graceDelta = eval.graceDelta,
                noteText = eval.noteText
            },
            nightEvent = night,
            nextContract = contract
        };
    }
}