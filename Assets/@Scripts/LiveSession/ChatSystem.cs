using UnityEngine;

public sealed class ChatSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChatSpecDB database;
    [SerializeField] private ChatRail chatRail;

    [Header("Preset Library")]
    [SerializeField] private ChatPresetLibrarySO presetLibrary;

    private readonly LiveChatState _ruleState = new();
    private ChatPresetSO _currentPreset;

    public void SetPreset(ChatPreset presetKey)
    {
        _currentPreset = presetLibrary.Get(presetKey);
        if (!_currentPreset)
        {
            Debug.LogError($"[ChatSystem] Preset missing in library: {presetKey}", this);
        }
    }

    public void EmitWave(int count)
    {
        if (count <= 0) return;
        if (!_ruleState.CanEmitWave()) return;

        if (!_currentPreset)
        {
            Debug.LogError("[ChatSystem] currentPreset is null. Call SetPreset first.", this);
            return;
        }

        bool allowToxic = _currentPreset.allowToxicRefrain;
        bool allowMyMsg = _currentPreset.allowMyMsgHighlight && _currentPreset.myMsgRatio > 0f;

        int forcedMyMsg = 0;
        if (allowMyMsg)
            forcedMyMsg = Mathf.Clamp(Mathf.CeilToInt(count * _currentPreset.myMsgRatio), 0, count);

        for (int i = 0; i < count; i++)
        {
            bool forceMy = (i < forcedMyMsg);

            // 1단계: crowdKind 선택
            ChatCrowdKind crowdKind = forceMy
                ? ChatCrowdKind.MyMsg
                : _currentPreset.RollCrowdKind(allowMyMsg: allowMyMsg, allowToxic: allowToxic);

            // 2단계: crowdKind 풀에서 weight 기반 선택
            var picked = PickSpecByCrowdKindWeighted(
                crowdKind: crowdKind,
                entryType: ChatEntryType.Chat,
                requiredConditions: null
            );

            if (!picked) continue;

            chatRail.Push(ChatEntryFactory.Create(picked));
        }

        _ruleState.ConsumeWave();
    }

    public void PushMyMsg(string message, int amount)
    {
        var data = new ChatEntryData
        {
            type = ChatEntryType.Chat,
            side = ChatEntrySide.My,
            crowdKind = ChatCrowdKind.MyMsg,
            chatName = "나",
            chatBody = message,
            donationAmount = amount,
        };

        chatRail.Push(data);
    }

    private ChatSpecSO PickSpecByCrowdKindWeighted(ChatCrowdKind crowdKind, ChatEntryType entryType, string[] requiredConditions)
    {
        var list = database.GetByCrowdKind(crowdKind);
        if (list == null || list.Count == 0)
            return null;

        float total = 0f;

        // 1) 조건/타입 필터 + total weight
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (!s) continue;

            if (s.entryType != entryType) continue;
            if (!ChatSpecDB.MatchesAllConditions(s, requiredConditions)) continue;

            float w = Mathf.Max(0f, s.weight);
            total += w;
        }

        if (total <= 0f)
            return null;

        // 2) roll
        float roll = Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (!s) continue;

            if (s.entryType != entryType) continue;
            if (!ChatSpecDB.MatchesAllConditions(s, requiredConditions)) continue;

            float w = Mathf.Max(0f, s.weight);
            acc += w;
            if (roll < acc)
                return s;
        }

        // 3) fallback
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var s = list[i];
            if (!s) continue;

            if (s.entryType != entryType) continue;
            if (!ChatSpecDB.MatchesAllConditions(s, requiredConditions)) continue;
            if (s.weight > 0f) return s;
        }

        return null;
    }
}