using UnityEngine;

public class TestLauncher : MonoBehaviour
{
    [SerializeField]private LiveFlowController liveFlowController;
    
    [SerializeField]private ChatRail chatRail;
    [SerializeField]private IdolSpeechQueue idolQueue;
    
    
    [Tooltip("LiveUI")]
    [SerializeField] private KeyCode liveKey = KeyCode.Alpha1;
    
    [SerializeField] private KeyCode liveStartKey = KeyCode.Alpha3;
    
    [SerializeField] private KeyCode EnqueIdolChatKey = KeyCode.Alpha4;
    
    [SerializeField] private KeyCode StartIdolChatKey = KeyCode.Alpha5;
    
    [SerializeField] private KeyCode EndChatKey = KeyCode.Alpha6;
    
    
    private void Update()
    {
        if (Input.GetKeyDown(liveKey))
            StartLive();
        
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
        liveFlowController.BindLiveUIRoot(liveUIRoot);
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
}
