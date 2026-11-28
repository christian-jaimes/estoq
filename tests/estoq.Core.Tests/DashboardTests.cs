using Estoq.Core.Dashboard;
using Estoq.Core.Roles;
using Estoq.Core.Sales;
using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class DashboardTests
{
    private static readonly TaxRate Sixteen = TaxRate.TryCreate(16m, out var rate) ? rate : default;
    private static readonly System.Globalization.CultureInfo Invariant = System.Globalization.CultureInfo.InvariantCulture;
    private static readonly DateOnly Today = new(2026, 11, 18); // a Wednesday

    private static DashboardLine Sale(string transaction, string day, string article, int units, decimal price, decimal cost,
        string category = "Menswear", PaymentMethod payment = PaymentMethod.Cash, string seller = "") =>
        new(transaction, DateTime.Parse(day + " 10:00", Invariant), article, "Name " + article, "SKU-" + article, category, seller, payment, units, price, cost, false);

    private static DashboardLine Return(string transaction, string day, string article, int units, decimal price, decimal cost) =>
        new(transaction, DateTime.Parse(day + " 12:00", Invariant), article, "Name " + article, "SKU-" + article, "Menswear", "", PaymentMethod.Cash, units, price, cost, true);

    [Theory]
    [InlineData(DashboardRange.Today, "2026-11-18", "2026-11-18")]
    [InlineData(DashboardRange.Last7Days, "2026-11-12", "2026-11-18")]
    [InlineData(DashboardRange.Last30Days, "2026-10-20", "2026-11-18")]
    [InlineData(DashboardRange.ThisMonth, "2026-11-01", "2026-11-18")]
    [InlineData(DashboardRange.LastMonth, "2026-10-01", "2026-10-31")]
    public void RangesResolve(DashboardRange range, string start, string end)
    {
        var period = DatePeriod.Resolve(range, Today);
        Assert.Equal(DateOnly.Parse(start, Invariant), period.Start);
        Assert.Equal(DateOnly.Parse(end, Invariant), period.End);
    }

    [Fact]
    public void LastMonthAcrossNewYear()
    {
        var period = DatePeriod.Resolve(DashboardRange.LastMonth, new DateOnly(2027, 1, 9));
        Assert.Equal(new DateOnly(2026, 12, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 12, 31), period.End);
    }

    [Fact]
    public void PreviousPeriodHasSameLengthAndEndsTheDayBefore()
    {
        var period = DatePeriod.Resolve(DashboardRange.ThisMonth, Today);
        var previous = period.Previous();
        Assert.Equal(period.Days, previous.Days);
        Assert.Equal(new DateOnly(2026, 10, 31), previous.End);
        Assert.Equal(new DateOnly(2026, 10, 14), previous.Start);
    }

    [Fact]
    public void TotalsCountSalesTransactionsItemsAndAverage()
    {
        var lines = new[]
        {
            Sale("000001", "2026-11-10", "A", 2, 58m, 20m),
            Sale("000001", "2026-11-10", "B", 1, 116m, 50m),
            Sale("000002", "2026-11-11", "A", 1, 58m, 20m),
            Sale("000003", "2026-10-30", "A", 5, 58m, 20m), // outside the period
        };
        var totals = DashboardCalculator.Totals(lines, DatePeriod.Resolve(DashboardRange.ThisMonth, Today), Sixteen, TaxMode.Included);
        Assert.Equal(290m, totals.Sales);
        Assert.Equal(2, totals.Transactions);
        Assert.Equal(4, totals.ItemsSold);
        Assert.Equal(145m, totals.AverageSale);
    }

    [Fact]
    public void GrossProfitIsBeforeTaxMinusCostAndReturnsComeOff()
    {
        // 116 including 16 % tax is 100 before tax; cost 40 leaves 60.
        var lines = new[]
        {
            Sale("000001", "2026-11-10", "A", 1, 116m, 40m),
            Return("000001", "2026-11-12", "A", 1, 116m, 40m),
            Sale("000002", "2026-11-12", "B", 1, 116m, 40m),
        };
        var totals = DashboardCalculator.Totals(lines, DatePeriod.Resolve(DashboardRange.ThisMonth, Today), Sixteen, TaxMode.Included);
        Assert.Equal(232m, totals.Sales);
        Assert.Equal(116m, totals.Refunds);
        Assert.Equal(60m, totals.GrossProfit);
    }

    [Fact]
    public void TaxExcludedAddsTaxToWhatCustomersPay()
    {
        var line = Sale("000001", "2026-11-10", "A", 1, 100m, 40m);
        Assert.Equal(116m, DashboardCalculator.Gross(line, Sixteen, TaxMode.Excluded));
        Assert.Equal(100m, DashboardCalculator.Net(line, Sixteen, TaxMode.Excluded));
        Assert.Equal(100m, DashboardCalculator.Gross(line, Sixteen, TaxMode.Included));
    }

    [Fact]
    public void NoLinesGiveEmptyTotals()
    {
        var totals = DashboardCalculator.Totals([], DatePeriod.Resolve(DashboardRange.ThisMonth, Today), Sixteen, TaxMode.Included);
        Assert.Equal(DashboardTotals.Empty, totals);
    }

    [Theory]
    [InlineData(112.5, 100, 12.5)]
    [InlineData(90, 100, -10)]
    [InlineData(100, 100, 0)]
    public void PercentChange(double current, double previous, double expected) =>
        Assert.Equal((decimal)expected, DashboardCalculator.PercentChange((decimal)current, (decimal)previous));

    [Fact]
    public void PercentChangeHasNoValueWithoutAPreviousPeriod() =>
        Assert.Null(DashboardCalculator.PercentChange(50m, 0m));

    [Fact]
    public void DailySeriesHasAPointForEveryDayIncludingEmptyOnes()
    {
        var lines = new[] { Sale("1", "2026-11-12", "A", 1, 10m, 4m), Sale("2", "2026-11-14", "A", 2, 10m, 4m) };
        var series = DashboardCalculator.SalesOverTime(lines, DatePeriod.Resolve(DashboardRange.Last7Days, Today), SeriesGranularity.Daily, Sixteen, TaxMode.Included);
        Assert.Equal(7, series.Count);
        Assert.Equal(10m, series[0].Amount);
        Assert.Equal(0m, series[1].Amount);
        Assert.Equal(20m, series[2].Amount);
    }

    [Fact]
    public void WeeklySeriesUsesMondayToSundayClippedToThePeriod()
    {
        // 2026-11-01 is a Sunday: the first bucket is that single day, then Mon 2 to Sun 8, 9 to 15, and 16 to 18.
        var lines = new[]
        {
            Sale("1", "2026-11-01", "A", 1, 10m, 4m),
            Sale("2", "2026-11-03", "A", 1, 20m, 4m),
            Sale("3", "2026-11-08", "A", 1, 5m, 4m),
            Sale("4", "2026-11-17", "A", 1, 7m, 4m),
        };
        var series = DashboardCalculator.SalesOverTime(lines, DatePeriod.Resolve(DashboardRange.ThisMonth, Today), SeriesGranularity.Weekly, Sixteen, TaxMode.Included);
        Assert.Equal(4, series.Count);
        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1)), (series[0].Start, series[0].End));
        Assert.Equal((new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8)), (series[1].Start, series[1].End));
        Assert.Equal(new DateOnly(2026, 11, 18), series[3].End);
        Assert.Equal([10m, 25m, 0m, 7m], series.Select(point => point.Amount));
    }

    [Fact]
    public void PaymentAndCategorySharesAddUpAndAreSortedBiggestFirst()
    {
        var lines = new[]
        {
            Sale("1", "2026-11-10", "A", 1, 30m, 10m, "Menswear", PaymentMethod.Cash),
            Sale("2", "2026-11-10", "B", 1, 70m, 10m, "Footwear", PaymentMethod.Card),
            Return("2", "2026-11-11", "B", 1, 70m, 10m), // returns never count as sales
        };
        var period = DatePeriod.Resolve(DashboardRange.ThisMonth, Today);
        var payments = DashboardCalculator.ByPayment(lines, period, Sixteen, TaxMode.Included);
        Assert.Equal([PaymentMethod.Card, PaymentMethod.Cash], payments.Select(share => share.Method));
        Assert.Equal(0.7m, payments[0].Share);
        Assert.Equal(1m, payments.Sum(share => share.Share));
        var categories = DashboardCalculator.ByCategory(lines, period, Sixteen, TaxMode.Included);
        Assert.Equal(["Footwear", "Menswear"], categories.Select(share => share.Category));
        Assert.Equal(70m, categories[0].Amount);
    }

    [Fact]
    public void TopProductsRankByUnitsThenSalesAndStopAtFive()
    {
        var lines = Enumerable.Range(1, 7).Select(i => Sale("T" + i, "2026-11-10", "ART" + i, i, 10m, 4m)).ToList();
        lines.Add(Sale("X", "2026-11-11", "ART1", 1, 10m, 4m)); // ART1 now has 2 units, same as ART2 but fewer sales
        var top = DashboardCalculator.TopProducts(lines, DatePeriod.Resolve(DashboardRange.ThisMonth, Today), Sixteen, TaxMode.Included);
        Assert.Equal(5, top.Count);
        Assert.Equal(["ART7", "ART6", "ART5", "ART4", "ART3"], top.Select(product => product.ArticleId));
        Assert.Equal([1, 2, 3, 4, 5], top.Select(product => product.Rank));
        Assert.Equal(70m, top[0].Sales);
    }

    [Fact]
    public void OwnFiguresKeepOnlyTheSellersLines()
    {
        var lines = new[]
        {
            Sale("1", "2026-11-10", "A", 1, 10m, 4m, seller: "Ana Lopez"),
            Sale("2", "2026-11-10", "A", 1, 10m, 4m, seller: "Marco"),
            Sale("3", "2026-11-10", "A", 1, 10m, 4m), // no seller: nobody's own
        };
        var own = DashboardCalculator.OwnedBy(lines, "ana lopez");
        Assert.Single(own);
        Assert.Equal("1", own[0].Transaction);
    }

    [Fact]
    public void LowStockIsAboveZeroAndAtOrBelowTheThresholdScarcestFirst()
    {
        var levels = new[]
        {
            new StockOnHand("A", "Alpha", "A1", 6),
            new StockOnHand("B", "Bravo", "B1", 5),
            new StockOnHand("C", "Charlie", "C1", 0),
            new StockOnHand("D", "Delta", "D1", 2),
            new StockOnHand("E", "Echo", "E1", 2),
        };
        var low = LowStock.Items(levels);
        Assert.Equal(["D", "E", "B"], low.Select(level => level.ArticleId));
    }

    [Theory]
    [InlineData(UserRole.Cashier, true, false, false, false)]
    [InlineData(UserRole.Manager, false, true, true, true)]
    [InlineData(UserRole.Owner, false, true, true, true)]
    public void AccessFollowsTheRoleTable(UserRole role, bool own, bool profit, bool charts, bool export)
    {
        var access = DashboardAccess.For(role);
        Assert.Equal((own, profit, charts, export), (access.OwnFiguresOnly, access.ShowGrossProfit, access.ShowCharts, access.CanExport));
    }

    [Fact]
    public void ReportQuotesValuesThatNeedIt()
    {
        var csv = DashboardReport.ToCsv([("Top", "Shirt, red \"XL\"", DashboardReport.Number(12.5m))]);
        Assert.Equal("section,label,value" + Environment.NewLine + "Top,\"Shirt, red \"\"XL\"\"\",12.50" + Environment.NewLine, csv);
    }
}
