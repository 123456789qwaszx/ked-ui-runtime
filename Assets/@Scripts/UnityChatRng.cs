using UnityEngine;

public sealed class UnityChatRng : IChatRng
{
    public float Next01() => Random.value;
    public float Range(float a, float b) => Random.Range(a, b);
    public int RangeInt(int a, int bExclusive) => Random.Range(a, bExclusive);
}