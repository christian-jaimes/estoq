namespace Estoq.App.Services;

public sealed class PopupService : IPopupService
{
    public PopupKind? Current { get; private set; }

    public event EventHandler? Changed;

    public void Show(PopupKind kind)
    {
        if (Current == kind)
        {
            return;
        }

        Current = kind;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Close()
    {
        if (Current is null)
        {
            return;
        }

        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
