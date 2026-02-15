// using UnityEngine;
//
// public class TestLauncher : MonoBehaviour
// {
//     private LiveChatBindings _liveFlowController;
//     
//     [SerializeField]private ChatRail chatRail;
//     [Tooltip("LiveUI")]
//     [SerializeField] private KeyCode liveKey = KeyCode.Alpha1;
//     
//     [SerializeField] private KeyCode liveStartKey = KeyCode.Alpha3;
//     
//     [SerializeField] private KeyCode endChatKey = KeyCode.Alpha6;
//     
//     private bool _init;
//
//     public void Initialize(LiveChatBindings liveChatBindings)
//     {
//         if(_init) return;
//         
//         _liveFlowController = liveChatBindings;
//         _init = true;
//     }
//     private void Update()
//     {
//         if(!_init)
//             return;
//         
//         if (Input.GetKeyDown(liveKey))
//             StartLive();
//         
//         if (Input.GetKeyDown(KeyCode.Alpha2))
//             PlayWelcomeSequence();
//         
//         if (Input.GetKeyDown(liveStartKey))
//             PushTestLiveChat();
//         
//         if (Input.GetKeyDown(endChatKey))
//             EndChat();
//     }
//     
//     public void StartLive()
//     {
//         UIManager.Instance.SwitchRootPatched<LiveUIRoot>();
//         LiveUIRoot liveUIRoot = UIManager.Instance.GetUI<LiveUIRoot>();
//         _liveFlowController.BindLiveUIRoot(liveUIRoot);
//     }
//     
//     public void PushTestLiveChat()
//     {
//         chatRail.PushIdol("…방송을 시작합니다.");
//     }
//
//     public void EndChat()
//     {
//         chatRail.Push(new ChatEntryData
//         {
//             type = ChatEntryType.System,
//             side = ChatEntrySide.Other,
//             chatName = "",
//             chatBody = "방송을 종료합니다…",
//         });
//     }
//     
//
//     private void PlayWelcomeSequence()
//     {
//         chatRail.PushIdol("…방송을 시작합니다.");
//     }
//     
//     
//     [Header("Welcome Messages")]
//     [SerializeField] private string[] welcomeMessages = 
//     {
//         "안녕! 오늘도 와줘서 고마워!",
//         "채팅은 휴대폰 화면에 올라오는 걸로 테스트 중이야."
//     };
// }
