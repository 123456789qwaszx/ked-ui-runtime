using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ChatRuleProfile", menuName = "Live/Chat Rule Profile", order = 203)]
public sealed class ChatRuleProfileSO : ScriptableObject
{
    [Serializable]
    public struct KindWeight
    {
        public ChatEventKind kind;
        
        [Range(0f, 10f)]
        public float weight;
    }
    
    [Serializable]
    public struct FlavorWeight
    {
        public CrowdFlavor flavor;

        [Range(0f, 10f)]
        public float weight;
    }
    
    [Serializable]
    public struct KindCooldown
    {
        public ChatEventKind kind;
        public float cooldownSeconds;
    }
    
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
    
    [Tooltip("같은 kind가 연속될 때 가중치 배수. 1=변화없음, <1=반복 억제, >1=도배/연속 강화")]
    [Range(0f, 2f)]
    public float streakWeightMultiplier = 1.2f;
    
    [Header("B2. Crowd Flavor Mix (Crowd 내부 비율)")]
    [Tooltip("Crowd일 때 Flavor 가중치. 비어있으면 Cheer..Chant 균등.")]
    public FlavorWeight[] crowdFlavorWeights;


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
    public float maxDonateRateHz = 0.2f;
    
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

    // ==== Helpers ====

    public float GetKindWeight(ChatEventKind kind, bool isBurst)
    {
        KindWeight[] weights = isBurst && burstMixOverride != null && burstMixOverride.Length > 0
            ? burstMixOverride
            : kindWeights;

        if (weights == null)
            return 1f;

        foreach (KindWeight kw in weights)
        {
            if (kw.kind == kind)
                return kw.weight;
        }

        return 1f;
    }
    
    public float GetCrowdFlavorWeight(CrowdFlavor flavor)
    {
        if (crowdFlavorWeights == null || crowdFlavorWeights.Length == 0)
            return -1f; // "미사용" 표시(샘플러에서 균등 fallback)

        foreach (FlavorWeight fw in crowdFlavorWeights)
        {
            if (fw.flavor == flavor)
                return fw.weight;
        }

        return 0f; // 명시하지 않은 flavor는 0으로 취급(뽑히지 않음)
    }

    public float GetKindCooldown(ChatEventKind kind)
    {
        if (kindCooldowns == null)
            return 0f;

        foreach (KindCooldown kc in kindCooldowns)
        {
            if (kc.kind == kind)
                return kc.cooldownSeconds;
        }

        return 0f;
    }

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
        
        if (crowdFlavorWeights != null && crowdFlavorWeights.Length > 0)
        {
            bool[] seen = new bool[7];
            for (int i = 0; i < crowdFlavorWeights.Length; i++)
            {
                int f = (int)crowdFlavorWeights[i].flavor;
                if (f < 0 || f > 6) continue;

                if (seen[f])
                {
                    Debug.LogWarning($"[ChatRuleProfileSO] {name}: crowdFlavorWeights has duplicate flavor: {crowdFlavorWeights[i].flavor}", this);
                    break;
                }
                seen[f] = true;
            }
        }
    }
}