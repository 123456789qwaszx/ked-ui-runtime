using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    private LiveChatBindings _liveFlowController;
    private ChatEngine _chatEngine;
    
    [Tooltip("LiveUI")]
    [SerializeField] private KeyCode liveKey = KeyCode.Alpha1;
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
    }
    
    public void StartLive()
    {
        UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
        LiveUIRoot liveUIRoot = UIManager.Instance.GetUI<LiveUIRoot>();
        _liveFlowController.BindLiveUIRoot(liveUIRoot);
        
        _chatEngine.StartEngine();
    }
}
