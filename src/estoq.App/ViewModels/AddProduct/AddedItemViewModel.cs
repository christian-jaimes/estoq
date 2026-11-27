namespace Estoq.App.ViewModels.AddProduct;

/// <summary>One article (variant) listed on the "Product added" confirmation.</summary>
/// <param name="Name">The product name.</param>
/// <param name="VariantText">Size and colour in the app language, for example "White – S"; empty for a one-size, no-colour item.</param>
/// <param name="Sku">The variant's SKU.</param>
/// <param name="PriceText">The selling price per unit in the base currency.</param>
/// <param name="QuantityText">Units added to stock.</param>
/// <param name="TotalText">Selling price times units.</param>
/// <param name="IsAlternateRow">True for every second row, which gets the table's banding.</param>
public sealed record AddedItemViewModel(string Name, string VariantText, string Sku, string PriceText, string QuantityText, string TotalText, bool IsAlternateRow)
{
    public string DetailText => VariantText.Length == 0 ? Sku : $"{VariantText} · {Sku}";
}
