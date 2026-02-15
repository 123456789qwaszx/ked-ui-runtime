// ChatPresetSO.cs
// 프리셋별 Kind 가중치 설정 (A/B/C)
using UnityEngine;

[CreateAssetMenu(fileName = "ChatPreset", menuName = "Live/Chat Preset", order = 202)]
public sealed class ChatPresetSO : ScriptableObject
{
    [Header("Preset Info")]
    public ChatPreset presetType;
    
    [Tooltip("프리셋 설명 (기획 참고용)")]
    [TextArea(2, 3)]
    public string description;

    [Header("Kind Weight Multipliers")]
    [Tooltip("각 Kind별 가중치 배율 (1.0 = 기본, 2.0 = 2배, 0 = 차단)")]
    public KindWeightPair[] kindWeights;

    [System.Serializable]
    public struct KindWeightPair
    {
        public ChatEntryKind kind;
        
        [Range(0f, 5f)]
        public float weightMultiplier;
    }

    [Header("Special Rules")]
    [Tooltip("TOXIC 리프레인 허용 여부 (C에서만 true)")]
    public bool allowToxicRefrain = false;
    
    [Tooltip("MYMSG 하이라이트 허용 (B에서만 true)")]
    public bool allowMyMsgHighlight = false;
    
    [Tooltip("MYMSG 비율 (0~1, B일 때만 사용)")]
    [Range(0f, 1f)]
    public float myMsgRatio = 0f;

    /// <summary>
    /// 특정 Kind의 가중치 배율 가져오기
    /// </summary>
    public float GetKindMultiplier(ChatEntryKind kind)
    {
        if (kindWeights == null)
            return 1f;

        foreach (var pair in kindWeights)
        {
            if (pair.kind == kind)
                return pair.weightMultiplier;
        }

        return 1f; // 기본값
    }

    /// <summary>
    /// Validation (Inspector 경고용)
    /// </summary>
    private void OnValidate()
    {
        // B가 아닌데 myMsgRatio가 0이 아니면 경고
        if (presetType != ChatPreset.B && myMsgRatio > 0f)
        {
            Debug.LogWarning($"[ChatPresetSO] {name}: myMsgRatio는 Preset B에서만 사용됩니다.", this);
        }

        // C가 아닌데 allowToxicRefrain이 true면 경고
        if (presetType != ChatPreset.C && allowToxicRefrain)
        {
            Debug.LogWarning($"[ChatPresetSO] {name}: allowToxicRefrain은 Preset C에서만 true여야 합니다.", this);
        }
    }
}