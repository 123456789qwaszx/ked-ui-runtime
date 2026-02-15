// ChatStreamController.cs
// 데이터 기반 채팅 생성 컨트롤러 (DB → Selector → Factory → ChatRail)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChatStreamController : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private ChatSpecDB database;

    [Header("Target")]
    [SerializeField] private ChatRail chatRail;

    [Header("Stream Settings")]
    [Tooltip("자동 채팅 생성 간격 (초)")]
    [SerializeField] private float autoStreamInterval = 2f;
    
    [Tooltip("자동 채팅 활성화")]
    [SerializeField] private bool enableAutoStream = false;

    [Header("Query Conditions")]
    [Tooltip("기본 조건 태그 (비워두면 전체)")]
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

    // ========== Public API ==========

    /// <summary>
    /// 랜덤 채팅 1개 생성
    /// </summary>
    public void GenerateRandomChat(ChatEntryKind? kind = null, params string[] conditions)
    {
        if (!database)
        {
            Debug.LogError("[ChatStreamController] Database not assigned.", this);
            return;
        }

        // 조건에 맞는 스펙 쿼리
        var specs = database.Query(kind, conditions);
        if (specs == null || specs.Count == 0)
        {
            Debug.LogWarning($"[ChatStreamController] No specs found for conditions: {string.Join(", ", conditions)}");
            return;
        }

        // 가중치 랜덤 선택
        var selectedSpec = ChatSpecSelector.SelectRandom(specs);
        if (!selectedSpec)
        {
            Debug.LogError("[ChatStreamController] SelectRandom failed.");
            return;
        }

        // Factory로 Data 생성
        var data = ChatEntryFactory.Create(selectedSpec);

        // ChatRail에 푸시
        if (chatRail)
            chatRail.Push(data);
    }

    /// <summary>
    /// 여러 개 랜덤 채팅 생성
    /// </summary>
    public void GenerateRandomChats(int count, ChatEntryKind? kind = null, params string[] conditions)
    {
        if (!database || !chatRail)
        {
            Debug.LogError("[ChatStreamController] Database or ChatRail not assigned.", this);
            return;
        }

        var specs = database.Query(kind, conditions);
        if (specs == null || specs.Count == 0)
        {
            Debug.LogWarning($"[ChatStreamController] No specs found.");
            return;
        }

        // 여러 개 선택 (중복 허용)
        var selectedSpecs = ChatSpecSelector.SelectRandomMultiple(specs, count);

        // Factory로 Data 생성
        var dataArray = ChatEntryFactory.CreateMultiple(selectedSpecs.ToArray());

        // ChatRail에 푸시
        chatRail.PushMultiple(dataArray);
    }

    /// <summary>
    /// 특정 ID로 채팅 생성
    /// </summary>
    public void GenerateById(string specId)
    {
        if (!database || !chatRail)
        {
            Debug.LogError("[ChatStreamController] Database or ChatRail not assigned.", this);
            return;
        }

        var spec = database.GetById(specId);
        if (!spec)
        {
            Debug.LogWarning($"[ChatStreamController] Spec not found: {specId}");
            return;
        }

        var data = ChatEntryFactory.Create(spec);
        chatRail.Push(data);
    }

    // ========== Auto Stream ==========

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

            // 기본 조건으로 랜덤 채팅 생성
            GenerateRandomChat(null, defaultConditions);
        }
    }

    // ========== Debug Helpers ==========

    [ContextMenu("🎲 Generate 1 Random Chat")]
    private void DebugGenerateOne()
    {
        GenerateRandomChat();
    }

    [ContextMenu("🎲 Generate 5 Random Chats")]
    private void DebugGenerateFive()
    {
        GenerateRandomChats(5);
    }

    [ContextMenu("📊 Print Weight Distribution")]
    private void DebugPrintWeights()
    {
        if (!database)
        {
            Debug.LogError("[ChatStreamController] Database not assigned.");
            return;
        }

        var specs = database.Query(null, defaultConditions);
        ChatSpecSelector.PrintWeightDistribution(specs);
    }
}