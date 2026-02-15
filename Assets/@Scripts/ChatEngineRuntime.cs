using System;

public sealed class ChatEngineRuntime
{
    public double now;

    public float accum;
    public float timeUntilNextEmit;

    // Burst
    public bool isBursting;
    public int burstRemaining;

    // Hygiene
    public float[] cooldownUntilByKind; // kind index 기반
    public int lastKindIndex = -1;
    public int sameKindStreak;

    // Donation gating
    public double lastDonationAt;

    // no-repeat
    public IntRingBuffer recentTextIds;
    
    // ===== Signal impulses (짧게 유지되는 힘) =====
    public float idolSpokeBoost;      // 0..1
    public float donationBoost;       // 0..1
    public float bigDonationBoost;    // 0..1
    public float systemBoost;         // 0..1
    public float myMsgBoost;          // 0..1

    public ChatEngineRuntime(int kindCount, int noRepeatWindow)
    {
        cooldownUntilByKind = new float[kindCount];
        recentTextIds = new IntRingBuffer(noRepeatWindow);
        Reset(kindCount, noRepeatWindow);
    }

    public void Reset(int kindCount, int noRepeatWindow)
    {
        now = 0;
        accum = 0;
        timeUntilNextEmit = 0;

        isBursting = false;
        burstRemaining = 0;

        if (cooldownUntilByKind == null || cooldownUntilByKind.Length != kindCount)
            cooldownUntilByKind = new float[kindCount];
        else
            Array.Clear(cooldownUntilByKind, 0, cooldownUntilByKind.Length);

        lastKindIndex = -1;
        sameKindStreak = 0;

        lastDonationAt = double.NegativeInfinity;

        if (recentTextIds == null) recentTextIds = new IntRingBuffer(noRepeatWindow);
        recentTextIds.Resize(noRepeatWindow);
        recentTextIds.Clear();
        
        idolSpokeBoost = 0f;
        donationBoost = 0f;
        bigDonationBoost = 0f;
        systemBoost = 0f;
        myMsgBoost = 0f;
    }
}