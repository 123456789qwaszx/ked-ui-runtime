using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    private LiveChatBindings _liveFlowController;
    private ChatEngine _chatEngine;
    
    [Tooltip("LiveUI")]
    [SerializeField] private KeyCode liveKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode endKey = KeyCode.Alpha2;
    //[SerializeField] private KeyCode liveStartKey = KeyCode.Alpha3;
    //[SerializeField] private KeyCode endChatKey = KeyCode.Alpha6;
    
    private bool _init;

    public void Initialize(LiveChatBindings liveChatBindings, ChatEngine chatEngine)
    {
        if(_init) return;
        
        _liveFlowController = liveChatBindings;
        _chatEngine = chatEngine;
        _init = true;
    }
    private void Update()
    {
        if(!_init)
            return;
        
        if (Input.GetKeyDown(liveKey))
            StartLive();
        
        if (Input.GetKeyDown(endKey))
            EndLive();
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
    
    // public void StartLive()
    // {
    //     UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
    //     LiveUIRoot liveUIRoot = UIManager.Instance.GetUI<LiveUIRoot>();
    //     _liveFlowController.BindLiveUIRoot(liveUIRoot);
    //     
    //     _chatEngine.StartEngine();
    // }
}
