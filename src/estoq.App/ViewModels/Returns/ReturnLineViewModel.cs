using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Estoq.App.ViewModels.Returns;

/// <summary>
/// One row of "Return items": a tick, the item, its price and a quantity stepper limited to what can still come back.
/// Ticking the row or changing the quantity tells the pop-up to recalculate the summary.
/// </summary>
public sealed partial class ReturnLineViewModel : ObservableObject
{
    private readonly Func<decimal, string> _formatPrice;
    private Action? _changed;

    public ReturnLineViewModel(
        int number,
        string variantId,
        string name,
        string detail,
        string sku,
        decimal unitPrice,
        int maxQuantity,
        bool isSelected,
        Func<decimal, string> formatPrice,
        Action changed,
        int returnedQuantity = 0,
        int? soldQuantity = null)
    {
        IsAlternateRow = number % 2 == 0;
        VariantId = variantId;
        Name = name;
        Initial = name.Length > 0 ? name[..1].ToUpperInvariant() : string.Empty;
        Detail = detail;
        Sku = sku;
        UnitPrice = unitPrice;
        MaxQuantity = maxQuantity;
        _formatPrice = formatPrice;
        ReturnedQuantity = returnedQuantity;
        SoldText = soldQuantity?.ToString(CultureInfo.InvariantCulture) ?? "-";
        Quantity = Math.Min(1, maxQuantity);
        IsSelected = isSelected && maxQuantity > 0;
        IncreaseCommand = new RelayCommand(() => SetQuantity(Quantity + 1));
        DecreaseCommand = new RelayCommand(() => SetQuantity(Quantity - 1));
        RemoveCommand = new RelayCommand(() => RemoveRequested?.Invoke(this, EventArgs.Empty));
        _changed = changed;
    }

    /// <summary>Raised by the remove button of a line added without a receipt.</summary>
    public event EventHandler? RemoveRequested;

    public bool IsAlternateRow { get; }

    public string VariantId { get; }

    public string Name { get; }

    public string Initial { get; }

    public string Detail { get; }

    public string Sku { get; }

    public decimal UnitPrice { get; }

    /// <summary>Units that can still come back: what was sold less what earlier returns took.</summary>
    public int MaxQuantity { get; }

    /// <summary>Units bought on the sale (the "Sold" column); a dash for an item added without a receipt.</summary>
    public string SoldText { get; }

    /// <summary>Units of this line that earlier returns already took back (read from the logged stock movements).</summary>
    public int ReturnedQuantity { get; }

    /// <summary>False once everything sold on this line was returned: the row is shown disabled and cannot be ticked.</summary>
    public bool CanReturn => MaxQuantity > 0;

    public double RowOpacity => CanReturn ? 1d : 0.5d;

    /// <summary>"Returned" when the whole line came back, "2 returned" when part of it did; empty when none did.</summary>
    public string StatusText { get; private set; } = string.Empty;

    public bool HasStatus => StatusText.Length > 0;

    public string PriceText => _formatPrice(UnitPrice);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial int Quantity { get; set; }

    public string QuantityText => Quantity.ToString(CultureInfo.InvariantCulture);

    public ICommand IncreaseCommand { get; }

    public ICommand DecreaseCommand { get; }

    public ICommand RemoveCommand { get; }

    /// <summary>Applies a typed quantity. Text that is not a whole number puts the old quantity back.</summary>
    public void CommitQuantity(string text)
    {
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var quantity))
        {
            SetQuantity(quantity);
        }

        OnPropertyChanged(nameof(QuantityText));
    }

    /// <summary>Sets the pill text (the view model that owns the language builds it) and redraws it.</summary>
    public void SetStatus(string text)
    {
        StatusText = text;
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(HasStatus));
    }

    /// <summary>Redraws the price after a language change (number format).</summary>
    public void Refresh() => OnPropertyChanged(nameof(PriceText));

    private void SetQuantity(int quantity)
    {
        Quantity = Estoq.Core.Returns.ReturnRules.ClampQuantity(quantity, MaxQuantity);
        IsSelected = true;
    }

    partial void OnIsSelectedChanged(bool value) => _changed?.Invoke();

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(QuantityText));
        _changed?.Invoke();
    }
}
