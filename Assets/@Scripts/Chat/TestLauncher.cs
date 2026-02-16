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
    
    
    [SerializeField] private bool autoAdvancePhase = true;

    private int _phaseIndex;
    private bool _liveActive;
    
    private BroadcastScenario _scenario;
    private double _phaseStartedAt;

    private bool _init;

    public void Initialize(LiveChatBindings liveChatBindings, ChatEngine chatEngine)
    {
        if (_init) return;

        _liveFlowController = liveChatBindings;
        _chatEngine = chatEngine;
        
        _profileResolver = new SimplePhaseProfileResolver();
        _scenario = BroadcastScenario.CreateTestDefault();
        
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
        
        if (_liveActive && autoAdvancePhase)
            AutoTick();
    }
    
    private void AutoTick()
    {
        if (!_scenario.TryGetPhase(_phaseIndex, out PhaseSpec current))
            return;

        if (current.durationSec <= 0f)
            return;

        double now = Time.timeAsDouble;
        double elapsed = now - _phaseStartedAt;

        if (elapsed >= current.durationSec)
            AdvancePhaseAuto();
    }
    
    private void AdvancePhaseAuto()
    {
        // 마지막 Phase면 종료
        if (_scenario.PhaseCount <= 0 || _phaseIndex >= _scenario.PhaseCount - 1)
        {
            EndLive();
            return;
        }

        double now = Time.timeAsDouble;

        // (테스트) 인터미션이 있는 Phase에서만 decision 기록 + 프로필 갱신
        if (_scenario.TryGetPhase(_phaseIndex, out PhaseSpec current) && current.hasIntermission)
        {
            PhaseDecisionKind kind = PhaseDecisionKind.BuildTrust;
            _chatEngine.RecordDecision(kind, "auto_choice_01", true);
            _currentProfileKey = _profileResolver.ResolveNextProfileKey(_currentProfileKey, kind);
        }

        _chatEngine.EndPhase(now);

        _phaseIndex++;

        if (!_scenario.TryGetPhase(_phaseIndex, out PhaseSpec next))
        {
            EndLive();
            return;
        }

        // 선택이 없었으면 baseProfileKey로 자연스럽게 리셋
        // 선택이 있었으면 resolver 결과(_currentProfileKey)가 유지됨
        if (!current.hasIntermission)
            _currentProfileKey = next.baseProfileKey;

        _chatEngine.BeginPhase(_phaseIndex, next.phaseId, _currentProfileKey, now);
        _phaseStartedAt = now;

        Debug.Log($"[TestLauncher] AutoPhase -> idx={_phaseIndex}, phaseId={next.phaseId}, profile={_currentProfileKey}");
    }

    public void StartLive()
    {
        UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
        LiveUIRoot liveUIRoot = UIManager.Instance.GetUI<LiveUIRoot>();
        _liveFlowController.BindLiveUIRoot(liveUIRoot);

        _chatEngine.StartEngine();

        _phaseIndex = 0;
        _liveActive = true;

        if (!_scenario.TryGetPhase(_phaseIndex, out PhaseSpec p0))
        {
            Debug.LogError("[TestLauncher] Scenario has no phases.");
            return;
        }

        _currentProfileKey = p0.baseProfileKey;

        double now = Time.timeAsDouble;
        _chatEngine.BeginEvent("run_test_001", _scenario.scenarioId, 0, now);
        _chatEngine.BeginPhase(_phaseIndex, p0.phaseId, _currentProfileKey, now);

        _phaseStartedAt = now;
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