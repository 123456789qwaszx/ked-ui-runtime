// LiveSession.cs
// 라이브 방송 전체 흐름 제어 (Orchestrator)
using System.Collections;
using UnityEngine;

public sealed class LiveSession : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveEventBus eventBus;

    [Header("Test Sequence")]
    [SerializeField] private bool autoStartTestSequence = true;
    [SerializeField] private float testSequenceDelay = 1f;

    private void Start()
    {
        // EventBus 검증
        if (!eventBus)
        {
            eventBus = LiveEventBus.Instance;
            if (!eventBus)
            {
                Debug.LogError("[LiveSession] LiveEventBus not found.", this);
                enabled = false;
                return;
            }
        }

        // 후원 이벤트 구독 (Donation → MYMSG → 스팅어 → 즉답)
        eventBus.OnDonationSubmitted += OnDonationSubmitted;

        // 테스트 시퀀스 자동 시작
        if (autoStartTestSequence)
            StartCoroutine(TestSequence());
    }

    private void OnDestroy()
    {
        if (!eventBus) return;
        eventBus.OnDonationSubmitted -= OnDonationSubmitted;
    }

    // ========== Test Sequence (P0 데모) ==========

    private IEnumerator TestSequence()
    {
        yield return new WaitForSeconds(testSequenceDelay);

        Debug.Log("=== [LiveSession] Test Sequence Start ===");

        // 1) 평온 시작 (Preset A, 3개)
        Debug.Log("[LiveSession] 1. 평온 시작 (Preset A)");
        eventBus.RaiseChatWave(ChatPreset.A, 3);
        yield return new WaitForSeconds(2f);

        // 2) 아이돌 확인형 멘트 (리프레인 윈도우 열림)
        Debug.Log("[LiveSession] 2. 아이돌 확인형 멘트");
        eventBus.RaiseIdolLine("check_001", ToneStage.Calm, isCheckIn: true);
        yield return new WaitForSeconds(1f);

        // 3) 리프레인 발생 (5초 내 1회만 가능)
        Debug.Log("[LiveSession] 3. 리프레인 발생");
        eventBus.RaiseChatRefrain("refrain_yes");
        yield return new WaitForSeconds(1f);

        // 3-1) 리프레인 재시도 (차단되어야 함)
        Debug.Log("[LiveSession] 3-1. 리프레인 재시도 (차단 예상)");
        eventBus.RaiseChatRefrain("refrain_yes");
        yield return new WaitForSeconds(2f);

        // 4) 후원 발생 (자동으로 Preset B 전환 + MYMSG)
        Debug.Log("[LiveSession] 4. 후원 발생 (1000원)");
        eventBus.RaiseDonationSubmitted(1000, "고마워요!");
        yield return new WaitForSeconds(3f);

        // 5) 분위기 고조 (Preset C, TOXIC 허용)
        Debug.Log("[LiveSession] 5. 분위기 고조 (Preset C)");
        eventBus.RaiseChatWave(ChatPreset.C, 5);
        yield return new WaitForSeconds(2f);

        // 6) 다시 평온으로 복귀 (Preset A)
        Debug.Log("[LiveSession] 6. 다시 평온으로 (Preset A)");
        eventBus.RaiseChatWave(ChatPreset.A, 3);

        Debug.Log("=== [LiveSession] Test Sequence End ===");
    }

    // ========== Donation 연출 (P0 핵심) ==========

    private void OnDonationSubmitted(int amount, string message)
    {
        // 후원 발생 → MYMSG 하이라이트 → 스팅어 → 아이돌 즉답
        StartCoroutine(DonationSequence(amount, message));
    }

    private IEnumerator DonationSequence(int amount, string message)
    {
        Debug.Log($"[LiveSession] Donation Sequence Start: {amount}원, {message}");

        // 1) Preset B로 전환 (MYMSG 중심)
        eventBus.RaisePresetChange(ChatPreset.B);

        // 2) MYMSG 하이라이트 (나의 후원 메시지)
        eventBus.RaiseMyMsg(message, amount);

        // 3) ChatWave B (MYMSG 비율 높게)
        eventBus.RaiseChatWave(ChatPreset.B, 3);

        // 4) 반박자~1박 딜레이 (0.25~0.85초)
        float delay = Random.Range(0.25f, 0.85f);
        yield return new WaitForSeconds(delay);

        // 5) 스팅어 재생 (TODO: AudioController 연동)
        Debug.Log("[LiveSession] (TODO) Play Stinger");
        // audioController.PlayDonationStinger(amount);

        // 6) 아이돌 즉답
        eventBus.RaiseIdolLine($"reply_thanks_{amount}", ToneStage.Bright, isCheckIn: false);
        Debug.Log($"[LiveSession] Idol Reply: 고마워요! {amount}원!");

        // 7) 2초 후 Preset A로 복귀
        yield return new WaitForSeconds(2f);
        eventBus.RaisePresetChange(ChatPreset.A);

        Debug.Log("[LiveSession] Donation Sequence End");
    }

    // ========== Manual Test Helpers ==========

    [ContextMenu("▶ Test: ChatWave A (3)")]
    private void TestChatWaveA()
    {
        eventBus.RaiseChatWave(ChatPreset.A, 3);
    }

    [ContextMenu("▶ Test: ChatWave B (5)")]
    private void TestChatWaveB()
    {
        eventBus.RaiseChatWave(ChatPreset.B, 5);
    }

    [ContextMenu("▶ Test: ChatWave C (5)")]
    private void TestChatWaveC()
    {
        eventBus.RaiseChatWave(ChatPreset.C, 5);
    }

    [ContextMenu("▶ Test: Idol CheckIn")]
    private void TestIdolCheckIn()
    {
        eventBus.RaiseIdolLine("check_test", ToneStage.Calm, isCheckIn: true);
    }

    [ContextMenu("▶ Test: Refrain")]
    private void TestRefrain()
    {
        eventBus.RaiseChatRefrain("refrain_yes");
    }

    [ContextMenu("▶ Test: Donation (1000)")]
    private void TestDonation()
    {
        eventBus.RaiseDonationSubmitted(1000, "테스트 후원이에요!");
    }
}