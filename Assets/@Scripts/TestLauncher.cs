using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    private LiveChatBindings _liveFlowController;
    
    [SerializeField]private ChatRail chatRail;
    [SerializeField]private IdolSpeechQueue idolQueue;
    
    [Tooltip("LiveUI")]
    [SerializeField] private KeyCode liveKey = KeyCode.Alpha1;
    
    [SerializeField] private KeyCode liveStartKey = KeyCode.Alpha3;
    
    [SerializeField] private KeyCode EnqueIdolChatKey = KeyCode.Alpha4;
    
    [SerializeField] private KeyCode StartIdolChatKey = KeyCode.Alpha5;
    
    [SerializeField] private KeyCode EndChatKey = KeyCode.Alpha6;
    
    private bool _init;

    public void Initialize(LiveChatBindings liveChatBindings)
    {
        if(_init) return;
        
        _liveFlowController = liveChatBindings;
        _init = true;
    }
    private void Update()
    {
        if(!_init)
            return;
        
        if (Input.GetKeyDown(liveKey))
            StartLive();
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
            PlayWelcomeSequence();
        
        if (Input.GetKeyDown(liveStartKey))
            PushTestLiveChat();
        
        if (Input.GetKeyDown(EnqueIdolChatKey))
            PushIdolChat();
        
        if (Input.GetKeyDown(StartIdolChatKey))
            StartIdolChat();
        
        if (Input.GetKeyDown(EndChatKey))
            EndChat();
    }
    
    public void StartLive()
    {
        UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
        LiveUIRoot liveUIRoot = UIManager.Instance.GetUI<LiveUIRoot>();
        _liveFlowController.BindLiveUIRoot(liveUIRoot);
    }
    
    public void PushTestLiveChat()
    {
        chatRail.PushIdol("…방송을 시작합니다.");
    }
    
    public void PushIdolChat()
    {
        idolQueue.Enqueue("채팅은 휴대폰 화면에 올라오는 걸로 테스트 중이야.");
    }
    
    public void StartIdolChat()
    {
        idolQueue.Play();
    }

    public void EndChat()
    {
        chatRail.Push(new ChatEntryData
        {
            kind = ChatEntryKind.System,
            side = ChatEntrySide.Other,
            name = "",
            body = "방송을 종료합니다…",
        });
    }
    

    private void PlayWelcomeSequence()
    {
        chatRail.PushIdol("…방송을 시작합니다.");

        // 환영 메시지 큐
        foreach (var msg in welcomeMessages)
        {
            if (!string.IsNullOrEmpty(msg))
                idolQueue.Enqueue(msg);
        }

        idolQueue.Play();
    }
    
    
    [Header("Welcome Messages")]
    [SerializeField] private string[] welcomeMessages = 
    {
        "안녕! 오늘도 와줘서 고마워!",
        "채팅은 휴대폰 화면에 올라오는 걸로 테스트 중이야."
    };
}
