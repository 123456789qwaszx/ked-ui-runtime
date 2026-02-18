using System;
using UnityEngine;

public sealed class LiveStreamBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LiveUIRoot liveUI;
    [SerializeField] private BroadcastEndPanel endPanel;
    
    [SerializeField] private TestLauncher testLauncher;
    [SerializeField] private ChatEngine chatEngine;

    private LiveChatBindings _liveChatBindings;
    private BroadcastEndBindings _endBindings;

    private BroadcastEventLogRecorder _recorder;
    private SimpleIdolReactor _idol;
    
    private InMemoryBroadcastStore _store;
    private BroadcastEndPipeline _endPipeline;

    private void Awake()
    {
        _recorder = new BroadcastEventLogRecorder();
        _idol = new SimpleIdolReactor();

        // 영속 상태 및 로그 저장소(P0: 메모리)
        _store = new InMemoryBroadcastStore();

        // 종료 파이프라인 구성(P0 기본값)
        _endPipeline = BuildEndPipelineP0();

        // 1) EndPanel bindings 먼저
        _endBindings = new BroadcastEndBindings(
            onContinue: HandleEndContinue,
            onClose: HandleEndClose
        );
        _endBindings.Bind(endPanel);
        endPanel.SetVisible(false);

        // 2) onEnded 콜백을 "패널 표시"로 바꿈
        Action<BroadcastEndResult> onEnded = HandleBroadcastEnded;
        
        ChatEngineDeps deps = new(
            store: _store,
            recorder: _recorder,
            idolReactor: _idol,
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
        var delta = new BroadcastScoringRules();
        var token = new TokenRuleset();
        var eval = new EvaluationRuleset();
        var night = new DefaultNightEventCatalog();
        var contract = new ContractBuilder();

        return new BroadcastEndPipeline(delta, token, eval, night, contract);
    }
    private void HandleBroadcastEnded(BroadcastEndResult result)
    {
        if (!endPanel) return;

        // UX: 정산 중엔 입력 막기 (선택)
        liveUI?.SetButtonsInteractable(false);
        liveUI?.SetOverlayDimVisible(true);

        // 패널 갱신/표시
        endPanel.SetData(result);
        endPanel.SetVisible(true);
    }

    private void HandleEndClose()
    {
        liveUI?.SetOverlayDimVisible(false);
        liveUI?.SetButtonsInteractable(true);

        // close는 "그냥 닫기" 정도
    }

    private void HandleEndContinue(string nightEventKey)
    {
        liveUI?.SetOverlayDimVisible(false);
        liveUI?.SetButtonsInteractable(true);

        // 여기서 nightEventKey로 흐름 결정:
        // - CPS route 시작
        // - 씬 전환
        // - 밤 이벤트 팝업 생성 등
        Debug.Log($"[Flow] Continue with nightEventKey={nightEventKey}");
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