using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace Estoq.App.ViewModels.PointOfSale;

/// <summary>One row of the cart table. Built again after every change, so it holds plain values.</summary>
public sealed class CartLineViewModel
{
    private readonly Action<string, int> _setQuantity;

    public CartLineViewModel(
        int number,
        string variantId,
        string name,
        string sku,
        int quantity,
        string priceText,
        string totalText,
        string? stockNote,
        Action<string, int> setQuantity)
    {
        Number = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        IsAlternateRow = number % 2 == 0;
        VariantId = variantId;
        Name = name;
        Initial = name.Length > 0 ? name[..1].ToUpperInvariant() : string.Empty;
        Sku = sku;
        Quantity = quantity;
        QuantityText = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ReceiptQuantityText = $"× {QuantityText}";
        ReceiptPriceText = priceText;
        PriceText = priceText;
        TotalText = totalText;
        StockNote = stockNote ?? string.Empty;
        StockNoteVisibility = stockNote is null ? Visibility.Collapsed : Visibility.Visible;
        _setQuantity = setQuantity;
        IncreaseCommand = new RelayCommand(() => _setQuantity(VariantId, Quantity + 1));
        DecreaseCommand = new RelayCommand(() => _setQuantity(VariantId, Quantity - 1));
        RemoveCommand = new RelayCommand(() => _setQuantity(VariantId, 0));
    }

    public string Number { get; }

    public bool IsAlternateRow { get; }

    public string VariantId { get; }

    public string Name { get; }

    public string Initial { get; }

    public string Sku { get; }

    public int Quantity { get; }

    public string QuantityText { get; }

    public string ReceiptQuantityText { get; }

    public string PriceText { get; }

    public string ReceiptPriceText { get; }

    public string TotalText { get; }

    /// <summary>Out-of-stock or partial-stock warning; empty when stock covers the quantity.</summary>
    public string StockNote { get; }

    public Visibility StockNoteVisibility { get; }

    public ICommand IncreaseCommand { get; }

    public ICommand DecreaseCommand { get; }

    public ICommand RemoveCommand { get; }

    /// <summary>Applies a typed quantity. Text that is not a whole number leaves the line as it was.</summary>
    public void CommitQuantity(string text)
    {
        if (int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var quantity)
            && quantity != Quantity)
        {
            _setQuantity(VariantId, quantity);
        }
    }
}
