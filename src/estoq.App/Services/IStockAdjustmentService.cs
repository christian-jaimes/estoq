using Estoq.Core.Stock;

namespace Estoq.App.Services;

/// <summary>An article (variant) as the Stock adjustments pop-up sees it: stock on hand is the sum of its movements.</summary>
public sealed record StockItem(string ArticleId, string Name, string? Size, string? Colour, string Sku, string Barcode, int OnHand)
{
    /// <summary>For example "Beige · M"; empty for a one-size item without a colour.</summary>
    public string VariantText => string.Join(" · ", new[] { Colour, Size }.Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>The variant text, or the SKU when the item has neither colour nor size.</summary>
    public string DetailText => VariantText.Length > 0 ? VariantText : Sku;
}

/// <summary>One item of an adjustment to apply: the article and its counted quantity (the new stock).</summary>
public sealed record AdjustmentItem(string ArticleId, int Counted);

/// <summary>Everything an applied adjustment saves (docs/features/stock-adjustments.md).</summary>
public sealed record AdjustmentRequest(AdjustmentType Type, string Reason, string Reference, string Notes, IReadOnlyList<AdjustmentItem> Items);

/// <summary>One item as written: stock before, the signed difference and stock after.</summary>
public sealed record AppliedAdjustmentItem(StockItem Item, int Previous, int Difference, int NewStock);

/// <summary>What the pop-up shows after an adjustment is applied.</summary>
public sealed record AdjustmentReceipt(IReadOnlyList<AppliedAdjustmentItem> Items);

/// <summary>
/// Catalog and saving for the Stock adjustments pop-up. The sample implementation appends movements to the sample
/// stock-movements data; the back end replaces it and saves all items in one transaction with the audit entry.
/// </summary>
public interface IStockAdjustmentService
{
    /// <summary>The article whose internal or supplier barcode, or SKU, equals <paramref name="code"/>.</summary>
    StockItem? FindByCode(string code);

    /// <summary>The article with this id, with its current stock.</summary>
    StockItem? Find(string articleId);

    /// <summary>Articles whose name, SKU or barcode contains <paramref name="text"/>, best matches first.</summary>
    IReadOnlyList<StockItem> Search(string text, int maxResults);

    /// <summary>
    /// Writes one stock movement per item that changed (never an overwrite), clears the "sold without stock" notifications of
    /// those items and returns what was written. Items whose counted quantity equals their stock write nothing.
    /// </summary>
    AdjustmentReceipt Apply(AdjustmentRequest request);
}
