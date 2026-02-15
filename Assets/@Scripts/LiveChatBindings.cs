using System;
using UnityEngine;

public sealed class LiveChatBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();
    private readonly DonationHandler _donationHandler;

    private LiveUIRoot bound;

    public LiveChatBindings (DonationHandler donationHandler)
    {
        _donationHandler = donationHandler;
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
        _ctx.Dispose();
        bound = null;
    }

    private void HandleExit()
    {
        // TODO: 씬 전환, 저장, 통계 등
        Debug.Log("[LiveFlowController] Exit requested.");
    }

    private void HandleDonateButton()
    {
        //_donationHandler.SubmitDonation();
    }
}