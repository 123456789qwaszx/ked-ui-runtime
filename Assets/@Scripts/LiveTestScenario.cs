// LiveTestScenarioSO.cs
// 재현 가능한 테스트 시나리오 데이터
using UnityEngine;

[CreateAssetMenu(fileName = "LiveTestScenario", menuName = "Dev/Live Test Scenario", order = 100)]
public sealed class LiveTestScenarioSO : ScriptableObject
{
    [System.Serializable]
    public struct Step
    {
        [Tooltip("시나리오 시작 후 몇 초에 실행할지")]
        public float time;

        public ChatEntryKind kind;

        [Tooltip("발신자 이름 (Chat/Donation에서 사용)")]
        public string name;

        [Tooltip("메시지 내용")]
        public string body;

        [Tooltip("후원 금액 (Donation에서만 사용)")]
        public int donationAmount;

        [Tooltip("내 메시지인가? (Chat/Donation에서 사용)")]
        public bool isMy;
    }

    [Header("Scenario Steps")]
    [Tooltip("시간 순서대로 정렬 권장")]
    public Step[] steps;

    [Header("Options")]
    [Tooltip("시나리오 끝나면 다시 반복")]
    public bool loop = false;

    [Tooltip("재생 속도 배율 (1.0 = 정상, 2.0 = 2배속)")]
    [Range(0.1f, 10f)]
    public float speedMultiplier = 1f;

    [Tooltip("랜덤 시드 (재현성 보장, 0이면 무시)")]
    public int randomSeed = 0;
}