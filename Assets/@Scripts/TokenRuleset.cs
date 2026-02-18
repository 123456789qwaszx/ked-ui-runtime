using System.Collections.Generic;
using UnityEngine;

public sealed class TokenRuleset
{
    // 토큰 ID 상수화(나중에 SO/DB로 빼도 됨)
    public const string Token_PromiseZone = "token_promise_zone";
    public const string Token_CommunitySpark = "token_community_spark";
    public const string Token_OperatorWatch = "token_operator_watch";
    public const string Token_BigDonor = "token_big_donor";

    public TokenDelta[] ApplyTokens(BroadcastSaveState state, BroadcastEventLog log, BroadcastScoreDelta deltas)
    {
        var outList = new List<TokenDelta>(4);

        // 약속 수락이 있으면 약속 토큰
        if (log.promiseAcceptedCount > 0 || (log.flags & BroadcastFlags.PromiseAccepted) != 0)
        {
            AddOrStack(state, Token_PromiseZone, 1, ttlEvents: 2, outList,
                effectText: "다음 방송 Zone 목표 +10 (미달 시 Breach+1)");
        }

        // 클립 씨앗 / 독성 / 혼돈이 있으면 커뮤 불씨
        bool spark = log.clipSeededCount > 0 || (log.flags & BroadcastFlags.ClipSeeded) != 0 || deltas.risk >= 15;
        if (spark)
        {
            AddOrStack(state, Token_CommunitySpark, 1, ttlEvents: 2, outList,
                effectText: "다음 방송 Risk 시작값 +15, ‘악성편집’ 사건 우선순위↑");
        }

        // 운영 경고가 있으면 운영 주시
        if (log.operatorWarningCount > 0 || (log.flags & BroadcastFlags.OperatorWarning) != 0)
        {
            AddOrStack(state, Token_OperatorWatch, 1, ttlEvents: 1, outList,
                effectText: "다음 방송 제약 발생 확률↑ (투표/후원 제한 가능)");
        }

        // 고액 후원자(보상+대가)
        if ((log.flags & BroadcastFlags.BigDonationOccurred) != 0)
        {
            AddOrStack(state, Token_BigDonor, 1, ttlEvents: 3, outList,
                effectText: "특별대우 리액션↑, 대신 ‘DM 요구’ 사건 조건 활성");
        }

        return outList.ToArray();
    }

    private static void AddOrStack(
        BroadcastSaveState state,
        string tokenId,
        int stackDelta,
        int ttlEvents,
        List<TokenDelta> outList,
        string effectText)
    {
        int idx = FindTokenIndex(state.tokens, tokenId);
        if (idx < 0)
        {
            state.tokens.Add(new TokenState { tokenId = tokenId, stack = stackDelta, ttlEvents = ttlEvents });
            outList.Add(new TokenDelta { kind = TokenDeltaKind.Added, tokenId = tokenId, stackDelta = stackDelta, effectText = effectText });
            return;
        }

        var t = state.tokens[idx];
        t.stack += stackDelta;
        // ttl 갱신(최대)
        t.ttlEvents = Mathf.Max(t.ttlEvents, ttlEvents);
        state.tokens[idx] = t;

        outList.Add(new TokenDelta { kind = TokenDeltaKind.Stacked, tokenId = tokenId, stackDelta = stackDelta, effectText = effectText });
    }

    private static int FindTokenIndex(List<TokenState> list, string tokenId)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].tokenId == tokenId)
                return i;
        }
        return -1;
    }
}
