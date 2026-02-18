using System;
using UnityEngine;

public sealed class LiveStreamBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveUIRoot liveUI;
    [SerializeField] private TestLauncher testLauncher;
    [SerializeField] private ChatEngine chatEngine;

    private LiveChatBindings _liveChatBindings;

    private InMemoryBroadcastLogRepository _repo;
    private BroadcastEventLogRecorder _recorder;
    private SimpleIdolReactor _idol;
    
    private InMemoryBroadcastStateRepository _stateRepo;
    private BroadcastEndPipeline _endPipeline;

    private void Awake()
    {
        _repo = new InMemoryBroadcastLogRepository();
        _recorder = new BroadcastEventLogRecorder();
        _idol = new SimpleIdolReactor();

        // 영속 상태 저장소(P0: 메모리)
        _stateRepo = new InMemoryBroadcastStateRepository();

        // 종료 파이프라인 구성(P0 기본값)
        _endPipeline = BuildEndPipelineP0();

        // 결과 콜백(P0: 콘솔 출력 or UI 전달)
        Action<BroadcastEndResult> onEnded = DumpEndResultToConsole;
        
        ChatEngineDeps deps = new(
            repository: _repo,
            recorder: _recorder,
            idolReactor: _idol,
            stateRepository: _stateRepo,
            endPipeline: _endPipeline,
            onEventEnded: onEnded
        );

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
    
    private static BroadcastEndPipeline BuildEndPipelineP0()
    {
        var delta = new DeltaRuleset();
        var token = new TokenRuleset();
        var eval = new EvaluationRuleset();
        var night = new DefaultNightEventCatalog();
        var contract = new ContractBuilder();

        return new BroadcastEndPipeline(delta, token, eval, night, contract);
    }

    private static void DumpEndResultToConsole(BroadcastEndResult result)
    {
        Debug.Log($"[BroadcastEnd] {result.recap.summaryText}");

        if (result.recap.changes != null)
        {
            for (int i = 0; i < result.recap.changes.Length; i++)
            {
                var c = result.recap.changes[i];
                Debug.Log($"- {c.label} {c.delta:+#;-#;0} {c.causeText}");
            }
        }

        Debug.Log($"[Eval] {result.evaluation.grade} note={result.evaluation.noteText}");
        Debug.Log($"[Night] {result.nightEvent.kind} key={result.nightEvent.eventKey} teaser={result.nightEvent.teaserText}");
        Debug.Log($"[Next] {result.nextContract.titleText}");
    }
}