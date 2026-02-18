using System;
using UnityEngine;

[Flags]
public enum BroadcastFlags : uint
{
    None = 0,
    OperatorWarning = 1u << 0,
    RestrictionTriggered = 1u << 1,  // 채팅/투표 제한 등
    VoteSplit = 1u << 2,
    ClipSeeded = 1u << 3,
    BigDonationOccurred = 1u << 4,
    MyMsgPinned = 1u << 5,
    IdolDirectRequest = 1u << 6,     // “약속해줘” 직접 요청이 있었음
    PromiseAccepted = 1u << 7,
    PromiseDodged = 1u << 8,
}

public enum ChatTag : byte
{
    Instinct = 0, // 직감
    Analysis  = 1, // 분석
    Chaos    = 2, // 혼돈
}

public enum IdolReaction : byte
{
    Positive = 0,
    Negative = 1,
    Neutral  = 2,
}

public enum PhaseDecisionKind : byte
{
    BuildTrust = 0,
    PushSupport = 1,
    ManageRisk = 2,
    StirHeat = 3,
}

/// <summary>
/// 방송 1회(Event) 동안의 로그. (저장 단위)
/// </summary>
[Serializable]
public sealed class BroadcastEventLog
{
    public string runId;        // 회차 식별자 (예: "run_003")
    public string eventId;      // 방송 식별자 (예: "live_ravi_01")
    public int eventIndex;      // 회차 내 몇 번째 방송인지

    public double startedAtSec;
    public double endedAtSec;

    // ----- Event-level aggregates (방송 전체 누적) -----
    public int donationCountTotal;
    public long donationSumTotal;          // 금액 합(정수 권장)

    public int emojiCountTotal;            // 이모티콘 총 사용 수 (미미한 분위기 영향용)
    public int chatLineCountTotal;         // 프리셋 채팅 문구 총 사용 수

    public int instinctCountTotal;
    public int analysisCountTotal;
    public int chaosCountTotal;

    public int idolPositiveReactTotal;     // 아이돌 반응 결과 카운트 (강도 고정이므로 횟수만)
    public int idolNegativeReactTotal;
    public int idolNeutralReactTotal;

    // Phase logs (방송은 여러 Phase로 구성)
    public PhaseLog[] phases;

    // (선택) 방송 종료 시점에 계산된 6지수 스냅샷을 저장하고 싶다면
    public IndicesSnapshot indicesAtEnd;
    
    
    public BroadcastFlags flags;

    public int operatorWarningCount;
    public int restrictionTriggeredCount;

    public int voteSplitCount;     // 단순 bool이면 flags만으로 충분. 필요시 count.
    public int clipSeededCount;

    public int myMsgPinnedCount;
    public int idolDirectRequestCount;

    public int promiseAcceptedCount;
    public int promiseDodgedCount;

    // (선택) “이번 방송의 최종 태도”를 하나로 요약하고 싶다면
    public ChatTag dominantTag; // Instinct/Analysis/Chaos 중 최다
    
    public static BroadcastEventLog CreateNew(string runId, string eventId, int eventIndex, double startedAtSec)
    {
        return new BroadcastEventLog
        {
            runId = runId,
            eventId = eventId,
            eventIndex = eventIndex,
            startedAtSec = startedAtSec,

            endedAtSec = double.NaN,
            phases = null,
            indicesAtEnd = default,

            flags = BroadcastFlags.None,
            dominantTag = ChatTag.Instinct,

            // 나머지 int/long은 0이 기본이라 굳이 안 적어도 됨
        };
    }
}

/// <summary>
/// Phase(2~30초 공연 1회) 단위의 로그.
/// Phase 경계에서 다음 Profile 결정을 위해 바로 쓸 수 있는 최소 정보만.
/// </summary>
[Serializable]
public struct PhaseLog
{
    public int phaseIndex;
    public string phaseId;                 // 예: "perf_01", "perf_02"
    public string profileKeyAtEnter;       // Phase 진입 시 프로필 키 (선택 결과 추적용)

    public double startedAtSec;
    public double endedAtSec;

    // ----- Phase-level aggregates -----
    public int donationCount;
    public long donationSum;

    public int emojiCount;
    public int chatLineCount;

    public int instinctCount;
    public int analysisCount;
    public int chaosCount;

    public int idolPositiveReact;
    public int idolNegativeReact;
    public int idolNeutralReact;

    // ----- Intermission decision (Phase 전환 선택) -----
    public bool hasDecision;
    public PhaseDecisionLog decision;
    
    public static PhaseLog CreateNew(int phaseIndex, string phaseId, string profileKeyAtEnter, double startedAtSec)
    {
        return new PhaseLog
        {
            phaseIndex = phaseIndex,
            phaseId = phaseId,
            profileKeyAtEnter = profileKeyAtEnter,
            startedAtSec = startedAtSec,
            endedAtSec = double.NaN,
            hasDecision = false,
            decision = default
            // 나머지는 struct 기본값 0이라 생략
        };
    }
}

/// <summary>
/// Phase 전환 시 선택 요소 로그. (강도 고정, 종류만 기록)
/// </summary>
[Serializable]
public struct PhaseDecisionLog
{
    public PhaseDecisionKind kind;     // BuildTrust / PushSupport / ManageRisk / StirHeat
    public string optionId;            // 선택지 식별자 (예: "choice_trust_01")
    public bool accepted;             // (선택) 성공/실패 같은 결과가 있다면. 없다면 항상 true로.
}

/// <summary>
/// 방송 종료 시 계산된 6개 지수(0..1). 저장은 선택.
/// </summary>
[Serializable]
public struct IndicesSnapshot
{
    [Range(0f, 1f)] public float support01;
    [Range(0f, 1f)] public float trust01;
    [Range(0f, 1f)] public float risk01;
    [Range(0f, 1f)] public float heat01;
    [Range(0f, 1f)] public float control01;
    [Range(0f, 1f)] public float publicMood01;
}