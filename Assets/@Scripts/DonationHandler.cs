using System.Collections;
using UnityEngine;

public sealed class DonationHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChatRail chatRail;
    [SerializeField] private IdolSpeechQueue idolQueue;
    
    [Header("Timing")]
    [SerializeField] private float minReactionDelay = 0.25f;
    [SerializeField] private float maxReactionDelay = 0.85f;
    
    [Header("Identity")]
    [SerializeField] private string myName = "나";
    [SerializeField] private string idolName = "라비";
    
    [Header("Donation Config")]
    [SerializeField] private int[] quickDonateAmounts = { 1000, 10000, 100000 };
    

    /// <summary>
    /// 후원 제출 (채팅 반영 + 아이돌 반응)
    /// </summary>
    public void SubmitDonation()
    {
        int amount = quickDonateAmounts != null && quickDonateAmounts.Length > 0 
            ? quickDonateAmounts[0] 
            : 1000;
        
        string donorName = myName;

        chatRail.PushDonation(donorName, amount, bodyOrEmpty: "", isMy: true);

        StartCoroutine(DonationReactionRoutine(donorName, amount));
    }

    private IEnumerator DonationReactionRoutine(string donorName, int amount)
    {
        // "반박자~1박" 느낌: 0.25~0.85초
        float delay = Random.Range(minReactionDelay, maxReactionDelay);
        yield return new WaitForSeconds(delay);

        // TODO: 사운드
        // audioController.PlayDonationSting(amount);

        // 아이돌 즉답
        if (idolQueue)
        {
            idolQueue.Enqueue($"{donorName}님, ₩{amount:N0} 고마워요!");
            idolQueue.Play();
        }
    }
    
    // ========== 나중에 확장 가능 ==========
    
    // public void SubmitDonationWithEffect(int amount, string donorName, EffectType effect) { ... }
    // public void PlayDonationRanking() { ... }
    // public void SetDonationTheme(ThemeType theme) { ... }
}