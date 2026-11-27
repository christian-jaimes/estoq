using System.Globalization;
using Estoq.Core.Stock;

namespace Estoq.App.Services;

/// <summary>
/// Sample stock adjustments on the development data: an adjustment appends one row per item to <c>dummy_stock_movements.csv</c>,
/// and stock on hand is the sum of an article's movements (<c>dummy_article.csv</c> is the article dimension).
/// Reason, reference, notes and user are kept by the back end's audit log; the sample movement table has no columns for them.
/// </summary>
public sealed class SampleStockAdjustmentService : IStockAdjustmentService
{
    private const string ArticleFile = "dummy_article.csv";
    private const string MovementFile = "dummy_stock_movements.csv";

    // Sample store location that receives the movement until locations exist in the app (same as opening stock).
    private const string Location = "STORE-GHENT-CENTER";

    private readonly INotificationService _notifications;

    public SampleStockAdjustmentService(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public StockItem? FindByCode(string code)
    {
        var trimmed = code.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return Load().FirstOrDefault(item =>
            string.Equals(item.Barcode, trimmed, StringComparison.Ordinal)
            || string.Equals(item.Sku, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public StockItem? Find(string articleId) =>
        Load().FirstOrDefault(item => string.Equals(item.ArticleId, articleId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<StockItem> Search(string text, int maxResults)
    {
        var query = text.Trim();
        if (query.Length == 0)
        {
            return [];
        }

        return Load()
            .Where(item => item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || item.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Sku.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Name.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)
                || item.Barcode.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                || item.Sku.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }

    public AdjustmentReceipt Apply(AdjustmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var articles = SampleCsv.Read(ArticleFile).ToDictionary(row => SampleCsv.Get(row, "article_id"), StringComparer.OrdinalIgnoreCase);
        var items = Load().ToDictionary(item => item.ArticleId, StringComparer.OrdinalIgnoreCase);
        var movementRows = SampleCsv.Read(MovementFile);
        var nextMovement = movementRows
            .Select(row => SampleCsv.Get(row, "movement_id"))
            .Where(id => id.StartsWith("MOV-", StringComparison.OrdinalIgnoreCase))
            .Select(id => int.TryParse(id.AsSpan(4), out var number) ? number : 0)
            .DefaultIfEmpty(0).Max() + 1;
        var now = DateTime.Now.ToString("dd-MM-yy HH:mm", CultureInfo.InvariantCulture);
        var movementType = StockAdjustmentRules.MovementType(request.Type);

        var applied = new List<AppliedAdjustmentItem>();
        var lines = new List<string>();
        foreach (var requested in request.Items)
        {
            if (!items.TryGetValue(requested.ArticleId, out var item))
            {
                continue;
            }

            var counted = StockAdjustmentRules.ClampCounted(request.Type, item.OnHand, requested.Counted);
            var difference = counted - item.OnHand;
            if (difference == 0)
            {
                continue;
            }

            var article = articles[item.ArticleId];
            lines.Add(string.Join(',',
                $"MOV-{nextMovement++}",
                item.ArticleId,
                movementType,
                SampleCsv.Get(article, "standard_cost_price"),
                SampleCsv.Get(article, "retail_selling_price"),
                difference.ToString(CultureInfo.InvariantCulture),
                now,
                Location));
            applied.Add(new AppliedAdjustmentItem(item, item.OnHand, difference, counted));
        }

        // All items are written together, so an adjustment is never half applied.
        SampleCsv.Append(MovementFile, lines);

        // Applying clears the "needs stock correction" notification of the corrected items.
        foreach (var line in applied)
        {
            ClearSoldWithoutStock(line.Item.ArticleId);
        }

        return new AdjustmentReceipt(applied);
    }

    private void ClearSoldWithoutStock(string articleId)
    {
        foreach (var notification in _notifications.GetAll()
                     .Where(n => n.Kind == NotificationKind.SoldWithoutStock
                         && n.NotificationId.EndsWith("-" + articleId, StringComparison.OrdinalIgnoreCase)))
        {
            _notifications.MarkRead(notification.NotificationId);
            _notifications.Clear(notification.NotificationId);
        }
    }

    /// <summary>Every active article with its stock on hand: the sum of its movements across locations.</summary>
    private static List<StockItem> Load()
    {
        var onHand = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var movement in SampleCsv.Read(MovementFile))
        {
            var id = SampleCsv.Get(movement, "article_id");
            if (id.Length > 0 && int.TryParse(SampleCsv.Get(movement, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
            {
                onHand[id] = onHand.GetValueOrDefault(id) + quantity;
            }
        }

        var items = new List<StockItem>();
        foreach (var article in SampleCsv.Read(ArticleFile))
        {
            if (bool.TryParse(SampleCsv.Get(article, "is_active"), out var isActive) && !isActive)
            {
                continue;
            }

            var id = SampleCsv.Get(article, "article_id");
            var name = SampleCsv.Get(article, "article_name");
            if (id.Length == 0 || name.Length == 0)
            {
                continue;
            }

            var size = SampleCsv.Get(article, "size");
            var colour = SampleCsv.Get(article, "color");
            items.Add(new StockItem(
                id,
                name,
                size.Length == 0 ? null : size,
                colour.Length == 0 ? null : colour,
                SampleCsv.Get(article, "sku"),
                SampleCsv.Get(article, "supplier_barcode"),
                onHand.GetValueOrDefault(id)));
        }

        return items;
    }
}
