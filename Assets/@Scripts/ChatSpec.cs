// ChatSpecSO.cs
// 개별 채팅 스펙 데이터 (기획자가 Inspector에서 수정)
using UnityEngine;

[CreateAssetMenu(fileName = "ChatSpec", menuName = "Live/Chat Spec", order = 200)]
public sealed class ChatSpecSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("고유 ID (중복 방지)")]
    public string id;

    [Header("Entry Data")]
    public ChatEntryKind kind = ChatEntryKind.Chat;
    
    [Tooltip("발신자 이름 (비워두면 랜덤 생성)")]
    public string name;
    
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

    [Header("Advanced")]
    [Tooltip("내 메시지로 표시할지 (테스트용)")]
    public bool isMy = false;

    // Validation
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
            id = name.GetHashCode().ToString();
        
        if (kind == ChatEntryKind.Donation && donationAmount <= 0)
            Debug.LogWarning($"[ChatSpecSO] {name}: Donation amount should be > 0", this);
    }
}