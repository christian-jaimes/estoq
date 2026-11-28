using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.AddProduct;

/// <summary>One row of the Variants + Stock table.</summary>
public sealed partial class VariantRowViewModel : ObservableObject
{
    private readonly Action<VariantRowViewModel> _remove;

    public VariantRowViewModel(string? size, string? colour, string label, string? swatchHex, string sku, string barcode, string currencyCode, bool canSeeCostPrice,
        Action<VariantRowViewModel> remove)
    {
        Size = size;
        Colour = colour;
        Label = label;
        SwatchHex = swatchHex;
        SwatchBrush = Swatch.FromHex(swatchHex);
        Sku = sku;
        InternalBarcode = barcode;
        CurrencyCode = currencyCode;
        CanSeeCostPrice = canSeeCostPrice;
        _remove = remove;
    }

    public string? Size { get; }

    /// <summary>The stable colour value (English name), not the translated label.</summary>
    public string? Colour { get; }

    /// <summary>For example "White – S", in the app language.</summary>
    public string Label { get; }

    public string? SwatchHex { get; }

    public Brush? SwatchBrush { get; }

    public bool HasSwatch => SwatchHex is not null;

    public bool IsAlternateRow { get; set; }

    /// <summary>Edit mode: an existing variant. Its stock and presence cannot change here; only its prices can.</summary>
    public bool IsLocked { get; init; }

    public bool IsEditable => !IsLocked;

    public string InternalBarcode { get; }

    public string CurrencyCode { get; }

    public bool CanSeeCostPrice { get; }

    public bool IsUpdatingDefaultPrices { get; private set; }

    public bool IsCostPriceCustomized { get; private set; }

    public bool IsSellingPriceCustomized { get; private set; }

    /// <summary>The generated SKU. It is read-only in the add-product form.</summary>
    public string Sku { get; }

    /// <summary>Editable opening stock text. Empty is allowed while editing and treated as zero when saved.</summary>
    [ObservableProperty]
    public partial string OpeningStockText { get; set; } = "0";

    public int OpeningStockUnits => int.TryParse(OpeningStockText, out var units) ? Math.Clamp(units, 0, 1_000_000) : 0;

    [RelayCommand]
    private void DecreaseOpeningStock() => OpeningStockText = Math.Max(0, OpeningStockUnits - 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

    [RelayCommand]
    private void IncreaseOpeningStock()
    {
        var units = string.IsNullOrWhiteSpace(OpeningStockText) ? 1 : Math.Min(1_000_000, OpeningStockUnits + 1);
        OpeningStockText = units.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>An optional variant-specific cost price; null inherits the product cost.</summary>
    [ObservableProperty]
    public partial string CostPriceText { get; set; } = string.Empty;

    /// <summary>An optional variant-specific selling price; null inherits the product price.</summary>
    [ObservableProperty]
    public partial string SellingPriceText { get; set; } = string.Empty;

    public void SetDefaultPrices(string costText, string sellingText)
    {
        IsUpdatingDefaultPrices = true;
        try
        {
            if (!IsCostPriceCustomized) CostPriceText = costText;
            if (!IsSellingPriceCustomized) SellingPriceText = sellingText;
        }
        finally { IsUpdatingDefaultPrices = false; }
    }

    public void MarkPriceCustomized(bool isCost)
    {
        if (isCost) IsCostPriceCustomized = true;
        else IsSellingPriceCustomized = true;
    }

    [RelayCommand]
    private void Remove() => _remove(this);

    public string Key => $"{Size}|{Colour}";
}
