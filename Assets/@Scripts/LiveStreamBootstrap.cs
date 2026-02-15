using UnityEngine;

public sealed class LiveStreamBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveUIRoot liveUI;
    [SerializeField] TestLauncher testLauncher;

    LiveChatBindings _liveChatBindings;
    
    private void Awake()
    {
        _liveChatBindings = new ();
        _liveChatBindings.BindLiveUIRoot(liveUI);
        
        testLauncher.Initialize(_liveChatBindings);
    }
    private void OnDestroy()
    {
        _liveChatBindings?.Dispose();
        _liveChatBindings = null;
    }
}