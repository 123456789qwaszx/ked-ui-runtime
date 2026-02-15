using UnityEngine;

public sealed class LiveStreamBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveUIRoot liveUI;
    [SerializeField] private TestLauncher testLauncher;
    [SerializeField] private ChatEngine chatEngine;

    LiveChatBindings _liveChatBindings;
    
    private void Awake()
    {
        _liveChatBindings = new (chatEngine);
        _liveChatBindings.BindLiveUIRoot(liveUI);
        
        testLauncher.Initialize(_liveChatBindings, chatEngine);
    }
    private void OnDestroy()
    {
        _liveChatBindings?.Dispose();
        _liveChatBindings = null;
    }
}