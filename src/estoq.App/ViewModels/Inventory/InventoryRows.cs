using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;

namespace Estoq.App.ViewModels.Inventory;

/// <summary>The stock filter picked by clicking a KPI card above the product table.</summary>
public enum InventoryTab
{
    All,
    In,
    Low,
    Out,
}

public enum InventorySort
{
    Name,
    Quantity,
    Price,
    Updated,
}

/// <summary>The tabs of the detail panel.</summary>
public enum InventoryDetailTab
{
    Overview,
    Variants,
    Stock,
    History,
}

/// <summary>The look of a status badge: "success", "warning", "danger" or "neutral". The words always come with it.</summary>
public static class StatusKinds
{
    public const string Success = "success";
    public const string Warning = "warning";
    public const string Danger = "danger";
    public const string Neutral = "neutral";
}

/// <summary>One entry of a filter or sort menu. A null value means "all".</summary>
public sealed record FilterOption(string? Value, string Label);

/// <summary>One row of the product table.</summary>
public sealed partial class InventoryRowViewModel : ObservableObject
{
    public InventoryRowViewModel(
        string id,
        string name,
        string code,
        string category,
        string brand,
        string variantsText,
        string stockText,
        string priceText,
        string statusKind,
        string statusText,
        bool isAlternateRow,
        bool isSelected)
    {
        Id = id;
        Name = name;
        Code = code;
        Initial = name.Length > 0 ? name[..1].ToUpperInvariant() : string.Empty;
        Category = category;
        Brand = brand;
        VariantsText = variantsText;
        StockText = stockText;
        PriceText = priceText;
        StatusKind = statusKind;
        StatusText = statusText;
        IsAlternateRow = isAlternateRow;
        IsSelected = isSelected;
    }

    public string Id { get; }

    public string Name { get; }

    public string Code { get; }

    /// <summary>First-letter placeholder until products have pictures.</summary>
    public string Initial { get; }

    public string Category { get; }

    public string Brand { get; }

    public string VariantsText { get; }

    public string StockText { get; }

    /// <summary>Selling price, or the lowest to highest price when the variants differ.</summary>
    public string PriceText { get; }

    public string StatusKind { get; }

    public string StatusText { get; }

    public bool IsAlternateRow { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>A label and its value in the Overview tab.</summary>
public sealed record DetailLine(string Label, string Value, bool ShowDivider);

/// <summary>One colour circle of the Variants tab: the colour with the size written in it. No stock or product details.</summary>
public sealed record VariantSwatchViewModel(string Hex, string SizeText, bool UseDarkText, string ToolTip);

/// <summary>One variant in the Stock tab.</summary>
public sealed record VariantRowViewModel(
    string Title,
    string Sku,
    string Barcode,
    bool HasBarcode,
    string QuantityText,
    string PriceText,
    bool IsAlternateRow);

/// <summary>One stock movement in the History tab.</summary>
public sealed record MovementRowViewModel(string TypeText, string QuantityText, string Location, string WhenText, bool IsAlternateRow);

/// <summary>Everything the detail panel shows for the selected product, built once per selection.</summary>
public sealed class ProductDetailViewModel
{
    public ProductDetailViewModel(
        string name,
        string code,
        IReadOnlyList<DetailLine> overview,
        IReadOnlyList<VariantSwatchViewModel> swatches,
        IReadOnlyList<VariantRowViewModel> variants,
        IReadOnlyList<MovementRowViewModel> history)
    {
        Name = name;
        Code = code;
        Initial = name.Length > 0 ? name[..1].ToUpperInvariant() : string.Empty;
        Overview = overview;
        Swatches = swatches;
        Variants = variants;
        History = history;
        HasHistory = history.Count > 0;
    }

    public string Name { get; }

    public string Code { get; }

    public string Initial { get; }

    public IReadOnlyList<DetailLine> Overview { get; }

    public IReadOnlyList<VariantSwatchViewModel> Swatches { get; }

    public IReadOnlyList<VariantRowViewModel> Variants { get; }

    public IReadOnlyList<MovementRowViewModel> History { get; }

    public bool HasHistory { get; }
}
