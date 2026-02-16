using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    private LiveChatBindings _liveFlowController;
    private ChatEngine _chatEngine;

    [Tooltip("LiveUI")]
    [SerializeField] private KeyCode liveKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode endKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode dumpKey = KeyCode.Alpha3;
    
    //[SerializeField] private KeyCode liveStartKey = KeyCode.Alpha3;
    //[SerializeField] private KeyCode endChatKey = KeyCode.Alpha6;

    private bool _init;

    public void Initialize(LiveChatBindings liveChatBindings, ChatEngine chatEngine)
    {
        if (_init) return;

        _liveFlowController = liveChatBindings;
        _chatEngine = chatEngine;
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
    }

    public void StartLive()
    {
        UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
        LiveUIRoot liveUIRoot = UIManager.Instance.GetUI<LiveUIRoot>();
        _liveFlowController.BindLiveUIRoot(liveUIRoot);

        // 1) 엔진 켜기
        _chatEngine.StartEngine();

        // 2) 방송 세션 열기 + 첫 Phase 열기 (테스트용 하드코딩 OK)
        double now = Time.timeAsDouble;
        _chatEngine.BeginEvent(runId: "run_test_001", eventId: "live_test_01", eventIndex: 0, nowSec: now);
        _chatEngine.BeginPhase(phaseIndex: 0, phaseId: "perf_01", profileKeyAtEnter: "Opening", nowSec: now);
    }

    private void EndLive()
    {
        double now = Time.timeAsDouble;

        // Phase 닫고 Event 닫기
        _chatEngine.EndPhase(now);
        _chatEngine.EndEvent(now);

        _chatEngine.StopEngine();
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
}