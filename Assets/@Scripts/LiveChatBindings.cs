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
        _engine.PushSignals(new ChatSignals(ChatSignalFlags.DonationHappened, donationAmount: 10000));
        Debug.Log("[LiveFlowController] DonationButton requested.");
    }
}