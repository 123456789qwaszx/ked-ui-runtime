using UnityEngine;

public sealed class LiveFlowController : MonoBehaviour
{
    private readonly UIBindingContext _ctx = new();
    
    [Header("UI")]
    [SerializeField] private LiveUIRoot liveUI;

    [Header("Identity")]
    [SerializeField] private string myName = "나";
    [SerializeField] private string idolName = "라비";

    [Header("Donation")]
    [SerializeField] private int[] quickDonateAmounts = { 1000, 10000, 100000 };

    [SerializeField]private ChatRail chatRail;
    [SerializeField]private IdolSpeechQueue idolQueue;


    public void BindLiveUIRoot(LiveUIRoot liveUIRoot)
    {
        _ctx.Bind(liveUIRoot, l => l.OnExitRequested += HandleExit, l => l.OnExitRequested -= HandleExit);
        _ctx.Bind(liveUIRoot, l => l.OnDonateRequested += HandleDonateButton, l => l.OnDonateRequested -= HandleDonateButton);
    }

    private void OnDestroy()
    {
        if (!liveUI) return;
        liveUI.OnExitRequested -= HandleExit;
        liveUI.OnDonateRequested -= HandleDonateButton;
    }

    private void HandleExit()
    {
        chatRail.Push(new ChatEntryData
        {
            kind = ChatEntryKind.System,
            side = ChatEntrySide.Other,
            name = "",
            body = "방송을 종료합니다…",
        });

        // TODO: 타이틀로 복귀 호출
        Debug.Log("[LiveFlowController] Exit requested.", this);
    }

    private void HandleDonateButton()
    {
        int amount = quickDonateAmounts != null && quickDonateAmounts.Length > 0 
            ? quickDonateAmounts[0] 
            : 1000;
        SubmitDonation(amount, myName);
    }

    public void SubmitDonation(int amount, string donorName)
    {
        // 1) 내 후원 메시지 반영(채팅)
        chatRail.PushDonation(donorName, amount, bodyOrEmpty: "", isMy: true);

        // 2) 사운드 딜레이 규칙(반박자~1박) + 아이돌 즉답
        StartCoroutine(DonationReactionRoutine(donorName, amount));
    }

    private System.Collections.IEnumerator DonationReactionRoutine(string donorName, int amount)
    {
        // "반박자~1박" 느낌: 0.25~0.85초
        float delay = UnityEngine.Random.Range(0.25f, 0.85f);
        yield return new WaitForSeconds(delay);

        // TODO: DonationSting 재생
        // audio.PlayDonationSting(amount);

        // 아이돌 즉답
        idolQueue.Enqueue($"{donorName}님, ₩{amount:N0} 고마워요!");
        idolQueue.Play();
    }
}