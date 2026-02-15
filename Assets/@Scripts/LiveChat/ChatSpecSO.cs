using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ChatSpec", menuName = "Live/Chat Spec", order = 200)]
public sealed class ChatSpecSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("고유 ID (중복 방지). 비어있으면 자동 생성됨.")]
    public string id;

    [Header("Entry Type (UI)")]
    [FormerlySerializedAs("kind")]
    public ChatEntryType entryType = ChatEntryType.Chat;

    [Header("Crowd Kind (Mood)")]
    [Tooltip("군중 채팅 분위기 분류 (CHEER/TOXIC/MEME...). Donation/Idol/System은 보통 None.")]
    public ChatCrowdKind crowdKind = ChatCrowdKind.None;

    [Header("Content")]
    [Tooltip("발신자 이름 (비워두면 랜덤 생성)")]
    public string chatName;

    [TextArea(2, 5)]
    [Tooltip("메시지 내용")]
    public string body;

    [Tooltip("후원 금액 (Donation에서만 사용)")]
    public int donationAmount;

    [Header("Selection")]
    [Tooltip("선택 가중치 (높을수록 자주 나옴)")]
    [Range(0f, 100f)]
    public float weight = 1f;

    [Tooltip("조건 태그 (예: morning, high_viewer, event)")]
    public string[] conditions;

    [Header("Flags")]
    [Tooltip("내 메시지로 표시할지 (테스트/특수용)")]
    public bool isMy = false;

    [Tooltip("리프레인(반복 대사)용 스펙인지")]
    public bool isRefrain = false;

    [Tooltip("리프레인 그룹 ID (예: refrain_angel). 비어도 됨.")]
    public string refrainId;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString("N");

        if (entryType == ChatEntryType.Donation && donationAmount <= 0)
            Debug.LogWarning($"[ChatSpecSO] {name}: Donation amount should be > 0", this);

        if (entryType == ChatEntryType.Idol || entryType == ChatEntryType.System || entryType == ChatEntryType.Donation)
        {
            if (crowdKind != ChatCrowdKind.None)
                crowdKind = ChatCrowdKind.None;
        }

        if (entryType == ChatEntryType.Chat && crowdKind == ChatCrowdKind.None)
            crowdKind = isMy ? ChatCrowdKind.MyMsg : ChatCrowdKind.Cheer;

        if (isRefrain && string.IsNullOrEmpty(refrainId))
            refrainId = "refrain_default";
    }
}
