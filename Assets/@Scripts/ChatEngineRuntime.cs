using System;

// ChatEngine 런타임 상태(프레임마다 변하는 값들만 보관).
// 스펙/룰/DB 같은 정적 데이터는 여기 두지 않는다.
public sealed class ChatEngineRuntime
{
    // ===== Clock / emit scheduling =====
    public float now;                 // 엔진 기준 현재 시간(초). hold/cooldown의 기준.
    public float accumulatedTime;     // dt 누적(emit 타이밍 계산용).
    public float timeUntilNextEmit;   // 다음 emit까지 남은 시간(초).

    // ===== Burst =====
    public bool isBursting;           // 몰아치기 모드 여부.
    public int burstRemaining;        // 남은 버스트 emit 횟수.

    // ===== Hygiene (repeat/cooldown) =====
    public float[] cooldownUntilByKind; // kind별 쿨다운 해제 시각(now 기준, 절대시간).
    public int lastKindIndex = -1;      // 직전 emit kind.
    public int sameKindStreak;          // 같은 kind 연속 횟수(반복 방지에 사용).

    // ===== Donation gating =====
    public double lastDonationTimeSec;     // 마지막 도네이션 시각(도네이션 반응 연타 방지).

    // ===== No-repeat text =====
    public IntRingBuffer recentTextIds; // 최근 텍스트 ID 링버퍼(최근 중복 방지).

    // ===== Signal impulses (짧게 감쇠되는 부스트 0..1) =====
    public float idolSpokeBoost;
    public float donationBoost;
    public float bigDonationBoost;
    public float systemBoost;
    public float myMsgBoost;

    // ===== Hold until (N초 유지하는 영향의 종료 시각) =====
    public float idolSpokeHoldUntil;
    public float donationHoldUntil;
    public float bigDonationHoldUntil;
    public float systemHoldUntil;
    public float myMsgHoldUntil;

    public ChatEngineRuntime(int kindCount, int noRepeatWindow)
    {
        cooldownUntilByKind = new float[kindCount];
        recentTextIds = new IntRingBuffer(noRepeatWindow);
        Reset(kindCount, noRepeatWindow);
    }

    /// <summary>세션 시작/프리셋 변경 등에서 런타임 상태 초기화.</summary>
    public void Reset(int kindCount, int noRepeatWindow)
    {
        now = 0f;
        accumulatedTime = 0f;
        timeUntilNextEmit = 0f;

        isBursting = false;
        burstRemaining = 0;

        if (cooldownUntilByKind == null || cooldownUntilByKind.Length != kindCount)
            cooldownUntilByKind = new float[kindCount];
        else
            Array.Clear(cooldownUntilByKind, 0, cooldownUntilByKind.Length);

        lastKindIndex = -1;
        sameKindStreak = 0;

        lastDonationTimeSec = double.NegativeInfinity;

        if (recentTextIds == null) recentTextIds = new IntRingBuffer(noRepeatWindow);
        recentTextIds.Resize(noRepeatWindow);
        recentTextIds.Clear();

        idolSpokeBoost = 0f;
        donationBoost = 0f;
        bigDonationBoost = 0f;
        systemBoost = 0f;
        myMsgBoost = 0f;

        idolSpokeHoldUntil = 0f;
        donationHoldUntil = 0f;
        bigDonationHoldUntil = 0f;
        systemHoldUntil = 0f;
        myMsgHoldUntil = 0f;
    }
}