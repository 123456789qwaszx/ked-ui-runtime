using System.Collections;
using UnityEngine;

public sealed class LiveSession : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChatSystem chat; 

    [Header("Test Sequence")]
    [SerializeField] private bool autoStartTestSequence = true;
    [SerializeField] private float testSequenceDelay = 1f;

    private void Start()
    {
        if (autoStartTestSequence)
            StartCoroutine(TestSequence());
    }

    private IEnumerator TestSequence()
    {
        yield return new WaitForSeconds(testSequenceDelay);

        Debug.Log("=== [LiveSession] Test Sequence Start ===");

        Debug.Log("[LiveSession] 1. 평온 시작 (Preset A)");
        chat.SetPreset(ChatPreset.A);
        chat.EmitWave(3);
        yield return new WaitForSeconds(2f);

        Debug.Log("[LiveSession] 4. 후원 발생 (1000원)");
        SubmitDonationForTest(1000, "고마워요!");
        yield return new WaitForSeconds(3f);

        Debug.Log("[LiveSession] 5. 분위기 고조 (Preset C)");
        chat.SetPreset(ChatPreset.C);
        chat.EmitWave(5);
        yield return new WaitForSeconds(2f);

        Debug.Log("[LiveSession] 6. 다시 평온으로 (Preset A)");
        chat.SetPreset(ChatPreset.A);
        chat.EmitWave(3);

        Debug.Log("=== [LiveSession] Test Sequence End ===");
    }

    // ========== Donation 연출 (P0 핵심) ==========

    public void SubmitDonationForTest(int amount, string message)
    {
        StartCoroutine(DonationSequence(amount, message));
    }

    private IEnumerator DonationSequence(int amount, string message)
    {
        Debug.Log($"[LiveSession] Donation Sequence Start: {amount}원, {message}");

        chat.SetPreset(ChatPreset.B);
        chat.PushMyMsg(message, amount);
        chat.EmitWave(3);

        float delay = Random.Range(0.25f, 0.85f);
        yield return new WaitForSeconds(delay);

        Debug.Log("[LiveSession] (TODO) Play Stinger");

        // 아이돌 즉답은 나중에 IdolPresenter 붙이면 여기서 호출
        Debug.Log($"[LiveSession] Idol Reply: 고마워요! {amount}원!");

        yield return new WaitForSeconds(2f);
        chat.SetPreset(ChatPreset.A);

        Debug.Log("[LiveSession] Donation Sequence End");
    }
}
