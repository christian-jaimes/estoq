using System.Globalization;
using Estoq.Core.Dashboard;
using Estoq.Core.Sales;

namespace Estoq.App.Services;

/// <summary>
/// Dashboard data from the sample tables: <c>dummy_article.csv</c> (dimension) and <c>dummy_stock_movements.csv</c> (fact).
/// A "sale" movement is a sold line and a "return" movement a returned line; lines sharing a receipt number are one transaction,
/// and a movement logged without a receipt counts as a transaction of its own.
/// The sample tables keep no payment method and no seller. Until the back end stores them, the payment method is a fixed,
/// repeatable mix worked out from the transaction (see <see cref="PaymentFor"/>) and the seller is left empty.
/// </summary>
internal sealed class SampleDashboardService : IDashboardService
{
    private const string MovementDateFormat = "dd-MM-yy HH:mm";

    // The mix the sample payments follow: cash most, then card, with a tail of the others.
    private static readonly (PaymentMethod Method, int Weight)[] PaymentMix =
    [
        (PaymentMethod.Cash, 38),
        (PaymentMethod.Card, 34),
        (PaymentMethod.Transfer, 12),
        (PaymentMethod.Zelle, 6),
        (PaymentMethod.MobilePayment, 5),
        (PaymentMethod.Usdt, 3),
        (PaymentMethod.PayPal, 2),
    ];

    public IReadOnlyList<DashboardLine> LoadLines()
    {
        try
        {
            var articles = LoadArticles();
            var lines = new List<DashboardLine>();
            foreach (var row in SampleCsv.Read(StockMovementLog.MovementFile))
            {
                var type = SampleCsv.Get(row, "movement_type");
                var isReturn = type == "return";
                if (!isReturn && type != "sale")
                {
                    continue;
                }

                var articleId = SampleCsv.Get(row, "article_id");
                if (!articles.TryGetValue(articleId, out var article)
                    || !DateTime.TryParseExact(SampleCsv.Get(row, "created_at"), MovementDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
                    || !int.TryParse(SampleCsv.Get(row, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity)
                    || !decimal.TryParse(SampleCsv.Get(row, "selling_price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                {
                    continue;
                }

                var cost = decimal.TryParse(SampleCsv.Get(row, "cost_price"), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedCost) ? parsedCost : 0m;
                var receipt = SampleCsv.Get(row, "receipt_number");
                var transaction = receipt.Length > 0 ? SampleSaleHistory.Normalize(receipt) : SampleCsv.Get(row, "movement_id");
                lines.Add(new DashboardLine(
                    transaction, at, articleId, article.Name, article.Sku, article.Category, string.Empty,
                    PaymentFor(transaction), Math.Abs(quantity), price, cost, isReturn));
            }

            return lines;
        }
        catch (IOException)
        {
            return [];
        }
    }

    public IReadOnlyList<StockOnHand> LoadStock()
    {
        try
        {
            var articles = LoadArticles();
            var onHand = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in SampleCsv.Read(StockMovementLog.MovementFile))
            {
                if (int.TryParse(SampleCsv.Get(row, "stock_quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity))
                {
                    var articleId = SampleCsv.Get(row, "article_id");
                    onHand[articleId] = onHand.GetValueOrDefault(articleId) + quantity;
                }
            }

            return [.. articles.Select(pair => new StockOnHand(pair.Key, pair.Value.Name, pair.Value.Sku, Math.Max(0, onHand.GetValueOrDefault(pair.Key))))];
        }
        catch (IOException)
        {
            return [];
        }
    }

    /// <summary>A repeatable pseudo-random method for the transaction, following <see cref="PaymentMix"/>; the same transaction always gets the same method.</summary>
    internal static PaymentMethod PaymentFor(string transaction)
    {
        // FNV-1a: string.GetHashCode changes from run to run, and the sample payments must not.
        var hash = 2166136261u;
        foreach (var character in transaction)
        {
            hash = (hash ^ character) * 16777619u;
        }

        var pick = (int)(hash % (uint)PaymentMix.Sum(entry => entry.Weight));
        foreach (var (method, weight) in PaymentMix)
        {
            if (pick < weight)
            {
                return method;
            }

            pick -= weight;
        }

        return PaymentMethod.Cash;
    }

    private static Dictionary<string, (string Name, string Sku, string Category)> LoadArticles()
    {
        var articles = new Dictionary<string, (string Name, string Sku, string Category)>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in SampleCsv.Read(StockMovementLog.ArticleFile))
        {
            var active = SampleCsv.Get(row, "is_active");
            if (active.Length > 0 && bool.TryParse(active, out var isActive) && !isActive)
            {
                continue;
            }

            articles[SampleCsv.Get(row, "article_id")] = (
                SampleCsv.Get(row, "article_name"),
                SampleCsv.Get(row, "sku"),
                SampleCsv.Get(row, "category_name"));
        }

        return articles;
    }
}
