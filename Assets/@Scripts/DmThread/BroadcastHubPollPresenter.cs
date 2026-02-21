using UnityEngine;

public sealed class BroadcastHubPollPresenter : MonoBehaviour
{
    [SerializeField] private TestLauncher testLauncher;
    
    [Header("Refs")]
    [SerializeField] private BroadcastHubUIRoot hub;

    [Header("Right Panel (optional)")]
    [SerializeField] private PollDetailPanel pollDetailPanel;

    [Header("Dummy Reward Icons (optional)")]
    [SerializeField] private Sprite rewardIcon0;
    [SerializeField] private Sprite rewardIcon1;
    [SerializeField] private Sprite rewardIcon2;
    [SerializeField] private Sprite rewardIcon3;

    private bool _bound;
    private int _selected = -1;

    // Left option labels
    private readonly string[] _options =
    {
        "ASMR",
        "노래 연습",
        "Q&A",
        "게임 한 판",
    };

    // Right panel text (per option)
    private readonly string[] _titles =
    {
        "ASMR로 갈까?",
        "노래 연습으로 갈까?",
        "Q&A로 갈까?",
        "게임 한 판으로 갈까?",
    };

    private readonly string[] _subtitles =
    {
        "첫 코너 · 조용히 끌어들이기",
        "첫 코너 · 실력으로 승부",
        "첫 코너 · 소통으로 분위기 장악",
        "첫 코너 · 터뜨려서 유입 잡기",
    };

    private readonly string[] _descs =
    {
        // ASMR
        "방송 시작 10분은 ASMR로 잡고 갈게.\n" +
        "채팅 속도는 낮아지지만, 체류와 호감도가 안정적으로 오를 수 있어.",

        // 노래 연습
        "오프닝에서 한 곡 연습/리허설을 보여줘.\n" +
        "실력 어필로 시청자 유입이 늘 수 있지만, 실수하면 리스크가 올라갈 수도 있어.",

        // Q&A
        "오늘 첫 코너는 질문 받아서 바로 답변하는 Q&A.\n" +
        "분위기는 좋아지지만, 민감한 질문이 섞이면 독성/리스크가 튈 수 있어.",

        // 게임
        "오프닝부터 게임으로 달려서 텐션을 끌어올려.\n" +
        "유입/채팅은 확 뛰지만, 자극적인 흐름으로 리스크가 더 빨리 쌓일 수 있어.",
    };

    // Default (before selecting any option)
    private const string DefaultTitle = "오늘 방송 첫 코너를 고르자";
    private const string DefaultSubtitle = "ON AIR 투표 · 라비 채널";
    private const string DefaultDesc =
        "왼쪽에서 코너를 고르면, 방송 오프닝이 그 코너로 시작돼.\n" +
        "코너에 따라 채팅 분위기와 리스크/호감도 보정이 달라질 수 있어.";

