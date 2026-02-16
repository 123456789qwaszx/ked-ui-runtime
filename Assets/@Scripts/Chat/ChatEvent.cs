public enum ChatEventKind : byte
{
    Crowd = 0,
    Donation = 1,
    Idol = 2,
    System = 3,
    MyMsg = 4,
    EmoteOnly = 5, // 선택: 이모트만 뜨는 유형
}

public enum ChatSide : byte
{
    Other = 0,
    My = 1,
}

public enum CrowdFlavor : byte
{
    None = 0,
    Cheer = 1,
    Meme = 2,
    Ship = 3,
    Meta = 4,
    Toxic = 5,
    Chant = 6,
}

public readonly struct ChatEvent
{
    public readonly ChatEventKind kind;
    public readonly ChatSide side;

    public readonly CrowdFlavor flavor;

    // DB key (표시 텍스트/이름은 resolver가 만든다)
    public readonly int nameId;  // 0이면 이름 없음/정책에 따름
    public readonly int textId;  // 0이면 텍스트 없음

    // donation
    public readonly int amount;

    // emote
    public readonly int emoteId; // 0이면 없음
    public readonly bool bigEmoteOnly;

    public ChatEvent(
        ChatEventKind kind,
        ChatSide side,
        CrowdFlavor flavor,
        int nameId,
        int textId,
        int amount,
        int emoteId,
        bool bigEmoteOnly)
    {
        this.kind = kind;
        this.side = side;
        this.flavor = flavor;
        this.nameId = nameId;
        this.textId = textId;
        this.amount = amount;
        this.emoteId = emoteId;
        this.bigEmoteOnly = bigEmoteOnly;
    }
}