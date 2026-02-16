using System;
using UnityEngine;

public sealed class DbChatPayloadSampler : IChatPayloadSampler
{
    private readonly ChatContentDBSO _db;
    private readonly UnityChatRng _rng;

    public DbChatPayloadSampler(ChatContentDBSO db, UnityChatRng rng)
    {
        _db = db;
        _rng = rng;
    }

    public bool TrySample(ChatRuleProfileSO profile, int kindIndex, ChatEngineRuntime rt, out ChatEvent evt)
    {
        var kind = (ChatEventKind)kindIndex;

        // 기본값
        ChatSide side = kind == ChatEventKind.MyMsg ? ChatSide.My : ChatSide.Other;
        CrowdFlavor flavor = CrowdFlavor.None;

        int nameId = 0;
        int textId = 0;
        int amount = 0;
        int emoteId = 0;
        bool bigEmoteOnly = false;

        // kind별 샘플링 정책 (지금은 단순, 나중에 signals로 분기 강화)
        switch (kind)
        {
            case ChatEventKind.Crowd:
                flavor = SampleCrowdFlavor(profile); // 간단히 랜덤 or 별도 분포로
                nameId = SampleNameId();
                textId = SampleTextId(kind, flavor, rt);
                break;

            case ChatEventKind.Donation:
                nameId = SampleNameId();
                amount = SampleDonationAmount();
                textId = SampleTextId(kind, CrowdFlavor.None, rt); // "후원 감사합니다" 같은 텍스트
                break;

            case ChatEventKind.Idol:
                // 아이돌은 이름 고정(또는 nameId=0으로 숨김)
                textId = SampleTextId(kind, CrowdFlavor.None, rt);
                break;

            case ChatEventKind.System:
                textId = SampleTextId(kind, CrowdFlavor.None, rt);
                break;

            case ChatEventKind.MyMsg:
                // 내 메시지는 외부에서 textId를 주입하는 방식도 가능
                textId = SampleTextId(kind, CrowdFlavor.None, rt);
                break;

            case ChatEventKind.EmoteOnly:
                nameId = SampleNameId();
                emoteId = SampleEmoteId();
                bigEmoteOnly = _rng.Next01() < 0.35f;
                break;
        }

        evt = new ChatEvent(kind, side, flavor, nameId, textId, amount, emoteId, bigEmoteOnly);
        return true;
    }

    private CrowdFlavor SampleCrowdFlavor(ChatRuleProfileSO profile)
    {
        // 프로파일이 없거나 flavorWeights가 비어있으면 기존처럼 균등
        if (!profile)
            return (CrowdFlavor)_rng.RangeInt(1, 7); // Cheer..Chant

        // profile.GetCrowdFlavorWeight가 -1이면 "미사용"이므로 균등 fallback
        // 가중치 룰렛: Cheer(1) ~ Chant(6)
        float total = 0f;

        for (int i = 1; i <= 6; i++)
        {
            float w = profile.GetCrowdFlavorWeight((CrowdFlavor)i);
            if (w < 0f)
            {
                // crowdFlavorWeights 미사용 → 균등 fallback
                return (CrowdFlavor)_rng.RangeInt(1, 7);
            }

            if (w > 0f)
                total += w;
        }

        if (total <= 0f)
        {
            // 전부 0이면 균등 fallback (또는 None 반환 정책도 가능)
            return (CrowdFlavor)_rng.RangeInt(1, 7);
        }

        float roll = _rng.Range(0f, total);
        float cum = 0f;

        for (int i = 1; i <= 6; i++)
        {
            float w = profile.GetCrowdFlavorWeight((CrowdFlavor)i);
            if (w <= 0f) continue;

            cum += w;
            if (roll < cum)
                return (CrowdFlavor)i;
        }

        // 이론상 도달 X (부동소수 오차 안전망)
        return (CrowdFlavor)_rng.RangeInt(1, 7);
    }

    private int SampleNameId()
    {
        if (!_db || _db.viewerNames == null || _db.viewerNames.Length == 0) return 0;
        return _rng.RangeInt(1, _db.viewerNames.Length + 1); // 1-based id
    }

    private int SampleEmoteId()
    {
        if (!_db || _db.emotes == null || _db.emotes.Length == 0) return 0;
        return _rng.RangeInt(1, _db.emotes.Length + 1);
    }

    private const int DebugTextId_SampleFailed = unchecked((int)0xFFFFFFFF);

