using UnityEngine;

public interface IChatRenderResolver
{
    ChatRenderModel Resolve(in ChatEvent evt);
}

public sealed class DbChatRenderResolver : IChatRenderResolver
{
    private readonly ChatContentDBSO _db;

    public DbChatRenderResolver(ChatContentDBSO db)
    {
        _db = db;
    }

    public ChatRenderModel Resolve(in ChatEvent evt)
    {
        string name = ResolveName(evt.nameId, evt);
        string body = ResolveBody(evt.textId, evt);
        Sprite emote = ResolveEmote(evt.emoteId);

        // Donation 표시 텍스트 정책 같은 건 여기서 결정
        if (evt.kind == ChatEventKind.Donation && evt.amount > 0)
        {
            // body가 없으면 기본 템플릿
            if (string.IsNullOrEmpty(body))
                body = "후원했습니다!";
        }

        return new ChatRenderModel(
            evt.kind,
            evt.side,
            name,
            body,
            evt.amount,
            emote,
            evt.bigEmoteOnly
        );
    }

    private string ResolveName(int nameId, in ChatEvent evt)
    {
        if (!_db || _db.viewerNames == null || _db.viewerNames.Length == 0) return string.Empty;
        if (evt.kind == ChatEventKind.Idol) return "라비"; // 지금은 하드코딩, 나중엔 별도 DB로
        if (evt.side == ChatSide.My) return "나";

        int idx = nameId - 1;
        if (idx < 0 || idx >= _db.viewerNames.Length) return string.Empty;
        return _db.viewerNames[idx];
    }

    private const int DebugTextId_SampleFailed = unchecked((int)0xFFFFFFFF);

    private string ResolveBody(int textId, in ChatEvent evt)
    {
        if (!_db) return string.Empty;

        if (textId == DebugTextId_SampleFailed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return $"<color=red>[MISSING TEXT: {evt.kind}/{evt.flavor}]</color>";
#else
        return "...";
#endif
        }

        if (textId == 0) return string.Empty;

        int k = (textId >> 24) & 0xFF;
        int f = (textId >> 16) & 0xFF;
        int i = (textId & 0xFFFF) - 1;
        if (i < 0) return string.Empty;

        var kind = (ChatEventKind)k;
        var flavor = (CrowdFlavor)f;

        if (_db.TryGetTexts(kind, flavor, out var texts) && texts != null && texts.Length > 0)
        {
            if (i >= 0 && i < texts.Length) return texts[i];
        }

        if (_db.TryGetTexts(kind, CrowdFlavor.None, out texts) && texts != null && texts.Length > 0)
        {
            int idx = Mathf.Clamp(i, 0, texts.Length - 1);
            return texts[idx];
        }

        return string.Empty;
    }

    private Sprite ResolveEmote(int emoteId)
    {
        if (emoteId == 0 || !_db || _db.emotes == null || _db.emotes.Length == 0) return null;
        int idx = emoteId - 1;
        if (idx < 0 || idx >= _db.emotes.Length) return null;
        return _db.emotes[idx];
    }
}
