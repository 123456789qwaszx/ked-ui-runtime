public sealed class ContractBuilder
{
    public NextContractPayload BuildNext(BroadcastSaveState state, EvaluationResult eval)
    {
        // P0: 계약 1개만
        // 규칙: breach/critical이면 목표 강화 + 선택 봉인 경고
        int zoneTarget = 10;

        if (eval.grade == EvalGrade.AtRisk) zoneTarget = 12;
        if (eval.grade == EvalGrade.Breach) zoneTarget = 14;
        if (eval.grade == EvalGrade.Critical) zoneTarget = 16;

        string goal1 = $"다음 방송에서 ZoneScore +{zoneTarget} 달성";
        string goal2 = state.risk >= 60 ? "Risk를 낮춰서 ‘잠금’을 풀어줘" : "약속을 가볍게 만들 수 있는 선택을 찾아줘";

        return new NextContractPayload
        {
            contractId = "contract_zone_survive_v1",
            titleText = "다음 스테이지 약속",
            goalsText = new[] { goal1, goal2 },
            hintText = "다음 방송 알림이 왔어. …이번엔, 지켜줄 거지?"
        };
    }
}