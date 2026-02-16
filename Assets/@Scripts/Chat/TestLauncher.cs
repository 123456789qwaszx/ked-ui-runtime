using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    private LiveChatBindings _liveFlowController;
    private ChatEngine _chatEngine;
    private IPhaseProfileResolver _profileResolver;
    
    [SerializeField] private string _currentProfileKey;

    [Tooltip("LiveUI")]
    [SerializeField] private KeyCode liveKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode endKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode dumpKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode nextPhaseKey = KeyCode.Alpha4;
    
    //[SerializeField] private KeyCode liveStartKey = KeyCode.Alpha3;
    //[SerializeField] private KeyCode endChatKey = KeyCode.Alpha6;
    
    private int _phaseIndex;
    private bool _liveActive;

    private bool _init;

    public void Initialize(LiveChatBindings liveChatBindings, ChatEngine chatEngine)
    {
        if (_init) return;

        _liveFlowController = liveChatBindings;
        _chatEngine = chatEngine;
        
        _profileResolver = new SimplePhaseProfileResolver();
        _init = true;
    }

    private void Update()
    {
        if (!_init)
            return;

        if (Input.GetKeyDown(liveKey))
            StartLive();

        if (Input.GetKeyDown(endKey))
            EndLive();
        
        if (Input.GetKeyDown(dumpKey))
            DumpLast();
        
        if (Input.GetKeyDown(nextPhaseKey))
            NextPhase();
    }

    public void StartLive()
    {
        UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
        LiveUIRoot liveUIRoot = UIManager.Instance.GetUI<LiveUIRoot>();
        _liveFlowController.BindLiveUIRoot(liveUIRoot);

        _chatEngine.StartEngine();

        double now = Time.timeAsDouble;
        _chatEngine.BeginEvent("run_test_001", "live_test_01", 0, now);

        _phaseIndex = 0;
        _liveActive = true;

        _currentProfileKey = "Opening";
        _chatEngine.BeginPhase(_phaseIndex, "perf_01", _currentProfileKey, now);
        //_chatEngine.BeginPhase(_phaseIndex, phaseId: "perf_01", profileKeyAtEnter: "Opening", nowSec: now);
    }

    private void EndLive()
    {
        if (!_liveActive)
            return;

        double now = Time.timeAsDouble;

        _chatEngine.EndPhase(now);
        _chatEngine.EndEvent(now);

        _chatEngine.StopEngine();

        _liveActive = false;
        _phaseIndex = 0;
        _currentProfileKey = null;

        Debug.Log("[TestLauncher] Live ended and saved.");
    }


    
    private void DumpLast()
    {
        BroadcastEventLog log = _chatEngine.GetLastSavedEventLogOrNull();
        if (log == null)
        {
            Debug.Log("[TestLauncher] No saved BroadcastEventLog yet.");
            return;
        }

        int phaseCount = log.phases != null ? log.phases.Length : 0;

        Debug.Log(
            $"[BroadcastEventLog]\n" +
            $"- runId={log.runId}, eventId={log.eventId}, eventIndex={log.eventIndex}\n" +
            $"- started={log.startedAtSec:F2}, ended={log.endedAtSec:F2}, phases={phaseCount}\n" +
            $"- donation: count={log.donationCountTotal}, sum={log.donationSumTotal}\n" +
            $"- emojiTotal={log.emojiCountTotal}, chatLineTotal={log.chatLineCountTotal}\n" +
            $"- tags: instinct={log.instinctCountTotal}, analysis={log.analysisCountTotal}, chaos={log.chaosCountTotal}\n" +
            $"- idolReact: +{log.idolPositiveReactTotal} / -{log.idolNegativeReactTotal} / ={log.idolNeutralReactTotal}"
        );

        if (log.phases == null) return;

        for (int i = 0; i < log.phases.Length; i++)
        {
            PhaseLog p = log.phases[i];
            string decision = p.hasDecision
                ? $"{p.decision.kind} (optionId={p.decision.optionId}, accepted={p.decision.accepted})"
                : "None";

            Debug.Log(
                $"[PhaseLog #{p.phaseIndex}] phaseId={p.phaseId}, profile={p.profileKeyAtEnter}\n" +
                $"- started={p.startedAtSec:F2}, ended={p.endedAtSec:F2}\n" +
                $"- donation: count={p.donationCount}, sum={p.donationSum}\n" +
                $"- emoji={p.emojiCount}, chatLine={p.chatLineCount}\n" +
                $"- tags: instinct={p.instinctCount}, analysis={p.analysisCount}, chaos={p.chaosCount}\n" +
                $"- idolReact: +{p.idolPositiveReact} / -{p.idolNegativeReact} / ={p.idolNeutralReact}\n" +
                $"- decision: {decision}"
            );
        }
    }
    
    private void NextPhase()
    {
        if (!_liveActive)
        {
            Debug.Log("[TestLauncher] NextPhase ignored: live is not active.");
            return;
        }

        double now = Time.timeAsDouble;

        // 1) 이번 인터미션 선택(테스트용)
        PhaseDecisionKind kind = PhaseDecisionKind.BuildTrust;
        string optionId = "choice_trust_01";

        // 2) 현재 Phase에 decision 기록 (EndPhase 전에!)
        _chatEngine.RecordDecision(kind, optionId, accepted: true);

        // 3) 다음 profileKey 결정
        _currentProfileKey = _profileResolver.ResolveNextProfileKey(_currentProfileKey, kind);

        // 4) 현재 Phase 종료 후 다음 Phase 시작
        _chatEngine.EndPhase(now);
        _phaseIndex++;

        string nextPhaseId = _phaseIndex == 1 ? "perf_02" : $"perf_{_phaseIndex + 1:00}";
        _chatEngine.BeginPhase(_phaseIndex, nextPhaseId, _currentProfileKey, now);

        Debug.Log($"[TestLauncher] NextPhase -> phaseIndex={_phaseIndex}, profile={_currentProfileKey}");
    }
}