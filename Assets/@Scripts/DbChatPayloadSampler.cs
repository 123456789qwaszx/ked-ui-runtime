using UnityEngine;

public sealed class DbChatPayloadSampler : IChatPayloadSampler
{
    private readonly ChatContentDBSO _db;
    private readonly IChatRng _rng;

    public DbChatPayloadSampler(ChatContentDBSO db, IChatRng rng)
    {
        _db = db;
        _rng = rng;
    }

    public bool TrySample(int kindIndex, ChatEngineRuntime rt, out ChatEvent evt)
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
                flavor = SampleCrowdFlavor(); // 간단히 랜덤 or 별도 분포로
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

    private CrowdFlavor SampleCrowdFlavor()
    {
        // 최소 구현: 균등. 나중에 Profile/Signals로 flavor 분포를 분리해도 됨.
        int v = _rng.RangeInt(1, 7); // Cheer..Chant
        return (CrowdFlavor)v;
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

    private int SampleTextId(ChatEventKind kind, CrowdFlavor flavor, ChatEngineRuntime rt)
    {
        if (!_db) return 0;
        if (!_db.TryGetTexts(kind, flavor, out var texts) || texts == null || texts.Length == 0) return 0;

        // textId를 “배열 인덱스”로 잡으면, noRepeat가 깨질 수 있어(버킷별 중복).
        // 최소 구현으로는 (kind, flavor, index) 해시를 textId로 만든다.
        int safety = 8;
        while (safety-- > 0)
        {
            int idx = _rng.RangeInt(0, texts.Length);
            int id = MakeTextId(kind, flavor, idx);

            if (rt.recentTextIds == null || !rt.recentTextIds.Contains(id))
                return id;
        }

        return 0;
    }

    private int MakeTextId(ChatEventKind kind, CrowdFlavor flavor, int idx)
    {
        // 간단 안정 해시: 1-based로 만들기
        // kind(0..), flavor(0..), idx(0..)
        int k = (int)kind & 0xFF;
        int f = (int)flavor & 0xFF;
        int i = idx & 0xFFFF;
        return (k << 24) | (f << 16) | i | 1;
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
