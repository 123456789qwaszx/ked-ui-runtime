using UnityEngine;

public sealed class BroadcastHubDmPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BroadcastHubUIRoot hub;
    [SerializeField] private DmThreadPanel _panel;

    [Header("Test Script")]
    [TextArea(3, 20)]
    [SerializeField] private string testJson; // JsonUtility로 읽을 테스트 스크립트

    private readonly DmThreadPlayer _player = new();
    private bool _bound;

    private void Awake()
    {
        if (!hub)
        {
            Debug.LogError("[BroadcastHubDmPresenter] hub is null.", this);
            enabled = false;
            return;
        }

        // DmThreadPanel 가져오기
        var panel = _panel;
        if (!panel)
        {
            Debug.LogError("[BroadcastHubDmPresenter] DmThreadPanel is null. Check Refs.DmThreadPanel_Widget.", this);
            enabled = false;
            return;
        }

        _player.Initialize(panel);

        // (선택) 결과 로그 확인
        _player.OnResult += e =>
        {
            Debug.Log($"[DM RESULT] kind={e.Kind} thread={e.ThreadId} event={e.EventId} choice={e.ChoiceId}");
        };
    }

    private void OnEnable()
    {
        if (_bound) return;
        _bound = true;

        hub.OnTabDmRequested += HandleTabDm;
    }

    private void OnDisable()
    {
        if (!_bound) return;
        _bound = false;

        hub.OnTabDmRequested -= HandleTabDm;
    }

    private void Update()
    {
        // DM 재생 중 탭/스페이스로 진행 확인용
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            _player.RequestAdvance();
    }

    private void HandleTabDm()
    {
        // 탭이 DM으로 바뀌면, 테스트 스크립트 시작
        var script = BuildScript();
        _player.StartDm(script, clearUi: true);
    }

    private DmScript BuildScript()
    {
        // 1) 인스펙터에 JSON을 넣어둔 경우
        if (!string.IsNullOrWhiteSpace(testJson))
        {
            try
            {
                return JsonUtility.FromJson<DmScript>(testJson);
            }
            catch
            {
                Debug.LogWarning("[BroadcastHubDmPresenter] Failed to parse testJson. Fallback to hardcoded script.", this);
            }
        }

        // 2) 하드코딩 fallback
        return new DmScript
        {
            threadId = "dm.ravi.main",
            version = "1",
            events = new[]
            {
                new DmEvent
                {
                    id = "m0",
                    kind = DmEventKind.Marker,
                    marker = new DmMarker { label = "DAY 7 - 밤" }
                },
                new DmEvent
                {
                    id = "l0",
                    kind = DmEventKind.Line,
                    line = new DmLine { kind = DmEntryKind.Incoming, speaker = "라비", text = "지금… 시간 돼?" }
                },
                new DmEvent
                {
                    id = "e0",
                    kind = DmEventKind.Emoji,
                    emoji = new DmEmoji { kind = DmEntryKind.Incoming, speaker = "라비", emojiId = "heart" }
                },
                new DmEvent
                {
                    id = "c0",
                    kind = DmEventKind.Choice,
                    choice = new DmChoice
                    {
                        prompt = "어떻게 답할까?",
                        options = new[]
                        {
                            new DmChoiceOption { id = "yes", text = "응, 무슨 일이야?", gotoEventId = "l1" },
                            new DmChoiceOption { id = "no",  text = "지금은 좀 바빠…", gotoEventId = "l2" },
                        }
                    }
                },
                new DmEvent
                {
                    id = "l1",
                    kind = DmEventKind.Line,
                    line = new DmLine { kind = DmEntryKind.Outgoing, speaker = "나", text = "응, 무슨 일이야?" }
                },
                new DmEvent
                {
                    id = "l2",
                    kind = DmEventKind.Line,
                    line = new DmLine { kind = DmEntryKind.Outgoing, speaker = "나", text = "지금은 좀 바빠…" }
                },
                new DmEvent
                {
                    id = "l3",
                    kind = DmEventKind.Line,
                    line = new DmLine { kind = DmEntryKind.Incoming, speaker = "라비", text = "알겠어. 그냥… 목소리 듣고 싶었어." }
                },
            }
        };
    }
}