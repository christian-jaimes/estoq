using System.Globalization;
using Estoq.Core.Sales;
using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>
/// A fixed, deterministic sales history for the front-end phase: 126 sales over the last month, a few voided or returned,
/// paid in several methods and currencies. Dates are relative to now so the Today, This week and This month tabs always have data.
/// </summary>
public sealed class SampleSalesService : ISalesService
{
    private const int SaleCount = 126;
    private const decimal TaxPercent = 16m;

    private static readonly (string Name, string Sku, string Variant, decimal Price)[] Products =
    [
        ("Basic T-Shirt", "TS-001", "Beige / M", 24m),
        ("Slim Fit Jeans", "JN-014", "Blue / 32", 28m),
        ("Hoodie", "HD-008", "Gray / L", 20m),
        ("Canvas Sneakers", "SN-022", "White / 42", 45m),
        ("Pleated Midi Dress", "DR-031", "Emerald / S", 49.95m),
        ("Leather Belt", "BL-005", "Black / 95", 18m),
        ("Wool Scarf", "SC-012", "Camel / One size", 15m),
        ("Chino Shorts", "SH-017", "Navy / 34", 22m),
    ];

    // Same sample rates as the till (SamplePointOfSaleService): units of currency per one unit of the base currency.
    private static readonly ExchangeRates Rates = new(Currencies.Default, new Dictionary<string, decimal>
    {
        ["EUR"] = 0.92m,
        ["VES"] = 36.50m,
        ["COP"] = 4100m,
    });

    // Cash twice as often as the others, as in a typical shop.
    private static readonly PaymentMethod[] SingleMethods =
    [
        PaymentMethod.Cash,
        PaymentMethod.Card,
        PaymentMethod.Zelle,
        PaymentMethod.Usdt,
        PaymentMethod.MobilePayment,
        PaymentMethod.PayPal,
        PaymentMethod.Cash,
    ];

    private static readonly string[] Staff = ["Maria P.", "John D.", "Luis R."];

    private readonly ISessionService _session;
    private IReadOnlyList<SaleRecord>? _sales;

    public SampleSalesService(ISessionService session) => _session = session;

    public string BaseCurrency => Rates.BaseCurrency;

    public IReadOnlyList<SaleRecord> GetSales() => _sales ??= Build();

    public IReadOnlyList<string> GetStaffNames() => GetSales()
        .Select(sale => sale.Staff)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public void RecordExport(int rowCount)
    {
        // The audit log arrives with the back end (docs/technical/data-model.md); nothing to write yet.
    }

    private List<SaleRecord> Build()
    {
        // The signed-in user gets a share of the sales, so a cashier (own sales only) never opens an empty page.
        string[] staff = _session.UserName is { Length: > 0 } name ? [.. Staff, name] : Staff;
        TaxRate.TryCreate(TaxPercent, out var tax);

        var now = DateTime.Now;
        var sales = new List<SaleRecord>(SaleCount);
        for (var number = 1; number <= SaleCount; number++)
        {
            // Roughly four sales a day, working hours only; never later than now.
            var daysAgo = (SaleCount - number) / 4;
            var soldAt = now.Date.AddDays(-daysAgo).AddHours(9 + ((number * 37) % 10)).AddMinutes((number * 53) % 60);
            if (soldAt > now)
            {
                soldAt = now.AddMinutes(-(SaleCount - number) * 9);
            }

            var lines = Enumerable.Range(0, 1 + (number % 3))
                .Select(index =>
                {
                    var product = Products[((number * 3) + (index * 5)) % Products.Length];
                    return new SaleLineRecord(product.Name, product.Sku, product.Variant, 1 + ((number + index) % 7 == 0 ? 1 : 0), product.Price);
                })
                .ToList();

            var discount = number % 9 == 0 ? 10m : 0m;
            var totals = SaleTotals.Calculate(lines.Sum(line => line.LineTotal), discount, tax, TaxMode.Excluded);
            var status = number % 23 == 0 ? SaleStatus.Voided : number % 11 == 0 ? SaleStatus.Returned : SaleStatus.Completed;

            sales.Add(new SaleRecord(
                "S-" + number.ToString("D6", CultureInfo.InvariantCulture),
                soldAt,
                staff[(number * 7) % staff.Length],
                status,
                lines,
                discount,
                TaxPercent,
                totals,
                BuildPayments(number, totals.Total)));
        }

        return sales;
    }

    private static List<SalePaymentRecord> BuildPayments(int number, decimal total)
    {
        // Every 17th sale is split between cash in dollars and a transfer in euros.
        if (number % 17 == 0)
        {
            var cash = Money.Round(total / 2m);
            var euros = Rates.FromBase(total - cash, "EUR");
            return
            [
                new SalePaymentRecord(PaymentMethod.Cash, Rates.BaseCurrency, cash, 1m),
                new SalePaymentRecord(PaymentMethod.Transfer, "EUR", euros, Rates.RateOf("EUR")),
            ];
        }

        var method = SingleMethods[(number * 5) % SingleMethods.Length];
        var currency = method == PaymentMethod.MobilePayment ? "VES" : Rates.BaseCurrency;
        return [new SalePaymentRecord(method, currency, Rates.FromBase(total, currency), Rates.RateOf(currency))];
    }
}
