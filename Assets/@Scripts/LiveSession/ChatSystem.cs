// ChatSystem.cs
// EventBus 구독 + Preset 적용 + Rule 관리 (통합 서비스)
using System.Collections.Generic;
using UnityEngine;

public sealed class ChatSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveEventBus eventBus;
    [SerializeField] private ChatSpecDB database;
    [SerializeField] private ChatRail chatRail;

    [Header("Presets")]
    [SerializeField] private ChatPresetSO presetA;
    [SerializeField] private ChatPresetSO presetB;
    [SerializeField] private ChatPresetSO presetC;

    [Header("Debug")]
    [SerializeField] private bool logEvents = true;

    private ChatRuleState ruleState = new();
    private ChatPresetSO currentPresetData;

    private void Awake()
    {
        // EventBus 검증
        if (!eventBus)
        {
            eventBus = LiveEventBus.Instance;
            if (!eventBus)
            {
                Debug.LogError("[ChatSystem] LiveEventBus not found.", this);
                enabled = false;
                return;
            }
        }

        // 참조 검증
        if (!database || !chatRail)
        {
            Debug.LogError("[ChatSystem] Database or ChatRail not assigned.", this);
            enabled = false;
            return;
        }

        // EventBus 구독
        eventBus.RequestChatWave += OnChatWaveRequested;
        eventBus.RequestChatRefrain += OnChatRefrainRequested;
        eventBus.RequestMyMsg += OnMyMsgRequested;
        eventBus.RequestPresetChange += OnPresetChangeRequested;
        eventBus.OnIdolLine += OnIdolLine;

        // 초기 프리셋
        SetPreset(ChatPreset.A);
    }

    private void OnDestroy()
    {
        if (!eventBus) return;

        eventBus.RequestChatWave -= OnChatWaveRequested;
        eventBus.RequestChatRefrain -= OnChatRefrainRequested;
        eventBus.RequestMyMsg -= OnMyMsgRequested;
        eventBus.RequestPresetChange -= OnPresetChangeRequested;
        eventBus.OnIdolLine -= OnIdolLine;
    }

    // ========== EventBus 핸들러 ==========

    private void OnChatWaveRequested(ChatPreset preset, int count)
    {
        if (logEvents)
            Debug.Log($"[ChatSystem] ChatWave requested: preset={preset}, count={count}");

        if (!ruleState.CanEmitWave())
        {
            if (logEvents)
                Debug.LogWarning("[ChatSystem] Wave cooldown active, skipping.");
            return;
        }

        SetPreset(preset);
        EmitWave(count);
        ruleState.ConsumeWave();
    }

    private void OnChatRefrainRequested(string refrainId)
    {
        if (logEvents)
            Debug.Log($"[ChatSystem] ChatRefrain requested: {refrainId}");

        if (!ruleState.CanEmitRefrain())
        {
            if (logEvents)
                Debug.LogWarning("[ChatSystem] Refrain window closed or already used.");
            return;
        }

        if (!currentPresetData.allowToxicRefrain && IsToxicRefrain(refrainId))
        {
            if (logEvents)
                Debug.LogWarning($"[ChatSystem] TOXIC refrain {refrainId} blocked in preset {ruleState.currentPreset}");
            return;
        }

        EmitRefrain(refrainId);
        ruleState.ConsumeRefrain();
    }

    private void OnMyMsgRequested(string message, int amount)
    {
        if (logEvents)
            Debug.Log($"[ChatSystem] MyMsg requested: message={message}, amount={amount}");

        // MYMSG 직접 푸시
        var data = new ChatEntryData
        {
            kind = ChatEntryKind.Chat,
            side = ChatEntrySide.My,
            name = "나",
            body = message,
        };

        chatRail.Push(data);
    }

    private void OnPresetChangeRequested(ChatPreset preset)
    {
        if (logEvents)
            Debug.Log($"[ChatSystem] Preset change requested: {preset}");

        SetPreset(preset);
    }

    private void OnIdolLine(string lineId, ToneStage tone, bool isCheckIn)
    {
        // 확인형 멘트면 리프레인 윈도우 열기
        if (isCheckIn)
        {
            ruleState.OpenRefrainWindow(5f);

            if (logEvents)
                Debug.Log($"[ChatSystem] IdolLine CheckIn detected: {lineId}, refrain window opened.");
        }
    }

    // ========== Preset 관리 ==========

    public void SetPreset(ChatPreset preset)
    {
        ruleState.currentPreset = preset;
        currentPresetData = preset switch
        {
            ChatPreset.A => presetA,
            ChatPreset.B => presetB,
            ChatPreset.C => presetC,
            _ => presetA,
        };

        if (!currentPresetData)
        {
            Debug.LogError($"[ChatSystem] Preset {preset} data not assigned!", this);
            currentPresetData = presetA; // Fallback
        }
    }

    // ========== Wave 생성 ==========

    private void EmitWave(int count)
    {
        if (count <= 0) return;

        // 1) DB에서 전체 쿼리 (조건 없음, 전체 풀)
        var specs = database.Query();
        if (specs == null || specs.Count == 0)
        {
            Debug.LogWarning("[ChatSystem] No specs in database.");
            return;
        }

        // 2) Effective Weight 계산 (원본 수정 X)
        var weightedSpecs = new List<(ChatSpecSO spec, float effectiveWeight)>();
        foreach (var spec in specs)
        {
            if (!spec) continue;

            float multiplier = currentPresetData.GetKindMultiplier(spec.kind);
            float effectiveWeight = spec.weight * multiplier;

            // 가중치 0이면 차단
            if (effectiveWeight > 0f)
                weightedSpecs.Add((spec, effectiveWeight));
        }

        if (weightedSpecs.Count == 0)
        {
            Debug.LogWarning("[ChatSystem] All specs blocked by preset weights.");
            return;
        }

        // 3) 가중치 랜덤 선택 (effectiveWeight 기반)
        for (int i = 0; i < count; i++)
        {
            var selected = SelectWeightedRandom(weightedSpecs);
            if (selected)
            {
                var data = ChatEntryFactory.Create(selected);
                chatRail.Push(data);
            }
        }
    }

    /// <summary>
    /// 가중치 기반 랜덤 선택 (effectiveWeight 사용)
    /// </summary>
    private ChatSpecSO SelectWeightedRandom(List<(ChatSpecSO spec, float effectiveWeight)> weightedSpecs)
    {
        if (weightedSpecs.Count == 0)
            return null;

        // 전체 가중치 합
        float totalWeight = 0f;
        foreach (var pair in weightedSpecs)
            totalWeight += pair.effectiveWeight;

        if (totalWeight <= 0f)
            return weightedSpecs[0].spec; // Fallback

        // 랜덤 선택
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var pair in weightedSpecs)
        {
            cumulative += pair.effectiveWeight;
            if (roll < cumulative)
                return pair.spec;
        }

        return weightedSpecs[weightedSpecs.Count - 1].spec;
    }

    // ========== Refrain 생성 ==========

    private void EmitRefrain(string refrainId)
    {
        var spec = database.GetById(refrainId);
        if (!spec)
        {
            Debug.LogWarning($"[ChatSystem] Refrain spec not found: {refrainId}");
            return;
        }

        var data = ChatEntryFactory.Create(spec);
        chatRail.Push(data);
    }

    private bool IsToxicRefrain(string refrainId)
    {
        var spec = database.GetById(refrainId);
        return spec && spec.conditions != null && System.Array.Exists(spec.conditions, c => c == "TOXIC");
    }

    // ========== Debug Helpers ==========

    [ContextMenu("📊 Print Current State")]
    private void DebugPrintState()
    {
        ruleState.PrintState();
    }

    [ContextMenu("🎲 Test Wave (Preset A, 3)")]
    private void DebugTestWave()
    {
        OnChatWaveRequested(ChatPreset.A, 3);
    }
}