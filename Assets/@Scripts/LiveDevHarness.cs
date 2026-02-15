#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;

public sealed class LiveDevHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DonationHandler donationHandler;
    [SerializeField] private ChatRail chatRail;

    [Header("Scenario Test")]
    [SerializeField] private LiveTestScenarioSO scenario;
    [SerializeField] private bool autoRunScenarioOnStart = false;

    [Header("Stress Test (Legacy)")]
    [SerializeField] private bool enableAutoFeed = false;
    [SerializeField] private float feedInterval = 0.33f;

    private Coroutine scenarioRoutine;
    private Coroutine autoFeedRoutine;
    
    private void Start()
    {
        if (autoRunScenarioOnStart && scenario)
            RunScenario();

        if (enableAutoFeed)
            RunStressTest();
    }

    // ========== Context Menu (인스펙터 우클릭) ==========

    [ContextMenu("▶ Run Scenario")]
    public void RunScenario()
    {
        if (!scenario)
        {
            Debug.LogWarning("[LiveDevHarness] No scenario assigned.", this);
            return;
        }

        if (scenarioRoutine != null)
            StopCoroutine(scenarioRoutine);

        scenarioRoutine = StartCoroutine(ScenarioRoutine());
    }

    [ContextMenu("■ Stop Scenario")]
    public void StopScenario()
    {
        if (scenarioRoutine != null)
        {
            StopCoroutine(scenarioRoutine);
            scenarioRoutine = null;
        }
    }

    [ContextMenu("▶ Stress Test (30 lines / 10 sec)")]
    public void RunStressTest()
    {
        if (autoFeedRoutine != null)
            StopCoroutine(autoFeedRoutine);

        autoFeedRoutine = StartCoroutine(AutoFeedRoutine());
    }

    [ContextMenu("■ Stop Stress Test")]
    public void StopStressTest()
    {
        if (autoFeedRoutine != null)
        {
            StopCoroutine(autoFeedRoutine);
            autoFeedRoutine = null;
        }
    }

    [ContextMenu("🧹 Clear Chat")]
    public void ClearChat()
    {
        if (chatRail)
            chatRail.Clear();
    }

    // ========== Scenario Execution ==========

    private IEnumerator ScenarioRoutine()
    {
        if (!chatRail)
        {
            Debug.LogError("[LiveDevHarness] ChatRail not found.", this);
            yield break;
        }

        // 랜덤 시드 설정
        if (scenario.randomSeed != 0)
            Random.InitState(scenario.randomSeed);

        float startTime = Time.time;
        int index = 0;

        do
        {
            while (index < scenario.steps.Length)
            {
                var step = scenario.steps[index];
                float targetTime = step.time / scenario.speedMultiplier;

                // 목표 시간까지 대기
                while (Time.time - startTime < targetTime)
                    yield return null;

                ExecuteStep(step);
                index++;
            }

            if (scenario.loop)
            {
                index = 0;
                startTime = Time.time;
            }

        } while (scenario.loop);

        scenarioRoutine = null;
    }

    private void ExecuteStep(LiveTestScenarioSO.Step step)
    {
        switch (step.kind)
        {
            case ChatEntryKind.Chat:
                chatRail.PushChat(step.name, step.body, step.isMy);
                break;

            case ChatEntryKind.Donation:
                // Donation은 Handler 통해서 처리 (reaction 포함)
                if (step.isMy && donationHandler)
                {
                    donationHandler.SubmitDonation();
                }
                else
                {
                    // 타인 후원은 직접 푸시
                    chatRail.PushDonation(step.name, step.donationAmount, step.body, step.isMy);
                }
                break;

            case ChatEntryKind.Idol:
                chatRail.PushIdol(step.body);
                break;

            case ChatEntryKind.System:
                chatRail.Push(new ChatEntryData
                {
                    kind = ChatEntryKind.System,
                    side = ChatEntrySide.Other,
                    name = "",
                    body = step.body,
                });
                break;
        }
    }

    // ========== Legacy Stress Test (Random Spam) ==========

    private IEnumerator AutoFeedRoutine()
    {
        if (!chatRail)
        {
            Debug.LogError("[LiveDevHarness] ChatRail not found.", this);
            yield break;
        }

        int i = 0;
        while (enableAutoFeed)
        {
            yield return new WaitForSeconds(feedInterval);

            // 랜덤한 시청자 채팅
            string viewer = "viewer_" + (i % 7);
            chatRail.PushChat(viewer, $"테스트 채팅 {i}", isMy: false);

            // 가끔 아이돌 말
            if (i % 5 == 0)
            {
                var idolQueue = GetComponent<IdolSpeechQueue>();
                if (idolQueue)
                    idolQueue.Enqueue($"(라비) 지금 {i}번째 채팅이야!");
            }

            i++;
        }

        autoFeedRoutine = null;
    }
}
#endif