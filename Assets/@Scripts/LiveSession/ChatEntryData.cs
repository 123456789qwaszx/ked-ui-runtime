using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum ChatEntryType : byte
{
    Chat = 0,
    Donation = 1,
    Idol = 2,
    System = 3,
}

public enum ChatCrowdKind : byte
{
    None = 0,
    Cheer = 1,
    Meme = 2,
    Ship = 3,
    Meta = 4,
    Toxic = 5,
    Chant = 6,
    MyMsg = 7,
}

public enum ChatEntrySide : byte
{
    Other = 0,
    My = 1,
}

[Serializable]
public struct ChatEntryData
{
    public ChatEntryType type;
    public ChatEntrySide side;
    public ChatCrowdKind crowdKind;

    public string chatName;
    public string chatBody;

    public int donationAmount;
    public Sprite emoteSprite;
    public bool bigEmoteOnly;
}