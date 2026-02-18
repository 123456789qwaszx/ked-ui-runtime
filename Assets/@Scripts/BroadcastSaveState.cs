using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum UnlockFlags : ulong
{
    None = 0,
    // 예: 투표, 리플, 기록탭, DM탭, 리서치 등
    Vote = 1UL << 0,
    Replay = 1UL << 1,
    Records = 1UL << 2,
    DmTab = 1UL << 3,
    Research = 1UL << 4,
}

[Flags]
public enum LockFlags : ulong
{
    None = 0,
    VoteLocked = 1UL << 0,
    DonationPresetLocked = 1UL << 1,
    OneChoiceSealed = 1UL << 2,
    DmSuppressed = 1UL << 3,
    NightEventDarker = 1UL << 4, // “불길 노드 주입” 계열
}

public enum EvalGrade : byte
{
    Success = 0,
    AtRisk = 1,
    Breach = 2,
    Critical = 3,
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
public sealed class BroadcastSaveState
{
    public string runId;               // 현재 회차
    public int eventIndex;             // 다음 방송 인덱스(혹은 마지막 방송 인덱스)

    // 핵심 3축(정수 추천)
    public int zoneScore;              // 생존/경연 축
    public int risk;                   // 논란/독성/운영 축
    public int promiseDebt;            // 약속/책임 부채 축

    // 누적 상태
    public int breachCount;            // 약속 파기 누적
    public int graceRemaining;         // 유예(기회) 남은 횟수

    public UnlockFlags unlocks;
    public LockFlags locks;

    // 토큰(영속)
    public List<TokenState> tokens = new List<TokenState>(16);

    // (선택) “불길함 누적”을 한 값으로도 두고 싶다면
    public int gloom;                  // SoftFail 누적 지표(0..)
}

[Serializable]
public struct TokenState
{
    public string tokenId;
    public int stack;
    public int ttlEvents;              // 남은 방송 수(0이면 무기한)
}