using Estoq.Core.Roles;
using Estoq.Core.Sales;
using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class SalesHistoryTests
{
    private static readonly DateOnly Today = new(2026, 11, 27); // a Friday

    private static SaleRecord Sale(
        string receipt,
        DateTime at,
        string staff = "Maria P.",
        SaleStatus status = SaleStatus.Completed,
        PaymentMethod method = PaymentMethod.Cash,
        decimal unitPrice = 10m,
        int quantity = 2,
        string product = "Basic T-Shirt")
    {
        var lines = new[] { new SaleLineRecord(product, "TS-001", "Beige / M", quantity, unitPrice) };
        var totals = SaleTotals.Calculate(unitPrice * quantity, 0m, TaxRate.TryCreate(16m, out var tax) ? tax : default, TaxMode.Excluded);
        return new SaleRecord(receipt, at, staff, status, lines, 0m, 16m, totals, [new SalePaymentRecord(method, "USD", totals.Total, 1m)]);
    }

    private static SaleRecord[] Sample() =>
    [
        Sale("S-000003", new DateTime(2026, 11, 27, 16, 0, 0), "Maria P."),
        Sale("S-000002", new DateTime(2026, 11, 25, 10, 0, 0), "John D.", method: PaymentMethod.Card),
        Sale("S-000001", new DateTime(2026, 10, 30, 9, 0, 0), "Luis R.", SaleStatus.Voided, product: "Hoodie"),
    ];

    private static string Receipts(IEnumerable<SaleRecord> sales) => string.Join(",", sales.Select(sale => sale.ReceiptNumber));

    // Page numbers joined by commas; 0 is the ellipsis.
    [Fact]
    public void WeekStartsOnMondayAndRunsSevenDays()
    {
        var (from, to) = SalesHistory.RangeOf(SalesPeriod.ThisWeek, Today, null, null);
        Assert.Equal(new DateOnly(2026, 11, 23), from);
        Assert.Equal(new DateOnly(2026, 11, 29), to);

        var (sundayFrom, _) = SalesHistory.RangeOf(SalesPeriod.ThisWeek, new DateOnly(2026, 11, 29), null, null);
        Assert.Equal(new DateOnly(2026, 11, 23), sundayFrom);
    }

    [Fact]
    public void MonthCoversFirstToLastDay()
    {
        var (from, to) = SalesHistory.RangeOf(SalesPeriod.ThisMonth, Today, null, null);
        Assert.Equal(new DateOnly(2026, 11, 1), from);
        Assert.Equal(new DateOnly(2026, 11, 30), to);
    }

    [Fact]
    public void CustomRangeIsOrderedWhenEnteredBackwards()
    {
        var (from, to) = SalesHistory.RangeOf(SalesPeriod.Custom, Today, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 10));
        Assert.Equal(new DateOnly(2026, 11, 10), from);
        Assert.Equal(new DateOnly(2026, 11, 20), to);
    }

    [Fact]
    public void ListIsNewestFirstAndPeriodFiltersByDay()
    {
        var all = SalesHistory.Apply(Sample(), new SalesFilter(), UserRole.Owner, "Owner", Today);
        Assert.Equal("S-000003,S-000002,S-000001", Receipts(all));

        var today = SalesHistory.Apply(Sample(), new SalesFilter(SalesPeriod.Today), UserRole.Owner, "Owner", Today);
        Assert.Equal("S-000003", Receipts(today));

        var week = SalesHistory.Apply(Sample(), new SalesFilter(SalesPeriod.ThisWeek), UserRole.Manager, "Owner", Today);
        Assert.Equal(2, week.Count);
    }

    [Fact]
    public void CashiersSeeOnlyTheirOwnSales()
    {
        var own = SalesHistory.Apply(Sample(), new SalesFilter(), UserRole.Cashier, "John D.", Today);
        Assert.Equal("S-000002", Receipts(own));
    }

    [Fact]
    public void SearchMatchesReceiptStaffAndProduct()
    {
        Assert.Single(SalesHistory.Apply(Sample(), new SalesFilter(Text: "000002"), UserRole.Owner, "x", Today));
        Assert.Single(SalesHistory.Apply(Sample(), new SalesFilter(Text: "luis"), UserRole.Owner, "x", Today));
        Assert.Single(SalesHistory.Apply(Sample(), new SalesFilter(Text: "hood"), UserRole.Owner, "x", Today));
        Assert.Empty(SalesHistory.Apply(Sample(), new SalesFilter(Text: "zzz"), UserRole.Owner, "x", Today));
    }

    [Fact]
    public void MethodStaffAndStatusFiltersCombine()
    {
        var card = SalesHistory.Apply(Sample(), new SalesFilter(Method: PaymentMethod.Card), UserRole.Owner, "x", Today);
        Assert.Equal("S-000002", Receipts(card));

        var voided = SalesHistory.Apply(Sample(), new SalesFilter(Status: SaleStatus.Voided), UserRole.Owner, "x", Today);
        Assert.Equal("S-000001", Receipts(voided));

        var none = SalesHistory.Apply(Sample(), new SalesFilter(Staff: "Maria P.", Status: SaleStatus.Voided), UserRole.Owner, "x", Today);
        Assert.Empty(none);
    }

    [Fact]
    public void VoidedSalesStayInTheListButNotInTheFigures()
    {
        var summary = SalesSummary.Of(Sample());
        Assert.Equal(2, summary.Transactions);
        Assert.Equal(4, summary.ItemsSold);
        Assert.Equal(46.40m, summary.TotalSales); // two sales of 23.20 (20.00 + 16 % tax)
        Assert.Equal(23.20m, summary.AverageSale);
    }

    [Fact]
    public void EmptySummaryIsZeroNotAnError()
    {
        var summary = SalesSummary.Of([]);
        Assert.Equal(0, summary.Transactions);
        Assert.Equal(0m, summary.AverageSale);
    }

    [Fact]
    public void PrimaryMethodIsTheOneThatPaidMost()
    {
        var totals = new SaleTotals(30m, 0m, 0m, 30m);
        var sale = new SaleRecord("S-1", Today.ToDateTime(TimeOnly.MinValue), "x", SaleStatus.Completed, [], 0m, 0m, totals,
        [
            new SalePaymentRecord(PaymentMethod.Cash, "USD", 10m, 1m),
            new SalePaymentRecord(PaymentMethod.Transfer, "EUR", 18.40m, 0.92m),
        ]);

        Assert.Equal(PaymentMethod.Transfer, sale.PrimaryMethod);
        Assert.Equal(20m, sale.Payments[1].BaseValue);
    }

    [Fact]
    public void CsvHasTheTableColumnsFixedNumbersAndSafeCells()
    {
        var sales = new[] { Sale("S-000003", new DateTime(2026, 11, 27, 16, 5, 0), staff: "=cmd|x") };
        var csv = SalesCsv.Build(
            sales,
            ["Receipt", "Date", "Staff", "Method", "Status", "Items", "Total", "Tax"],
            method => method.ToString(),
            status => status.ToString());

        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("\"Receipt\",\"Date\",\"Staff\",\"Method\",\"Status\",\"Items\",\"Total\",\"Tax\"", lines[0]);
        Assert.Equal("\"S-000003\",\"2026-11-27 16:05\",\"'=cmd|x\",\"Cash\",\"Completed\",2,23.20,3.20", lines[1]);
    }
}
