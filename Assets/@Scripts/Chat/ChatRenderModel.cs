using UnityEngine;

public readonly struct ChatRenderModel
{
    public readonly ChatEventKind kind;
    public readonly ChatSide side;

    public readonly string nameText;
    public readonly string bodyText;

    public readonly int amount;
    public readonly Sprite emoteSprite;
    public readonly bool bigEmoteOnly;

    public ChatRenderModel(
        ChatEventKind kind, ChatSide side,
        string nameText, string bodyText,
        int amount, Sprite emoteSprite, bool bigEmoteOnly)
    {
        this.kind = kind;
        this.side = side;
        this.nameText = nameText;
        this.bodyText = bodyText;
        this.amount = amount;
        this.emoteSprite = emoteSprite;
        this.bigEmoteOnly = bigEmoteOnly;
    }
}