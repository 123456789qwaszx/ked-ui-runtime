using System;

[Serializable]
public struct RecapLine
{
    public string summaryText;     // 요약 1줄(밝은데 불길)
    public RecapChangeLine[] changes; // 고정 3줄 권장
}

[Serializable]
public struct RecapChangeLine
{
    public string label;           // "Zone", "Risk", "Promise" 등
    public int delta;              // +12, -10 등
    public string causeText;       // (원인: 고액 후원/운영 경고/약속 수락)
}

[Serializable]
public struct SettlementPayload
{
    public int zoneDelta;
    public int riskDelta;
    public int promiseDelta;

    public TokenDelta[] tokenDeltas;  // 생성/강화/만료
    public LockFlags lockAdded;
    public LockFlags lockRemoved;     // 필요하면
}

public enum TokenDeltaKind : byte
{
    Added = 0,
    Stacked = 1,
    Removed = 2,
    Refreshed = 3,
}

[Serializable]
public struct TokenDelta
{
    public TokenDeltaKind kind;
    public string tokenId;
    public int stackDelta;        // +1 등
    public string effectText;     // “다음 방송 Risk 시작값 +15” 같은 1줄 영향력(필수)
}

[Serializable]
public struct EvaluationPayload
{
    public EvalGrade grade;
    public bool contractMet;      // 주 계약 달성 여부
    public bool riskThresholdHit; // 임계치 초과 여부
    public int breachCountDelta;  // +1 등
    public int graceDelta;        // -1 등
    public string noteText;       // “다음 방송이 더 불길해졌어” 1줄
}

[Serializable]
public struct NightEventPayload
{
    public NightEventKind kind;
    public string eventKey;       // 씬/노드/프리팹 키
    public string titleText;      // UI/로그 표시용
    public string teaserText;     // 1~2줄(밝은 문장 + 어긋난 단어)
}

[Serializable]
public struct NextContractPayload
{
    public string contractId;
    public string titleText;
    public string[] goalsText;    // 1~2개만
    public string hintText;       // “유혹적인 다음 방송 버튼” 문구
}

[Serializable]
public sealed class BroadcastEndResult
{
    public RecapLine recap;
    public SettlementPayload settlement;
    public EvaluationPayload evaluation;
    public NightEventPayload nightEvent;
    public NextContractPayload nextContract;
}