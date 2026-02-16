using System.Collections.Generic;
using UnityEngine;

public interface IChatPayloadSampler
{
    // kindIndex를 받아 payload(텍스트/금액/이모트 등)를 샘플링.
    // noRepeat 검사용 textId까지 포함된 ChatEvent를 반환.
    bool TrySample(int kindIndex, ChatEngineRuntime rt, out ChatEvent evt);
}

// 채팅 생성 엔진.
// - Tick: 시간 진행 + emit 스케줄링
// - SampleKindIndex: 현재 룰/상태 기반 kind 확률 샘플
// - Sampler: kind에 맞는 payload(DB) 샘플
// - RecordAfterEmit: streak/cooldown/no-repeat 같은 런타임 기록
public sealed class ChatEngineCore
{
    private readonly int _kindCount;
    private readonly IChatPayloadSampler _sampler;
    private readonly UnityChatRng _rng;

    // kind별 가중치 계산용 스크래치(매 Tick 할당/리스트 생성 방지)
    private readonly float[] _weightScratch;

    public ChatEngineCore(int kindCount, IChatPayloadSampler sampler, UnityChatRng rng)
    {
        _kindCount = kindCount;
        _sampler = sampler;
        _rng = rng;
        _weightScratch = new float[kindCount];
    }

    // dt만큼 진행하면서, 0개 이상 ChatEvent를 outQueue에 enqueue.
    // 프레임 드랍(dt가 커짐)에도 "누락 없이" 따라가도록 while로 catch-up
    public void Tick(ChatRuleProfileSO profile, ChatEngineRuntime rt, float dt, Queue<ChatEvent> outQueue)
    {
        if (!profile) return;

        rt.now += dt;
        rt.accumulatedTime += dt;

        DecaySignals(profile, rt, dt);

        // dt가 커도 emit 타이밍을 여러 번 따라가기
        int safety = 128; // dt 폭주 시 무한루프 방지
        while (rt.accumulatedTime >= rt.timeUntilNextEmit && safety-- > 0)
        {
            rt.accumulatedTime -= rt.timeUntilNextEmit;

            // 버스트는 "emit 시점"에서만 진입 체크
            if (!rt.isBursting)
            {
                if (TryStartBurst(profile, rt))
                {
                    rt.timeUntilNextEmit = NextBurstInterval(profile);
                    continue;
                }
            }

            // 1) kind 샘플(룰/상태/게이트 기반)
            int kindIndex = SampleKindIndex(profile, rt);

            if (kindIndex >= 0)
            {
                // 2) payload 샘플(DB) → ChatEvent 생성
                if (_sampler != null && _sampler.TrySample(kindIndex, rt, out ChatEvent evt))
                {
                    // 3) 런타임 기록(쿨다운/스트릭/중복방지 등)
                    RecordAfterEmit(profile, rt, kindIndex, evt);
                    outQueue.Enqueue(evt);
                }
            }

            // 4) 다음 emit 간격 재스케줄
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

        // safety로 빠져나오면(폭주) 누적시간을 정리해서 엔진 안정화
        if (safety <= 0)
        {
            rt.accumulatedTime = 0f;
            rt.timeUntilNextEmit = Mathf.Max(0.05f, rt.timeUntilNextEmit);
        }
    }

    // 버스트 진입 확률 체크(emit 시점에서만).
    // burstChancePerSec를 "이번 emit 간격"에 대한 확률로 변환
    private bool TryStartBurst(ChatRuleProfileSO profile, ChatEngineRuntime rt)
    {
        if (profile.burstChancePerSec <= 0f) return false;

        float p = Mathf.Clamp01(profile.burstChancePerSec * Mathf.Max(0.05f, rt.timeUntilNextEmit));
        if (_rng.Next01() >= p) return false;

        int length = _rng.RangeInt(profile.burstLengthRange.x, profile.burstLengthRange.y + 1);
        rt.isBursting = length > 0;
        rt.burstRemaining = length;

        return rt.isBursting;
    }

    //일반 모드 emit 간격(초): baseRatePerSec + jitter 적용 후 interval로 변환
    private float NextNormalInterval(ChatRuleProfileSO profile)
    {
        float jitter = _rng.Range(-profile.rateJitter, profile.rateJitter);
        float rate = profile.baseRatePerSec * (1f + jitter);

        float interval = 1f / rate;
        
        return Mathf.Clamp(interval, profile.minInterval, profile.maxInterval);
    }

    // 버스트 모드 emit 간격(초): 범위에서 랜덤
    private float NextBurstInterval(ChatRuleProfileSO profile)
    {
        float minInterval = Mathf.Max(0.001f, profile.burstIntervalRange.x);
        float maxInterval = Mathf.Max(minInterval, profile.burstIntervalRange.y);
        
        return _rng.Range(minInterval, maxInterval);
    }

    // 현재 룰/상태에 따라 kindIndex를 가중치 랜덤으로 선택.
    // - 쿨다운, 연속 제한, 도네이션 빈도 제한, 시그널 부스트 등을 반영.
    // - _weightScratch는 매 Tick 할당 방지용
    private int SampleKindIndex(ChatRuleProfileSO profile, ChatEngineRuntime rt)
    {
        float total = 0f;

        for (int i = 0; i < _kindCount; i++)
        {
            ChatEventKind kind = (ChatEventKind)i;
            float weight = profile.GetKindWeight(kind, rt.isBursting);

            if (weight <= 0f)
            {
                _weightScratch[i] = 0f;
                continue;
            }

            // kind 쿨다운(절대시간 게이트)
            float cooldown = profile.GetKindCooldown(kind);
            if (cooldown > 0f && rt.cooldownUntilByKind[i] > rt.now)
            {
                _weightScratch[i] = 0f; 
                continue;
            }

            // 같은 kind 연속 제한
            if (rt.lastKindIndex == i // 직전 Kind == 지금 검사 중인 Kind
                && rt.sameKindStreak >= profile.maxSameKindStreak) // Kind 연속 횟수가 제한치 도달
            {
                _weightScratch[i] = 0f;
                continue;
            }

            // 도네이션 빈도 제한
            if (kind == ChatEventKind.Donation && profile.maxDonateRateHz > 0f)
            {
                double minInterval = 1.0 / profile.maxDonateRateHz;
                if (rt.now - rt.lastDonationTimeSec < minInterval)
                {
                    _weightScratch[i] = 0f;
                    continue;
                }
            }

            // 연속 스트릭 보정(같은 kind 가중치 조절 1=변화없음, <1=반복 억제, >1=도배/연속 강화")
            if (rt.lastKindIndex == i && rt.sameKindStreak > 0)
            {
                weight *= Mathf.Pow(profile.streakWeightMultiplier, rt.sameKindStreak);
            }

            // 이벤트 부스트(아이돌 발언/도네/시스템/내말 등) 반영
            weight = ApplySignalWeightMultipliers(weight, profile, rt, i);

            _weightScratch[i] = weight;
            total += weight;
        }

        if (total <= 0f) return -1;

        // 가중치 룰렛 샘플
        float roll = _rng.Range(0f, total);
        float cum = 0f;
        for (int i = 0; i < _kindCount; i++)
        {
            float w = _weightScratch[i];
            if (w <= 0f) continue;

            cum += w;
            if (roll < cum) return i;
        }

        return -1;
    }

    // emit 이후 런타임 상태 업데이트(위생/게이트/중복방지).
    private void RecordAfterEmit(ChatRuleProfileSO profile, ChatEngineRuntime rt, int kindIndex, ChatEvent evt)
    {
        // same-kind streak
        if (rt.lastKindIndex == kindIndex) rt.sameKindStreak++;
        else { rt.lastKindIndex = kindIndex; rt.sameKindStreak = 1; }

        // kind cooldown(해제 시각 기록)
        float cd = profile.GetKindCooldown((ChatEventKind)kindIndex);
        if (cd > 0f)
            rt.cooldownUntilByKind[kindIndex] = rt.now + cd;

        // donation gating
        if (evt.kind == ChatEventKind.Donation)
            rt.lastDonationTimeSec = rt.now;

        // no-repeat window
        if (profile.noRepeatTextWindowN > 0 && evt.textId != 0)
            rt.recentTextIds.Push(evt.textId);
    }

    // 시그널 부스트 감쇠.
    // holdUntil 이전에는 감쇠하지 않고, hold가 끝난 뒤부터 decayPerSec로 감소.
    private void DecaySignals(ChatRuleProfileSO profile, ChatEngineRuntime rt, float dt)
    {
        float decay = Mathf.Max(0f, profile.signalDecayPerSec) * dt;
        float now = rt.now;

        if (now >= rt.idolSpokeHoldUntil)   rt.idolSpokeBoost = Mathf.Max(0f, rt.idolSpokeBoost - decay);
        if (now >= rt.donationHoldUntil)    rt.donationBoost = Mathf.Max(0f, rt.donationBoost - decay);
        if (now >= rt.bigDonationHoldUntil) rt.bigDonationBoost = Mathf.Max(0f, rt.bigDonationBoost - decay);
        if (now >= rt.systemHoldUntil)      rt.systemBoost = Mathf.Max(0f, rt.systemBoost - decay);
        if (now >= rt.myMsgHoldUntil)       rt.myMsgBoost = Mathf.Max(0f, rt.myMsgBoost - decay);
    }

    // kind별 가중치에 시그널 부스트를 곱으로 반영(0..1 → 1..mul).
    private float ApplySignalWeightMultipliers(float weight, ChatRuleProfileSO profile, ChatEngineRuntime rt, int kindIndex)
    {
        ChatEventKind kind = (ChatEventKind)kindIndex;

        switch (kind)
        {
            case ChatEventKind.Idol:
                if (rt.idolSpokeBoost > 0f)
                    weight *= Mathf.Lerp(1f, profile.idolSpokeWeightMul, rt.idolSpokeBoost);
                break;

            case ChatEventKind.Donation:
                if (rt.donationBoost > 0f)
                    weight *= Mathf.Lerp(1f, profile.donationWeightMul, rt.donationBoost);

                if (rt.bigDonationBoost > 0f)
                    weight *= Mathf.Lerp(1f, profile.bigDonationWeightMul, rt.bigDonationBoost);
                break;

            case ChatEventKind.System:
                if (rt.systemBoost > 0f)
                    weight *= Mathf.Lerp(1f, profile.systemWeightMul, rt.systemBoost);
                break;

            case ChatEventKind.MyMsg:
                if (rt.myMsgBoost > 0f)
                    weight *= Mathf.Lerp(1f, profile.myMsgWeightMul, rt.myMsgBoost);
                break;
        }

        return weight;
    }
}