using System;

public enum DmEventKind : byte
{
    Line = 0,
    Choice = 1,
    Marker = 2,
}

[Serializable]
public struct DmScript
{
    public string threadId;   // 예: "dm.ravi.main"
    public string version;    // 예: "1"
    public DmEvent[] events;
}

[Serializable]
public struct DmEvent
{
    public string id;         // 안정 키 (세이브/로그/디버그)
    public DmEventKind kind;

    public DmLine line;
    public DmChoice choice;
    public DmMarker marker;
}

[Serializable]
public struct DmLine
{
    public DmEntryKind kind;   // Incoming/Outgoing/System
    public string speaker;     // "라비" / "나" / ""(system)
    public string text;

    public float typingSeconds; // (아직 미사용이지만 남겨도 됨)
    public bool waitForTap;     // 라인 단위로만 남겨두기 OK
}

[Serializable]
public struct DmChoice
{
    public string prompt;
    public DmChoiceOption[] options;
}

[Serializable]
public struct DmChoiceOption
{
    public string id;
    public string text;
    public string gotoEventId;     // 없으면 다음으로 진행
}

[Serializable]
public struct DmMarker
{
    public string label;           // 날짜/구분선 텍스트
}

public enum DmEntryKind : byte
{
    Incoming = 0,   // 라비
    Outgoing = 1,   // 나
    System = 2,     // 구분선/공지
}

public readonly struct DmEntryModel
{
    public readonly DmEntryKind Kind;
    public readonly string Name;
    public readonly string Text;

    public DmEntryModel(DmEntryKind kind, string name, string text)
    {
        Kind = kind;
        Name = name;
        Text = text;
    }
}

public enum DmResultKind : byte
{
    Completed = 0,
    ChoiceSelected = 1,
}

public readonly struct DmResultEvent
{
    public readonly string ThreadId;
    public readonly DmResultKind Kind;
    public readonly string EventId;
    public readonly string ChoiceId;

    public DmResultEvent(string threadId, DmResultKind kind, string eventId, string choiceId)
    {
        ThreadId = threadId;
        Kind = kind;
        EventId = eventId;
        ChoiceId = choiceId;
    }
}