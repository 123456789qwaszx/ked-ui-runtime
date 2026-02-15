using System.Collections.Generic;
using UnityEngine;

public interface IChatRng
{
    float Next01();                 // [0,1)
    float Range(float a, float b);  // [a,b)
    int RangeInt(int a, int bExclusive);
}

public interface IChatPayloadSampler
{
    // kindIndex를 받아서 “텍스트/금액/이모트” 같은 payload를 샘플링하고,
    // noRepeat 검사에 쓸 textId를 함께 돌려주는 형태를 추천
    bool TrySample(int kindIndex, ChatEngineRuntime rt, out ChatEvent evt);
}

public sealed class ChatEngineCore
{
    private readonly int _kindCount;
    private readonly IChatPayloadSampler _sampler;
    private readonly IChatRng _rng;

    // 샘플링 스크래치 (할당 방지)
    private readonly float[] _wScratch;

    public ChatEngineCore(int kindCount, IChatPayloadSampler sampler, IChatRng rng)
    {
        _kindCount = kindCount;
        _sampler = sampler;
        _rng = rng;
        _wScratch = new float[kindCount];
    }

    public void Tick(ChatRuleProfileSO profile, ChatEngineRuntime rt, float dt, Queue<ChatEvent> outQueue)
    {
        if (!profile) return;

        DecaySignals(profile, rt, dt);

        rt.now += dt;
        rt.accum += dt;

        // 프레임 드랍에도 정확히 따라가기
        int safety = 128; // 혹시 dt가 폭발했을 때 무한루프 방지
        while (rt.accum >= rt.timeUntilNextEmit && safety-- > 0)
        {
            rt.accum -= rt.timeUntilNextEmit;

            // 버스트 시작 체크 (버스트가 아니고, 발행 타이밍에 도달했을 때만)
            if (!rt.isBursting)
            {
                if (TryStartBurst(profile, rt))
                {
                    // 버스트는 "바로 다음 emit 간격"을 burst interval로 둠
                    rt.timeUntilNextEmit = NextBurstInterval(profile);
                    continue;
                }
            }

            // 1) Kind 샘플
            int kindIndex = SampleKindIndex(profile, rt);

            if (kindIndex >= 0)
            {
                // 2) Payload 샘플 (DB)
                if (_sampler != null && _sampler.TrySample(kindIndex, rt, out var evt))
                {
                    // 3) 기록 (streak/cooldown/no-repeat 등)
                    RecordAfterEmit(profile, rt, kindIndex, evt);
                    outQueue.Enqueue(evt);
                }
            }

            // 4) 다음 간격 스케줄
            if (rt.isBursting)
            {
                rt.burstRemaining--;
                if (rt.burstRemaining <= 0)
                {
                    rt.isBursting = false;
                    rt.timeUntilNextEmit = NextNormalInterval(profile);
                }
                else
                {
                    rt.timeUntilNextEmit = NextBurstInterval(profile);
                }
            }
            else
            {
                rt.timeUntilNextEmit = NextNormalInterval(profile);
            }
        }

        // safety로 빠져나온 경우, accum을 정리(폭주 방지)
        if (safety <= 0)
        {
            rt.accum = 0;
            rt.timeUntilNextEmit = Mathf.Max(0.05f, rt.timeUntilNextEmit);
        }
    }

    private bool TryStartBurst(ChatRuleProfileSO profile, ChatEngineRuntime rt)
    {
        if (profile.burstChancePerSec <= 0f) return false;

        // “발행 시점”에서만 확률 체크: dt 독립적으로 안정
        // chancePerSec를 “발행마다”로 변환하려면 대략 interval을 곱해도 되지만,
        // 여기서는 단순히 작은 확률로 충분(튜닝은 profile에서).
        float p = Mathf.Clamp01(profile.burstChancePerSec * Mathf.Max(0.05f, rt.timeUntilNextEmit));
        if (_rng.Next01() >= p) return false;

        int len = _rng.RangeInt(profile.burstLengthRange.x, profile.burstLengthRange.y + 1);
        rt.isBursting = len > 0;
        rt.burstRemaining = len;
        return rt.isBursting;
    }

    private float NextNormalInterval(ChatRuleProfileSO profile)
    {
        float rate = Mathf.Max(0.0001f, profile.baseRatePerSec);
        float jitter = profile.rateJitter > 0f ? _rng.Range(-profile.rateJitter, profile.rateJitter) : 0f;
        float jitteredRate = rate * (1f + jitter);
        if (jitteredRate <= 0.0001f) jitteredRate = 0.0001f;

        float interval = 1f / jitteredRate;
        return Mathf.Clamp(interval, profile.minInterval, profile.maxInterval);
    }

    private float NextBurstInterval(ChatRuleProfileSO profile)
    {
        float a = Mathf.Max(0.001f, profile.burstIntervalRange.x);
        float b = Mathf.Max(a, profile.burstIntervalRange.y);
        return _rng.Range(a, b);
    }

