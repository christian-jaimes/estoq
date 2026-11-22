namespace Estoq.App.Services;

/// <summary>The flows that open as pop-ups from the F1 to F4 shortcuts and Account settings.</summary>
public enum PopupKind
{
    PointOfSale,
    AddItems,
    StockAdjustments,
    Returns,
    Settings,
}

/// <summary>Owns which pop-up is open. Only one pop-up is open at a time.</summary>
public interface IPopupService
{
    /// <summary>The open pop-up, or null when none is open.</summary>
    PopupKind? Current { get; }

    event EventHandler? Changed;

    /// <summary>Opens a pop-up, replacing the one that is open. Does nothing if it is already open.</summary>
    void Show(PopupKind kind);

    /// <summary>Closes the open pop-up. Does nothing if none is open.</summary>
    void Close();
}
