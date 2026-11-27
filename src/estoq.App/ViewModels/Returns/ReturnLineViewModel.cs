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
        Action changed)
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
        Quantity = 1;
        IsSelected = isSelected;
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