    private int SampleKindIndex(ChatRuleProfileSO profile, ChatEngineRuntime rt)
    {
        // 0) weight source 선택 (burst override)
        // 여기서 중요한 건 “foreach + 리스트 생성”을 피하고,
        // 런타임에서 kindCount 배열로 weight map을 만들어 쓰는 것.
        // 지금은 profile에서 직접 조회하는 방식이므로, 아래는 안전한 기본 구현(개선 포인트는 하단에 따로).
        // ----
        float total = 0f;
        for (int i = 0; i < _kindCount; i++)
        {
            var kind = (ChatEventKind)i;
            float w = profile.GetKindWeight(kind, rt.isBursting);

            if (w <= 0f) { _wScratch[i] = 0f; continue; }

            // cooldown
            float cd = profile.GetKindCooldown(kind);
            if (cd > 0f && rt.cooldownUntilByKind[i] > rt.now) { _wScratch[i] = 0f; continue; }

            // same-kind streak limit
            if (rt.lastKindIndex == i && rt.sameKindStreak >= profile.maxSameKindStreak)
            {
                _wScratch[i] = 0f;
                continue;
            }

            // donation frequency limit
            if (kind == ChatEventKind.Donation && profile.maxDonateFrequency > 0f)
            {
                double minInterval = 1.0 / profile.maxDonateFrequency;
                if (rt.now - rt.lastDonationAt < minInterval)
                {
                    _wScratch[i] = 0f;
                    continue;
                }
            }

            // streak bias (연속성 보정)
            if (rt.lastKindIndex == i && rt.sameKindStreak > 0)
            {
                w *= Mathf.Pow(profile.streakBias, rt.sameKindStreak);
            }
            
            ApplySignalWeightMultipliers(ref w, profile, rt, i);

            _wScratch[i] = w;
            total += w;
        }
        
        if (total <= 0f) return -1;

        float roll = _rng.Range(0f, total);
        float cum = 0f;
        for (int i = 0; i < _kindCount; i++)
        {
            float w = _wScratch[i];
            if (w <= 0f) continue;
            cum += w;
            if (roll < cum) return i;
        }
        return -1;
    }

    private void RecordAfterEmit(ChatRuleProfileSO profile, ChatEngineRuntime rt, int kindIndex, ChatEvent evt)
    {
        // streak
        if (rt.lastKindIndex == kindIndex) rt.sameKindStreak++;
        else { rt.lastKindIndex = kindIndex; rt.sameKindStreak = 1; }

        // cooldown until
        float cd = profile.GetKindCooldown((ChatEventKind)kindIndex);
        if (cd > 0f)
        {
            rt.cooldownUntilByKind[kindIndex] = (float)(rt.now + cd);
        }

        // donation time
        if (evt.kind == ChatEventKind.Donation)
        {
            rt.lastDonationAt = rt.now;
        }

        // no-repeat window
        if (profile.noRepeatTextWindowN > 0 && evt.textId != 0)
        {
            rt.recentTextIds.Push(evt.textId);
        }
    }
    
    private void DecaySignals(ChatRuleProfileSO profile, ChatEngineRuntime rt, float dt)
    {
        float decay = Mathf.Max(0f, profile.signalDecayPerSec) * dt;

        rt.idolSpokeBoost = Mathf.Max(0f, rt.idolSpokeBoost - decay);
        rt.donationBoost = Mathf.Max(0f, rt.donationBoost - decay);
        rt.bigDonationBoost = Mathf.Max(0f, rt.bigDonationBoost - decay);
        rt.systemBoost = Mathf.Max(0f, rt.systemBoost - decay);
        rt.myMsgBoost = Mathf.Max(0f, rt.myMsgBoost - decay);
    }
    
    private void ApplySignalWeightMultipliers(ref float w, ChatRuleProfileSO profile, ChatEngineRuntime rt, int kindIndex)
    {
        var kind = (ChatEventKind)kindIndex;

        if (kind == ChatEventKind.Idol && rt.idolSpokeBoost > 0f)
        {
            float mul = Mathf.Lerp(1f, profile.idolSpokeWeightMul, rt.idolSpokeBoost);
            w *= mul;
        }

        if (kind == ChatEventKind.Donation && rt.donationBoost > 0f)
        {
            float mul = Mathf.Lerp(1f, profile.donationWeightMul, rt.donationBoost);
            w *= mul;
        }

        if (kind == ChatEventKind.Donation && rt.bigDonationBoost > 0f)
        {
            float mul = Mathf.Lerp(1f, profile.bigDonationWeightMul, rt.bigDonationBoost);
            w *= mul;
        }

        if (kind == ChatEventKind.System && rt.systemBoost > 0f)
        {
            float mul = Mathf.Lerp(1f, profile.systemWeightMul, rt.systemBoost);
            w *= mul;
        }

        if (kind == ChatEventKind.MyMsg && rt.myMsgBoost > 0f)
        {
            float mul = Mathf.Lerp(1f, profile.myMsgWeightMul, rt.myMsgBoost);
            w *= mul;
        }
    }
}
