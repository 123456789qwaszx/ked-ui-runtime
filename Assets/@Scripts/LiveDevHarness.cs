// LiveDevHarness.cs (Updated)
// Dev/Editor-only: 데이터 기반 테스트 지원
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;

public sealed class LiveDevHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChatRail chatRail;
    [SerializeField] private ChatStreamController chatStreamController;

    [Header("Data-Driven Test (New)")]
    [Tooltip("데이터 기반 자동 채팅 테스트")]
    [SerializeField] private bool enableDataDrivenStream = false;
    [SerializeField] private float dataDrivenInterval = 1.5f;

    private Coroutine scenarioRoutine;
    private Coroutine autoFeedRoutine;
    private Coroutine dataDrivenRoutine;

    private void Start()
    {
        if (enableDataDrivenStream)
            StartDataDrivenStream();
    }

    [ContextMenu("▶ Data-Driven Stream")]
    public void StartDataDrivenStream()
    {
        if (!chatStreamController)
        {
            Debug.LogWarning("[LiveDevHarness] ChatStreamController not assigned.", this);
            return;
        }

        if (dataDrivenRoutine != null)
            StopCoroutine(dataDrivenRoutine);

        dataDrivenRoutine = StartCoroutine(DataDrivenStreamRoutine());
    }

    [ContextMenu("■ Stop Data-Driven Stream")]
    public void StopDataDrivenStream()
    {
        if (dataDrivenRoutine != null)
        {
            StopCoroutine(dataDrivenRoutine);
            dataDrivenRoutine = null;
        }
    }

    [ContextMenu("■ Stop All Tests")]
    public void StopAllTests()
    {
        StopDataDrivenStream();
    }

    [ContextMenu("🧹 Clear Chat")]
    public void ClearChat()
    {
        if (chatRail)
            chatRail.Clear();
    }

    private IEnumerator DataDrivenStreamRoutine()
    {
        if (!chatStreamController)
        {
            Debug.LogError("[LiveDevHarness] ChatStreamController not found.", this);
            yield break;
        }

        Debug.Log("[LiveDevHarness] Data-Driven Stream started.");

        while (enableDataDrivenStream)
        {
            yield return new WaitForSeconds(dataDrivenInterval);

            // ChatStreamController를 통해 데이터 기반 채팅 생성
            chatStreamController.GenerateRandomChat();
        }

        dataDrivenRoutine = null;
    }
}
#endif