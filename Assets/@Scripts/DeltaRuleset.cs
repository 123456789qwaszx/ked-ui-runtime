public sealed class DeltaRuleset
{
    // 임계치(판정 테이블이랑 같이 쓰임)
    public int riskSoftThreshold = 60;
    public int riskHardThreshold = 80;

    // 델타 기본 계수(초기값)
    public int zoneBase = 0;
    public int riskBase = 0;
    public int promiseBase = 0;

    // 가산치(초기 밸런스)
    public int zonePerBigDonation = 6;
    public int zonePerPinned = 2;
    public int zonePerPositiveReact = 1;

    public int riskPerOperatorWarning = 12;
    public int riskPerRestriction = 8;
    public int riskPerClipSeeded = 10;
    public int riskPerNegativeReact = 1;

    public int promisePerDirectRequest = 1;
    public int promisePerAccepted = 1;
    public int promisePerDodged = 0; // 회피는 promise 대신 risk를 올리는 게 더 맛있음
    public int riskPerPromiseDodged = 6;

    // 태도(직감/분석/혼돈) 보정
    public int zonePerInstinct = 1;
    public int riskPerInstinct = 1;
    public int promisePerInstinct = 0;

    public int riskPerAnalysis = -2;     // 분석은 리스크를 깎는다
    public int zonePerAnalysis = 0;

    public int promisePerChaos = 2;
    public int riskPerChaos = 2;
    public int zonePerChaos = 0;         // 혼돈은 성과가 튀기도 하지만 P0에선 0으로 시작

    public Deltas ComputeDeltas(BroadcastEventLog log)
    {
        int zone = zoneBase;
        int risk = riskBase;
        int promise = promiseBase;

        // --- zone ---
        if ((log.flags & BroadcastFlags.BigDonationOccurred) != 0)
            zone += zonePerBigDonation;

        zone += log.myMsgPinnedCount * zonePerPinned;
        zone += log.idolPositiveReactTotal * zonePerPositiveReact;

        zone += log.instinctCountTotal * zonePerInstinct;
        zone += log.analysisCountTotal * zonePerAnalysis;
        zone += log.chaosCountTotal * zonePerChaos;

        // --- risk ---
        risk += log.operatorWarningCount * riskPerOperatorWarning;
        risk += log.restrictionTriggeredCount * riskPerRestriction;
        risk += log.clipSeededCount * riskPerClipSeeded;
        risk += log.idolNegativeReactTotal * riskPerNegativeReact;

        risk += log.instinctCountTotal * riskPerInstinct;
        risk += log.analysisCountTotal * riskPerAnalysis;
        risk += log.chaosCountTotal * riskPerChaos;

        // promise dodged => risk up
        risk += log.promiseDodgedCount * riskPerPromiseDodged;

        // --- promise ---
        promise += log.idolDirectRequestCount * promisePerDirectRequest;
        promise += log.promiseAcceptedCount * promisePerAccepted;
        promise += log.promiseDodgedCount * promisePerDodged;

        promise += log.chaosCountTotal * promisePerChaos;
        promise += log.instinctCountTotal * promisePerInstinct;

        return new Deltas(zone, risk, promise);
    }
}

public readonly struct Deltas
{
    public readonly int zone;
    public readonly int risk;
    public readonly int promise;

    public Deltas(int zone, int risk, int promise)
    {
        this.zone = zone;
        this.risk = risk;
        this.promise = promise;
    }
}