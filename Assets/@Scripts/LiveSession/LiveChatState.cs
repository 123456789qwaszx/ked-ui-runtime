using UnityEngine;

public sealed class LiveChatState
{
    public double lastWaveAt;
    public float minWaveInterval = 0.5f;

    public bool CanEmitWave()
    {
        return Time.time - lastWaveAt >= minWaveInterval;
    }

    public void ConsumeWave()
    {
        lastWaveAt = Time.time;
    }
}