    private int SampleTextId(ChatEventKind kind, CrowdFlavor flavor, ChatEngineRuntime rt)
    {
        if (!_db.TryGetTexts(kind, flavor, out var texts) || texts == null || texts.Length == 0)
        {// 실패원인 1: ChatContentDBSO.textBuckets에 해당 조합이 없음. 해결방안 (1): ChatContentDBSO 최소한 폴백을 위한(Crowd, None)을 추가하고, 올바른 (kind, flavor)을 추가하십니오.
         // ## 각 Kind별 1개씩의 None이 필요합니다. 또 런타임 중 추가/변경은 반영되지 않습니다.
         // 실패원인 2: 물려놓은 DB가 올바른 것인지 확인하고, 버킷 내 빈 Texts가 있는지 점검하십시오.
         // 실패원인 3: enum mismatch: Flavor Enum을 수정했다면, 오래된 데이터를 수정하십시오.
            Debug.LogError(
                $"[ChatSampler] No texts bucket. db={_db.name} kind={kind} flavor={flavor} (and/or None fallback empty)",
                _db
            );
            return DebugTextId_SampleFailed;
        }

        int safety = 8;
        int fallbackId = 0;  // noRepeat 실패 시 마지막으로 뽑은 id 저장
    
        while (safety-- > 0)
        {
            int idx = _rng.RangeInt(0, texts.Length);

            if (string.IsNullOrEmpty(texts[idx]))
            {
                int emptyId = MakeTextId(kind, flavor, idx);
                Debug.LogWarning( // 실패 원인: DB의 texts[idx]가 nuil 혹은 빈 문자열
                    $"[ChatSampler] Empty text skipped. db={_db.name} kind={kind} flavor={flavor} idx={idx} textId=0x{emptyId:X8}",
                    _db
                );
                continue;
            }

            int id = MakeTextId(kind, flavor, idx);

            if (rt.recentTextIds == null || !rt.recentTextIds.Contains(id))
                return id;

            // noRepeat에 걸렸지만, 최소한 유효한 텍스트는 있음
            fallbackId = id;
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log( // 실패원인: 랜덤으로 뽑은 텍스트가 최근 사용 기록에 있기에, 중복 금지 규칙에 걸림.
                // 주의: 정상상황이지만, noRepeatTextWindowN을 낮추거나, 자주 나오는 버킷의 텍스트를 추가하십시오.
                $"[ChatSampler] noRepeat hit. db={_db.name} kind={kind} flavor={flavor} idx={idx} textId=0x{id:X8}",
                _db
            );
#endif
        }

        // noRepeat 때문에 실패했지만, 유효한 텍스트는 있음 → fallback 사용
        if (fallbackId != 0)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning( // 실패 원인: while(safety 동안) 계속 noRepeat에 걸렸기에, 그냥 마지막 유효텍스트를 사용.
                // 해결 방안: noRepeatTextWindowN을 줄이거나, 버킷 텍스트를 늘리십시오.
                $"[ChatSampler] noRepeat failed, using fallback. db={_db.name} kind={kind} flavor={flavor} len={texts.Length}",
                _db
            );
#endif
            return fallbackId;  // 중복이지만 그냥 사용
        }

        Debug.LogError( // 경고: 유효한 텍스트를 하나도 못 찾았거나, 텍스트 배열 자체를 정상적으로 읽지 못했습니다.
            $"[ChatSampler] SampleTextId FAILED. db={_db.name} kind={kind} flavor={flavor} len={texts.Length}",
            _db
        );

        return DebugTextId_SampleFailed;
    }

    private int MakeTextId(ChatEventKind kind, CrowdFlavor flavor, int idx)
    {
        // 1-based id 생성
        int k = (int)kind & 0xFF;
        int f = (int)flavor & 0xFF;
        int i = (idx + 1) & 0xFFFF;  // 1-based로 변환
        return (k << 24) | (f << 16) | i;
    }

    private int SampleDonationAmount()
    {
        if (!_db || _db.donationAmounts == null || _db.donationAmounts.Length == 0)
            return 1000;

        float total = 0f;
        for (int i = 0; i < _db.donationAmounts.Length; i++)
            total += Mathf.Max(0f, _db.donationAmounts[i].weight);

        if (total <= 0f)
            return _db.donationAmounts[0].amount;

        float roll = _rng.Range(0f, total);
        float cum = 0f;

        for (int i = 0; i < _db.donationAmounts.Length; i++)
        {
            float w = Mathf.Max(0f, _db.donationAmounts[i].weight);
            cum += w;
            if (roll < cum)
                return _db.donationAmounts[i].amount;
        }

        return _db.donationAmounts[_db.donationAmounts.Length - 1].amount;
    }
}