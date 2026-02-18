using System;
using UnityEngine.Serialization;

[Serializable]
public struct SettlementPayload
{
    public int zoneDelta;
    public int riskDelta;
    public int promiseDelta;

    public TokenDelta[] tokenDeltas;  // 생성/강화/만료
    public LockFlags locksAdded;
    public LockFlags locksRemoved;
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

public enum NightEventKind : byte
{
    None = 0,
    Reward = 1,
    Pressure = 2,
    Scandal = 3,
    Operator = 4,
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
    public SettlementPayload settlementPayload;
    public EvaluationPayload evaluation;
    public NightEventPayload nightEvent;
    public NextContractPayload nextContract;
}