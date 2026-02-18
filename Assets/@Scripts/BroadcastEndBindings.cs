using System;

public sealed class BroadcastEndBindings : IDisposable
{
    private readonly UIBindingContext _ctx = new();

    private BroadcastEndPanel _bound;

    // “night event key를 받으면 뭘 할지”는 상위 Flow가 결정
    private readonly Action<string> _onContinue;
    private readonly Action _onClose;

    public BroadcastEndBindings(Action<string> onContinue, Action onClose = null)
    {
        _onContinue = onContinue;
        _onClose = onClose;
    }

    public void Bind(BroadcastEndPanel panel)
    {
        if (!panel) return;

        if (_bound && _bound != panel)
            _ctx.Unbind(_bound);

        _bound = panel;

        _ctx.Unbind(panel);
        _ctx.Bind(panel, p => p.OnCloseRequested += HandleClose, p => p.OnCloseRequested -= HandleClose);
        _ctx.Bind(panel, p => p.OnContinueRequested += HandleContinue, p => p.OnContinueRequested -= HandleContinue);
    }

    public void Dispose()
    {
        _ctx.Unbind(_bound);
        _bound = null;
    }

    private void HandleClose()
    {
        _bound?.SetVisible(false);
        _onClose?.Invoke();
    }

    private void HandleContinue(string nightEventKey)
    {
        _bound?.SetVisible(false);
        _onContinue?.Invoke(nightEventKey);
    }
}