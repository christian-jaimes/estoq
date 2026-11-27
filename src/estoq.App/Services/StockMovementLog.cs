using System.Globalization;

namespace Estoq.App.Services;

/// <summary>One stock movement to log. <paramref name="Quantity"/> is signed: a sale takes stock (negative), a return gives it back.</summary>
/// <param name="ReceiptNumber">The sale's receipt number for a sale or a return of that sale; empty otherwise.</param>
/// <param name="Description">Free text for the movement; for a return "&lt;reason&gt; - &lt;note&gt;".</param>
internal sealed record MovementEntry(string ArticleId, string Type, decimal Price, int Quantity, string ReceiptNumber = "", string Description = "");

/// <summary>
/// Appends sale and return movements to <c>dummy_stock_movements.csv</c> (columns: movement_id, article_id, movement_type, cost_price,
/// selling_price, stock_quantity, created_at, location_code, receipt_number, movement_description). The receipt number lets Returns find a sale's contents later.
/// </summary>
internal static class StockMovementLog
{
    public const string MovementFile = "dummy_stock_movements.csv";
    public const string ArticleFile = "dummy_article.csv";

    private const string DefaultLocation = "STORE-GHENT-CENTER";

    /// <summary>
    /// The movement description "&lt;first&gt; - &lt;note&gt;" (a return's reason and note, an adjustment's reference and notes); just one of
    /// them when the other is empty. Line breaks become spaces, because the sample CSV is read line by line.
    /// </summary>
    public static string Describe(string first, string note)
    {
        var head = first.ReplaceLineEndings(" ").Trim();
        var tail = note.ReplaceLineEndings(" ").Trim();
        return head.Length > 0 && tail.Length > 0 ? $"{head} - {tail}" : head.Length > 0 ? head : tail;
    }

    public static void Append(IReadOnlyList<MovementEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var costs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var article in SampleCsv.Read(ArticleFile))
        {
            costs[SampleCsv.Get(article, "article_id")] = SampleCsv.Get(article, "standard_cost_price");
        }

        var next = SampleCsv.Read(MovementFile)
            .Select(row => SampleCsv.Get(row, "movement_id"))
            .Where(id => id.StartsWith("MOV-", StringComparison.OrdinalIgnoreCase))
            .Select(id => int.TryParse(id.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0)
            .DefaultIfEmpty(0).Max() + 1;
        var now = DateTime.Now.ToString("dd-MM-yy HH:mm", CultureInfo.InvariantCulture);

        var lines = entries.Select(entry => string.Join(',',
            $"MOV-{next++}",
            entry.ArticleId,
            entry.Type,
            costs.GetValueOrDefault(entry.ArticleId, "0"),
            entry.Price.ToString("0.##", CultureInfo.InvariantCulture),
            entry.Quantity.ToString(CultureInfo.InvariantCulture),
            now,
            DefaultLocation,
            entry.ReceiptNumber,
            SampleCsv.Escape(entry.Description))).ToList();
        SampleCsv.Append(MovementFile, lines);
    }
}
