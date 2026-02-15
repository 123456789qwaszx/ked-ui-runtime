// ChatRuleProfileSO.cs
// 튜닝 가능한 채팅 생성 프로필 (Rate + Mix + Burst + Hygiene)
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ChatRuleProfile", menuName = "Live/Chat Rule Profile", order = 203)]
public sealed class ChatRuleProfileSO : ScriptableObject
{
    [Header("A. Rate (속도)")]
    [Tooltip("기본 초당 생성 개수")]
    [Range(0.1f, 10f)]
    public float baseRatePerSec = 1.5f;
    
    [Tooltip("자연스러운 흔들림 (±%)")]
    [Range(0f, 0.5f)]
    public float rateJitter = 0.2f;
    
    [Tooltip("최소 발행 간격 (초) - 안전장치")]
    public float minInterval = 0.1f;
    
    [Tooltip("최대 발행 간격 (초) - 안전장치")]
    public float maxInterval = 5f;

    [Header("B. Mix (7 kinds 비율)")]
    [Tooltip("각 Kind별 가중치")]
    public KindWeight[] kindWeights;
    
    [Tooltip("연속성 보정 (같은 Actor/Action 이어질 확률 보정)")]
    [Range(0f, 2f)]
    public float streakBias = 1.2f;

    [System.Serializable]
    public struct KindWeight
    {
        public ChatEventKind kind;
        
        [Range(0f, 10f)]
        public float weight;
    }

    [Header("C. Burst (버스트)")]
    [Tooltip("초당 버스트 발생 확률")]
    [Range(0f, 1f)]
    public float burstChancePerSec = 0.05f;
    
    [Tooltip("버스트 길이 범위 (몇 개)")]
    public Vector2Int burstLengthRange = new Vector2Int(3, 8);
    
    [Tooltip("버스트 중 간격 범위 (초)")]
    public Vector2 burstIntervalRange = new Vector2(0.05f, 0.2f);
    
    [Tooltip("버스트 중 가중치 재정의 (비어있으면 기본 Mix 사용)")]
    public KindWeight[] burstMixOverride;

    [Header("D. Hygiene (위생 규칙)")]
    [Tooltip("Kind별 쿨다운 (초)")]
    public KindCooldown[] kindCooldowns;
    
    [Tooltip("같은 Kind 최대 연속 횟수")]
    [Range(1, 10)]
    public int maxSameKindStreak = 3;
    
    [Tooltip("최근 N개와 같은 텍스트 금지")]
    [Range(0, 20)]
    public int noRepeatTextWindowN = 5;
    
    [Tooltip("후원 최대 빈도 (초당) - 0이면 제한 없음")]
    [Range(0f, 1f)]
    public float maxDonateFrequency = 0.2f;
    
    [Header("E. Signal Response (신호 반응)")]
    [Range(0f, 5f)] public float idolSpokeWeightMul = 2.0f;
    [Range(0f, 5f)] public float donationWeightMul = 2.5f;
    [Range(0f, 5f)] public float bigDonationWeightMul = 3.5f;
    [Range(0f, 5f)] public float systemWeightMul = 2.0f;
    [Range(0f, 5f)] public float myMsgWeightMul = 2.0f;

    [Tooltip("신호 반응이 유지되는 시간(초)")]
    public float signalHoldSeconds = 2.0f;

    [Tooltip("신호 반응이 감쇠되는 속도(초당)")]
    public float signalDecayPerSec = 1.2f;
    
    

    [System.Serializable]
    public struct KindCooldown
    {
        public ChatEventKind kind;
        public float cooldownSeconds;
    }

    // ========== Helper API ==========

    /// <summary>
    /// Kind 가중치 가져오기 (기본 Mix 또는 Burst Override)
    /// </summary>
    public float GetKindWeight(ChatEventKind kind, bool isBurst)
    {
        var weights = isBurst && burstMixOverride != null && burstMixOverride.Length > 0
            ? burstMixOverride
            : kindWeights;

        if (weights == null)
            return 1f;

        foreach (var kw in weights)
        {
            if (kw.kind == kind)
                return kw.weight;
        }

        return 1f; // 기본값
    }

    /// <summary>
    /// Kind 쿨다운 가져오기
    /// </summary>
    public float GetKindCooldown(ChatEventKind kind)
    {
        if (kindCooldowns == null)
            return 0f;

        foreach (var kc in kindCooldowns)
        {
            if (kc.kind == kind)
                return kc.cooldownSeconds;
        }

        return 0f;
    }

    /// <summary>
    /// Jitter 적용된 Rate 계산
    /// </summary>
    public float GetJitteredRate()
    {
        float jitter = UnityEngine.Random.Range(-rateJitter, rateJitter);
        return baseRatePerSec * (1f + jitter);
    }

    /// <summary>
    /// Validation (Inspector 경고용)
    /// </summary>
    private void OnValidate()
    {
        if (minInterval > maxInterval)
        {
            Debug.LogWarning($"[ChatRuleProfileSO] {name}: minInterval > maxInterval", this);
        }

        if (kindWeights == null || kindWeights.Length == 0)
        {
            Debug.LogWarning($"[ChatRuleProfileSO] {name}: kindWeights is empty", this);
        }
    }
}