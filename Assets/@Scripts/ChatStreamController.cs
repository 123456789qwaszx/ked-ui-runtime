using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChatStreamController : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private ChatSpecDB database;

    [Header("Target")]
    [SerializeField] private ChatRail chatRail;

    [Header("Preset (for waves)")]
    [SerializeField] private ChatPresetSO presetA;
    [SerializeField] private ChatPresetSO presetB;
    [SerializeField] private ChatPresetSO presetC;

    [Header("Stream Settings")]
    [SerializeField] private float autoStreamInterval = 2f;
    [SerializeField] private bool enableAutoStream = false;

    [Header("Query Conditions")]
    [SerializeField] private string[] defaultConditions;

    private Coroutine streamRoutine;

    private void Start()
    {
        if (enableAutoStream)
            StartAutoStream();
    }

    private void OnDestroy()
    {
        StopAutoStream();
    }

    public void EmitWave(ChatPreset preset, int count, params string[] conditions)
    {
        if (!database || !chatRail)
        {
            Debug.LogError("[ChatStreamController] Database or ChatRail not assigned.", this);
            return;
        }

        ChatPresetSO presetSO = ResolvePreset(preset);
        if (!presetSO)
        {
            Debug.LogError($"[ChatStreamController] Preset asset missing: {preset}", this);
            return;
        }

        if (count <= 0)
            return;

        int forcedMyMsg = 0;
        bool allowMyMsg = presetSO.allowMyMsgHighlight && presetSO.myMsgRatio > 0f;
        if (allowMyMsg)
            forcedMyMsg = Mathf.Clamp(Mathf.CeilToInt(count * presetSO.myMsgRatio), 0, count);

        bool allowToxic = presetSO.allowToxicRefrain;

        for (int i = 0; i < count; i++)
        {
            bool forceMy = (i < forcedMyMsg);

            ChatCrowdKind crowdKind = forceMy
                ? ChatCrowdKind.MyMsg
                : presetSO.RollCrowdKind(allowMyMsg: allowMyMsg, allowToxic: allowToxic);

            var picked = PickSpecByCrowdKindWeighted(crowdKind, conditions);
            if (!picked)
                continue;

            chatRail.Push(ChatEntryFactory.Create(picked));
        }
    }

    private ChatSpecSO PickSpecByCrowdKindWeighted(ChatCrowdKind kind, string[] conditions)
    {
        List<ChatSpecSO> list = database.GetByCrowdKind(kind);
        if (list == null || list.Count == 0)
            return null;

        float total = 0f;

        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (!s) continue;
            if (s.entryType != ChatEntryType.Chat) continue; // 스트림은 기본 Chat만
            if (!ChatSpecDB.MatchesAllConditions(s, conditions)) continue;

            float w = Mathf.Max(0f, s.weight);
            total += w;
        }

        if (total <= 0f)
            return null;

        float roll = Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (!s) continue;
            if (s.entryType != ChatEntryType.Chat) continue;
            if (!ChatSpecDB.MatchesAllConditions(s, conditions)) continue;

            float w = Mathf.Max(0f, s.weight);
            acc += w;
            if (roll < acc)
                return s;
        }

        return null;
    }

    private ChatPresetSO ResolvePreset(ChatPreset preset)
    {
        switch (preset)
        {
            case ChatPreset.A: return presetA;
            case ChatPreset.B: return presetB;
            case ChatPreset.C: return presetC;
            default: return presetA;
        }
    }

    [ContextMenu("▶ Start Auto Stream")]
    public void StartAutoStream()
    {
        if (streamRoutine != null)
            StopAutoStream();

        streamRoutine = StartCoroutine(AutoStreamRoutine());
    }

    [ContextMenu("■ Stop Auto Stream")]
    public void StopAutoStream()
    {
        if (streamRoutine != null)
        {
            StopCoroutine(streamRoutine);
            streamRoutine = null;
        }
    }

    private IEnumerator AutoStreamRoutine()
    {
        while (enableAutoStream)
        {
            yield return new WaitForSeconds(autoStreamInterval);
            EmitWave(ChatPreset.A, 1, defaultConditions);
        }
    }
}