    private void Awake()
    {
        if (!hub)
        {
            Debug.LogError("[BroadcastHubPollPresenter] hub is null.", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (_bound) return;
        _bound = true;

        hub.OnTabPollRequested += HandleTabPoll;
        hub.OnPollOptionSelected += HandleOptionSelected;
        hub.OnPollConfirmRequested += HandleConfirm;
    }

    private void OnDisable()
    {
        if (!_bound) return;
        _bound = false;

        hub.OnTabPollRequested -= HandleTabPoll;
        hub.OnPollOptionSelected -= HandleOptionSelected;
        hub.OnPollConfirmRequested -= HandleConfirm;
    }

    private void HandleTabPoll()
    {
        _selected = -1;

        // Left option bar
        hub.SetPollOptions(_options);

        // Right panel resolve
        EnsurePanel();

        if (pollDetailPanel)
        {
            pollDetailPanel.Show(true);

            // Default text (before selection)
            pollDetailPanel.SetTexts(DefaultTitle, DefaultSubtitle, DefaultDesc);

            // Reward header + baseline rewards
            pollDetailPanel.SetRewardHeader("보상/영향 (선택 전)");
            pollDetailPanel.SetRewardCount(3);
            pollDetailPanel.SetReward(0, rewardIcon0, "+? 시청자");
            pollDetailPanel.SetReward(1, rewardIcon1, "+? 호감도");
            pollDetailPanel.SetReward(2, rewardIcon2, "+? 리스크");

            // Confirm disabled until a choice is selected
            pollDetailPanel.SetConfirmInteractable(false, "선택해줘");
        }

        Debug.Log("[POLL] Open");
    }

    private void HandleOptionSelected(int index)
    {
        _selected = index;

        string label = ((uint)index < (uint)_options.Length) ? _options[index] : "";
        Debug.Log($"[POLL] Selected: {index} ({label})");

        EnsurePanel();

        if (!pollDetailPanel)
            return;

        // 선택지에 따라 우측 텍스트 변경
        if ((uint)index < (uint)_titles.Length)
            pollDetailPanel.SetTexts(_titles[index], _subtitles[index], _descs[index]);
        else
            pollDetailPanel.SetTexts(DefaultTitle, DefaultSubtitle, DefaultDesc);

        // Confirm 버튼 활성/문구
        if (index >= 0 && index < _options.Length)
            pollDetailPanel.SetConfirmInteractable(true, $"「{label}」로 방송 켤게");
        else
            pollDetailPanel.SetConfirmInteractable(false, "선택해줘");

        // 보상/영향도 선택에 따라 변경
        ApplyDummyRewardsByIndex(index);
    }

    private void HandleConfirm()
    {
        if (_selected < 0 || _selected >= _options.Length)
            return;

        string label = _options[_selected];
        Debug.Log($"[POLL] Confirmed: {_selected} ({label})");

        EnsurePanel();

        if (pollDetailPanel)
        {
            // 확정 후 잠금
            pollDetailPanel.SetConfirmInteractable(false, "확정 완료");
        }

        if (testLauncher)
            testLauncher.StartLive();
        // TODO: 여기서 실제 게임 로직(방송 시작/phase 진입/로그 기록)을 붙이면 됨
    }

    private void ApplyDummyRewardsByIndex(int index)
    {
        if (!pollDetailPanel) return;

        if (index < 0)
        {
            pollDetailPanel.SetRewardHeader("보상/영향 (선택 전)");
            pollDetailPanel.SetRewardCount(3);
            pollDetailPanel.SetReward(0, rewardIcon0, "+? 시청자");
            pollDetailPanel.SetReward(1, rewardIcon1, "+? 호감도");
            pollDetailPanel.SetReward(2, rewardIcon2, "+? 리스크");
            return;
        }

        pollDetailPanel.SetRewardCount(3);

        switch (index)
        {
            case 0: // ASMR
                pollDetailPanel.SetRewardHeader("보상/영향 (ASMR)");
                pollDetailPanel.SetReward(0, rewardIcon0, "+150 시청자");
                pollDetailPanel.SetReward(1, rewardIcon1, "+2 호감도");
                pollDetailPanel.SetReward(2, rewardIcon2, "+1 피로");
                break;

            case 1: // 노래 연습
                pollDetailPanel.SetRewardHeader("보상/영향 (노래 연습)");
                pollDetailPanel.SetReward(0, rewardIcon0, "+220 시청자");
                pollDetailPanel.SetReward(1, rewardIcon1, "+1 스킬");
                pollDetailPanel.SetReward(2, rewardIcon2, "+1 리스크");
                break;

            case 2: // Q&A
                pollDetailPanel.SetRewardHeader("보상/영향 (Q&A)");
                pollDetailPanel.SetReward(0, rewardIcon0, "+180 시청자");
                pollDetailPanel.SetReward(1, rewardIcon1, "+1 분석");
                pollDetailPanel.SetReward(2, rewardIcon2, "-1 독성");
                break;

            case 3: // 게임 한 판
                pollDetailPanel.SetRewardHeader("보상/영향 (게임)");
                pollDetailPanel.SetReward(0, rewardIcon0, "+260 시청자");
                pollDetailPanel.SetReward(1, rewardIcon1, "+1 혼돈");
                pollDetailPanel.SetReward(2, rewardIcon2, "+1 리스크");
                break;

            default:
                pollDetailPanel.SetRewardHeader("보상/영향");
                pollDetailPanel.SetReward(0, rewardIcon0, "+100");
                pollDetailPanel.SetReward(1, rewardIcon1, "+1");
                pollDetailPanel.SetReward(2, rewardIcon2, "-1");
                break;
        }
    }

    private void EnsurePanel()
    {
        if (!pollDetailPanel)
            pollDetailPanel = FindFirstObjectByType<PollDetailPanel>();
    }
}