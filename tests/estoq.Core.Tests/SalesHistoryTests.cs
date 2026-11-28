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

    [Fact]
    public void WeekStartsOnMondayAndRunsSevenDays()
    {
        var week = DateRange.Of(QuickRange.ThisWeek, Today);
        Assert.Equal(new DateOnly(2026, 11, 23), week.From);
        Assert.Equal(new DateOnly(2026, 11, 29), week.To);

        Assert.Equal(new DateOnly(2026, 11, 23), DateRange.Of(QuickRange.ThisWeek, new DateOnly(2026, 11, 29)).From);
    }

    [Fact]
    public void MonthAndYearCoverFirstToLastDay()
    {
        var month = DateRange.Of(QuickRange.ThisMonth, Today);
        Assert.Equal(new DateOnly(2026, 11, 1), month.From);
        Assert.Equal(new DateOnly(2026, 11, 30), month.To);

        var year = DateRange.Of(QuickRange.ThisYear, Today);
        Assert.Equal(new DateOnly(2026, 1, 1), year.From);
        Assert.Equal(new DateOnly(2026, 12, 31), year.To);

        var february = DateRange.Of(QuickRange.ThisMonth, new DateOnly(2028, 2, 10));
        Assert.Equal(new DateOnly(2028, 2, 29), february.To);
    }

    [Fact]
    public void TodayIsASingleDay() =>
        Assert.Equal(new DateRange(Today, Today), DateRange.Of(QuickRange.Today, Today));

    [Fact]
    public void RangeIsOrderedWhenEnteredBackwards()
    {
        var range = DateRange.Create(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 10));
        Assert.Equal(new DateOnly(2026, 11, 10), range.From);
        Assert.Equal(new DateOnly(2026, 11, 20), range.To);
        Assert.True(range.Contains(new DateOnly(2026, 11, 20)));
        Assert.False(range.Contains(new DateOnly(2026, 11, 21)));
    }

    [Fact]
    public void ShortcutMatchesOnlyItsOwnRange()
    {
        Assert.Equal(QuickRange.ThisMonth, DateRange.Match(DateRange.Of(QuickRange.ThisMonth, Today), Today));
        Assert.Equal(QuickRange.Today, DateRange.Match(new DateRange(Today, Today), Today));
        Assert.Equal(QuickRange.ThisYear, DateRange.Match(DateRange.Of(QuickRange.ThisYear, Today), Today));
        Assert.Null(DateRange.Match(new DateRange(new DateOnly(2026, 11, 2), Today), Today));
        Assert.Null(DateRange.Match(null, Today));
    }

    [Fact]
    public void ListIsNewestFirstAndDateRangeFiltersByDay()
    {
        var all = SalesHistory.Apply(Sample(), new SalesFilter(), UserRole.Owner, "Owner", Today);
        Assert.Equal("S-000003,S-000002,S-000001", Receipts(all));

        var today = SalesHistory.Apply(Sample(), new SalesFilter(DateRange.Of(QuickRange.Today, Today)), UserRole.Owner, "Owner", Today);
        Assert.Equal("S-000003", Receipts(today));

        var week = SalesHistory.Apply(Sample(), new SalesFilter(DateRange.Of(QuickRange.ThisWeek, Today)), UserRole.Manager, "Owner", Today);
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
    public void MethodAndStaffFiltersCombine()
    {
        var card = SalesHistory.Apply(Sample(), new SalesFilter(Methods: [PaymentMethod.Card]), UserRole.Owner, "x", Today);
        Assert.Equal("S-000002", Receipts(card));

        var none = SalesHistory.Apply(Sample(), new SalesFilter(Methods: [PaymentMethod.Card], StaffNames: ["Maria P."]), UserRole.Owner, "x", Today);
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
    public void MultiSelectFiltersMatchAnyTickedValue()
    {
        var staff = SalesHistory.Apply(Sample(), new SalesFilter(StaffNames: ["maria p.", "Luis R."]), UserRole.Owner, "x", Today);
        Assert.Equal("S-000003,S-000001", Receipts(staff));

        var methods = SalesHistory.Apply(Sample(), new SalesFilter(Methods: [PaymentMethod.Card, PaymentMethod.Cash]), UserRole.Owner, "x", Today);
        Assert.Equal(3, methods.Count);

        Assert.Equal(3, SalesHistory.Apply(Sample(), new SalesFilter(Methods: [], StaffNames: []), UserRole.Owner, "x", Today).Count);
    }

    [Fact]
    public void SortOrdersByTheChosenColumnAndFallsBackToNewestFirst()
    {
        var sales = new[]
        {
            Sale("S-000001", new DateTime(2026, 11, 20, 9, 0, 0), "Zoe", quantity: 3, unitPrice: 5m),
            Sale("S-000002", new DateTime(2026, 11, 22, 9, 0, 0), "Ana", quantity: 1, unitPrice: 50m),
            Sale("S-000003", new DateTime(2026, 11, 21, 9, 0, 0), "Ana", SaleStatus.Voided, quantity: 2, unitPrice: 10m),
        };

        Assert.Equal("S-000002,S-000003,S-000001", Receipts(SalesHistory.Sort(sales, SalesSort.Date, descending: true)));
        Assert.Equal("S-000001,S-000003,S-000002", Receipts(SalesHistory.Sort(sales, SalesSort.Date, descending: false)));
        Assert.Equal("S-000001,S-000002,S-000003", Receipts(SalesHistory.Sort(sales, SalesSort.Receipt, descending: false)));
        Assert.Equal("S-000002,S-000003,S-000001", Receipts(SalesHistory.Sort(sales, SalesSort.Staff, descending: false)));
        Assert.Equal("S-000002,S-000003,S-000001", Receipts(SalesHistory.Sort(sales, SalesSort.Total, descending: true)));
        Assert.Equal("S-000001,S-000003,S-000002", Receipts(SalesHistory.Sort(sales, SalesSort.Items, descending: true)));
        Assert.Equal("S-000002,S-000003,S-000001", Receipts(SalesHistory.Sort(sales, SalesSort.Tax, descending: true)));
        Assert.Equal("S-000003,S-000002,S-000001", Receipts(SalesHistory.Sort(sales, SalesSort.Status, descending: true)));
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
