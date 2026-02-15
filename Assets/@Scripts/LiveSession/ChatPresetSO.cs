using UnityEngine;

public enum ChatPreset
{
    A,
    B,
    C,
}

[CreateAssetMenu(fileName = "ChatPreset", menuName = "Live/Chat Preset", order = 202)]
public sealed class ChatPresetSO : ScriptableObject
{
    public ChatPreset presetType;

    [System.Serializable]
    public struct KindWeightPair
    {
        public ChatCrowdKind kind;
        [Range(0f, 5f)] public float weightMultiplier; // 여기서는 “선택 가중치”로 사용
    }

    public KindWeightPair[] kindWeights;

    public bool allowToxicRefrain = false;
    public bool allowMyMsgHighlight = false;

    [Range(0f, 1f)]
    public float myMsgRatio = 0f;

    public float GetKindMultiplier(ChatCrowdKind kind)
    {
        if (kindWeights == null) return 1f;
        for (int i = 0; i < kindWeights.Length; i++)
        {
            if (kindWeights[i].kind == kind)
                return kindWeights[i].weightMultiplier;
        }
        return 1f;
    }

    public ChatCrowdKind RollCrowdKind(bool allowMyMsg, bool allowToxic)
    {
        if (kindWeights == null || kindWeights.Length == 0)
            return ChatCrowdKind.Cheer;

        float total = 0f;

        for (int i = 0; i < kindWeights.Length; i++)
        {
            var k = kindWeights[i].kind;
            if (!allowMyMsg && k == ChatCrowdKind.MyMsg) continue;
            if (!allowToxic && k == ChatCrowdKind.Toxic) continue;

            float w = Mathf.Max(0f, kindWeights[i].weightMultiplier);
            total += w;
        }

        if (total <= 0f)
            return ChatCrowdKind.Cheer;

        float roll = Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < kindWeights.Length; i++)
        {
            var k = kindWeights[i].kind;
            if (!allowMyMsg && k == ChatCrowdKind.MyMsg) continue;
            if (!allowToxic && k == ChatCrowdKind.Toxic) continue;

            float w = Mathf.Max(0f, kindWeights[i].weightMultiplier);
            acc += w;
            if (roll < acc)
                return k;
        }

        return kindWeights[kindWeights.Length - 1].kind;
    }
}