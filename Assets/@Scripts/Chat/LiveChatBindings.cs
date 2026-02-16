using System;
using UnityEngine;

public sealed class LiveChatBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();

    private readonly ChatEngine _engine;
    private LiveUIRoot bound;

    public LiveChatBindings(ChatEngine engine)
    {
        _engine = engine;
    }

    public void BindLiveUIRoot(LiveUIRoot liveUIRoot)
    {
        if (!liveUIRoot) return;

        if (bound && bound != liveUIRoot)
            _ctx.Unbind(bound);

        bound = liveUIRoot;

        _ctx.Unbind(liveUIRoot);
        _ctx.Bind(liveUIRoot, l => l.OnExitRequested += HandleExit, l => l.OnExitRequested -= HandleExit);
        _ctx.Bind(liveUIRoot, l => l.OnDonateRequested += HandleDonateButton, l => l.OnDonateRequested -= HandleDonateButton);
        _ctx.Bind(liveUIRoot, l => l.OnSendEmojiRequested += HandleSendEmojiButton, l => l.OnSendEmojiRequested -= HandleSendEmojiButton);
        _ctx.Bind(liveUIRoot, l => l.OnSendChatRequested += HandleSendChatButton, l => l.OnSendChatRequested -= HandleSendChatButton);
    }

    public void Dispose()
    {
        _ctx.Unbind(bound);
        bound = null;
    }

    private void HandleExit()
    {
        // TODO: 씬 전환, 저장, 통계 등
        _engine.StopEngine();
        Debug.Log("[LiveFlowController] Exit requested.");
    }

    private void HandleDonateButton()
    {
        const int amount = 10000;

        // 1) 즉시 반응(연출/부스트)
        _engine.PushSignals(new ChatSignals(ChatSignalFlags.DonationHappened, donationAmount: amount));

        // 2) 기록
        _engine.RecordDonation(amount);

        Debug.Log("[LiveFlowController] DonationButton requested.");
    }
    
    private void HandleSendEmojiButton()
    {
        // 1) 즉시 반응(선택: ISpoke 같은 플래그로 펌핑)
        _engine.PushSignals(new ChatSignals(ChatSignalFlags.ISpoke));

        // 2) 기록 (테스트용 emojiId=1)
        _engine.RecordEmoji(emojiId: 1);

        Debug.Log("[LiveFlowController] SendEmojiButton requested.");
    }

    private void HandleSendChatButton()
    {
        // 1) 즉시 반응
        _engine.PushSignals(new ChatSignals(ChatSignalFlags.ISpoke));

        // 2) 기록 + 아이돌 반응 결정
        IdolReaction reaction = _engine.SubmitChat(ChatTag.Analysis, optionId: "chat_analysis_01");

        Debug.Log($"[LiveFlowController] SendChatButton requested. reaction={reaction}");
    }
}