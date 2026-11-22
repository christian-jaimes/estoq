using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.AddProduct;

/// <summary>One row of the Variants + Stock table.</summary>
public sealed partial class VariantRowViewModel : ObservableObject
{
    public VariantRowViewModel(string? size, string? colour, string label, string? swatchHex, string sku, string barcode)
    {
        Size = size;
        Colour = colour;
        Label = label;
        SwatchHex = swatchHex;
        SwatchBrush = Swatch.FromHex(swatchHex);
        Sku = sku;
        InternalBarcode = barcode;
    }

    public string? Size { get; }

    /// <summary>The stable colour value (English name), not the translated label.</summary>
    public string? Colour { get; }

    /// <summary>For example "White – S", in the app language.</summary>
    public string Label { get; }

    public string? SwatchHex { get; }

    public Brush? SwatchBrush { get; }

    public bool HasSwatch => SwatchHex is not null;

    public string InternalBarcode { get; }

    /// <summary>The generated SKU; the owner can type over it. It must stay unique.</summary>
    [ObservableProperty]
    public partial string Sku { get; set; }

    /// <summary>True once the owner typed their own SKU, so regenerating variants keeps it.</summary>
    public bool IsSkuOverridden { get; set; }

    /// <summary>Opening stock. A double because the number box binds to one; the value is always a whole number of at least 0.</summary>
    [ObservableProperty]
    public partial double OpeningStock { get; set; }

    public int OpeningStockUnits => double.IsNaN(OpeningStock) ? 0 : (int)Math.Max(0, Math.Round(OpeningStock));

    public string Key => $"{Size}|{Colour}";
}
