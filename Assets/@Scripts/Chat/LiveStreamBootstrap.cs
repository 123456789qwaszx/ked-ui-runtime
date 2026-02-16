using UnityEngine;

public sealed class LiveStreamBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveUIRoot liveUI;
    [SerializeField] private TestLauncher testLauncher;
    [SerializeField] private ChatEngine chatEngine;

    private LiveChatBindings _liveChatBindings;

    private IBroadcastLogRepository _repo;
    private IBroadcastEventRecorder _recorder;
    private IIdolReactor _idol;

    private void Awake()
    {
        _repo = new InMemoryBroadcastLogRepository();
        _recorder = new BroadcastEventRecorder();
        _idol = new SimpleIdolReactor();

        ChatEngineDeps deps = new (_repo, _recorder, _idol);

        chatEngine.Initialize(deps);

        _liveChatBindings = new LiveChatBindings(chatEngine);
        _liveChatBindings.BindLiveUIRoot(liveUI);

        testLauncher.Initialize(_liveChatBindings, chatEngine);
    }

    private void OnDestroy()
    {
        _liveChatBindings?.Dispose();
        _liveChatBindings = null;
    }
}