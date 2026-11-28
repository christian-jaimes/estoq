using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Controls;
using Estoq.App.Services;
using Estoq.App.ViewModels.PointOfSale;
using Estoq.Core.Dashboard;
using Estoq.Core.Setup;

namespace Estoq.App.ViewModels;

/// <summary>
/// The dashboard overview (docs/features/dashboard.md): KPI cards with the change against the previous period, sales over time,
/// sales by payment method, top sellers, sales by category and low stock. The figures come from <see cref="DashboardCalculator"/>;
/// this class picks what the signed-in role may see, formats it and keeps the range and granularity choices.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private const int TopPaymentMethods = 4;

    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;
    private readonly IDashboardService _data;
    private readonly IPointOfSaleService _pointOfSale;
    private readonly INavigationService _navigation;
    private readonly IReportExportService _export;

    private DashboardRange _range = DashboardRange.ThisMonth;
    private SeriesGranularity _granularity = SeriesGranularity.Daily;
    private DashboardAccess _access = DashboardAccess.For(Estoq.Core.Roles.UserRole.Cashier);
    private List<(string Section, string Label, string Value)> _report = [];

    public DashboardViewModel(
        ILocalizer localizer, ISessionService session, IDashboardService data, IPointOfSaleService pointOfSale,
        INavigationService navigation, IReportExportService export)
    {
        _localizer = localizer;
        _session = session;
        _data = data;
        _pointOfSale = pointOfSale;
        _navigation = navigation;
        _export = export;
        _localizer.LanguageChanged += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<CurrencyChipViewModel> Ranges { get; } = [];

    public ObservableCollection<CurrencyChipViewModel> Granularities { get; } = [];

    public ObservableCollection<DashboardKpi> Kpis { get; } = [];

    public ObservableCollection<PaymentRow> Payments { get; } = [];

    public ObservableCollection<CategoryRow> Categories { get; } = [];

    public ObservableCollection<TopProductRow> TopProducts { get; } = [];

    public ObservableCollection<LowStockRow> LowStockItems { get; } = [];

    public string Title => _localizer["Nav_Dashboard"];

    [ObservableProperty]
    public partial string Subtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChartPoint> SalesSeries { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<DonutSlice> PaymentSlices { get; set; } = [];

    [ObservableProperty]
    public partial string PaymentTotal { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LowStockCount { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowCharts { get; set; }

    [ObservableProperty]
    public partial bool CanExport { get; set; }

    [ObservableProperty]
    public partial bool HasSalesSeries { get; set; }

    [ObservableProperty]
    public partial bool HasPayments { get; set; }

    [ObservableProperty]
    public partial bool HasTopProducts { get; set; }

    [ObservableProperty]
    public partial bool HasCategories { get; set; }

    [ObservableProperty]
    public partial bool HasLowStock { get; set; }

    [ObservableProperty]
    public partial string ExportStatus { get; set; } = string.Empty;

    /// <summary>Formats a value-axis label compactly ("$1.2K"), for the area chart.</summary>
    public Func<decimal, string> AxisFormat => value => Compact(value, _currency, _localizer.Culture);

    private string _currency = string.Empty;

    /// <summary>Reads the data again and rebuilds every figure. Called when the page opens, when the language changes and after a range choice.</summary>
    public void Refresh()
    {
        _access = DashboardAccess.For(_session.Role);
        ShowCharts = _access.ShowCharts;
        CanExport = _access.CanExport;
        BuildChoices();

        var culture = _localizer.Culture;
        var currency = _currency = _pointOfSale.GetSettings().Rates.BaseCurrency;
        var rate = SampleStoreTax.Rate;
        var mode = SampleStoreTax.Mode;
        var period = DatePeriod.Resolve(_range, DateOnly.FromDateTime(DateTime.Now));
        var previous = period.Previous();

        var lines = _data.LoadLines();
        if (_access.OwnFiguresOnly)
        {
            lines = DashboardCalculator.OwnedBy(lines, _session.UserName ?? string.Empty);
        }

        Subtitle = _range == DashboardRange.Today
            ? period.Start.ToString("D", culture)
            : _localizer.Format("Dash_SubtitleRange", period.Start.ToString("d MMM yyyy", culture), period.End.ToString("d MMM yyyy", culture));

        _report = [(_localizer["Dash_ReportSection_Period"], _localizer["Dash_ReportLabel_From"], period.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                   (_localizer["Dash_ReportSection_Period"], _localizer["Dash_ReportLabel_To"], period.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))];

        var now = DashboardCalculator.Totals(lines, period, rate, mode);
        var before = DashboardCalculator.Totals(lines, previous, rate, mode);
        BuildKpis(now, before, currency, culture);

        Categories.Clear();
        Payments.Clear();
        TopProducts.Clear();
        if (_access.ShowCharts)
        {
            BuildCharts(lines, period, rate, mode, currency, culture);
        }
        else
        {
            SalesSeries = [];
            PaymentSlices = [];
            HasSalesSeries = HasPayments = HasTopProducts = HasCategories = false;
        }

        BuildLowStock();
    }

    /// <summary>
    /// Builds the range and daily/weekly chips. While the language is unchanged the existing chips only move their selection, so the chip being
    /// clicked is not torn out from under the pointer.
    /// </summary>
    private void BuildChoices()
    {
        Sync(Ranges, Enum.GetValues<DashboardRange>(), _range, range => _localizer["Dash_Range_" + range], SelectRange);
        Sync(Granularities, Enum.GetValues<SeriesGranularity>(), _granularity, granularity => _localizer["Dash_Granularity_" + granularity], SelectGranularity);
    }

    private static void Sync<T>(ObservableCollection<CurrencyChipViewModel> chips, T[] values, T selected, Func<T, string> label, Action<string> select)
        where T : struct, Enum
    {
        if (chips.Count == values.Length && chips.Select((chip, i) => chip.Label == label(values[i])).All(same => same))
        {
            for (var i = 0; i < values.Length; i++)
            {
                chips[i].IsSelected = EqualityComparer<T>.Default.Equals(values[i], selected);
            }

            return;
        }

        chips.Clear();
        foreach (var value in values)
        {
            chips.Add(new CurrencyChipViewModel(value.ToString(), label(value), EqualityComparer<T>.Default.Equals(value, selected), select));
        }
    }

    private void SelectRange(string code)
    {
        if (Enum.TryParse<DashboardRange>(code, out var range) && range != _range)
        {
            _range = range;
            ExportStatus = string.Empty;
            Refresh();
        }
    }

    private void SelectGranularity(string code)
    {
        if (Enum.TryParse<SeriesGranularity>(code, out var granularity) && granularity != _granularity)
        {
            _granularity = granularity;
            Refresh();
        }
    }

    private void BuildKpis(DashboardTotals now, DashboardTotals before, string currency, CultureInfo culture)
    {
        Kpis.Clear();
        string Money(decimal amount) => CurrencyFormat.Amount(amount, currency, culture);

        void Add(string glyph, string labelKey, string value, decimal current, decimal previous, bool risingIsBad = false)
        {
            _report.Add((_localizer["Dash_ReportSection_Totals"], _localizer[labelKey], DashboardReport.Number(current)));
            var change = DashboardCalculator.PercentChange(current, previous);
            if (change is not { } percent)
            {
                Kpis.Add(new DashboardKpi(glyph, _localizer[labelKey], value, _localizer["Dash_NoPrevious"], string.Empty, 0));
                return;
            }

            var glyphOfChange = percent > 0 ? "" : percent < 0 ? "" : string.Empty;
            var tone = percent == 0 ? 0 : (percent > 0) != risingIsBad ? 1 : -1;
            var text = _localizer.Format("Dash_ChangeVsPrevious", (percent > 0 ? "+" : string.Empty) + percent.ToString("0.0", culture) + "%");
            Kpis.Add(new DashboardKpi(glyph, _localizer[labelKey], value, text, glyphOfChange, tone));
        }

        Add("", "Dash_KpiSales", Money(now.Sales), now.Sales, before.Sales);
        Add("", "Dash_KpiTransactions", now.Transactions.ToString("N0", culture), now.Transactions, before.Transactions);
        Add("", "Dash_KpiItems", now.ItemsSold.ToString("N0", culture), now.ItemsSold, before.ItemsSold);
        Add("", "Dash_KpiAverage", Money(now.AverageSale), now.AverageSale, before.AverageSale);
        Add("", "Dash_KpiRefunds", Money(now.Refunds), now.Refunds, before.Refunds, risingIsBad: true);
        if (_access.ShowGrossProfit)
        {
            Add("", "Dash_KpiProfit", Money(now.GrossProfit), now.GrossProfit, before.GrossProfit);
        }
    }

    private void BuildCharts(IReadOnlyList<DashboardLine> lines, DatePeriod period, TaxRate rate, TaxMode mode, string currency, CultureInfo culture)
    {
        string Money(decimal amount) => CurrencyFormat.Amount(amount, currency, culture);
        string Percent(decimal share) => share.ToString("P0", culture);

        var series = DashboardCalculator.SalesOverTime(lines, period, _granularity, rate, mode);
        SalesSeries = [.. series.Select(point => new ChartPoint(
            point.Start.ToString("d MMM", culture),
            point.Amount,
            (point.End == point.Start ? point.Start.ToString("d MMM", culture) : $"{point.Start.ToString("d MMM", culture)} - {point.End.ToString("d MMM", culture)}") + ": " + Money(point.Amount)))];
        HasSalesSeries = series.Count >= 2 && series.Any(point => point.Amount > 0m);
        foreach (var point in series)
        {
            _report.Add((_localizer["Dash_ReportSection_Series"], point.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), DashboardReport.Number(point.Amount)));
        }

        // The four chart colours go to the four biggest methods; the rest share one "Other methods" row and the muted colour.
        var payments = DashboardCalculator.ByPayment(lines, period, rate, mode);
        var shown = payments.Take(TopPaymentMethods).ToList();
        var rest = payments.Skip(TopPaymentMethods).ToList();
        var slices = new List<DonutSlice>();
        for (var i = 0; i < shown.Count; i++)
        {
            Payments.Add(new PaymentRow(_localizer["Pos_Method_" + shown[i].Method], Percent(shown[i].Share), Money(shown[i].Amount), i));
            slices.Add(new DonutSlice((double)shown[i].Share, i));
            _report.Add((_localizer["Dash_ReportSection_Payments"], _localizer["Pos_Method_" + shown[i].Method], DashboardReport.Number(shown[i].Amount)));
        }

        if (rest.Count > 0)
        {
            var amount = rest.Sum(share => share.Amount);
            var share = rest.Sum(item => item.Share);
            Payments.Add(new PaymentRow(_localizer["Dash_PaymentOther"], Percent(share), Money(amount), ChartPalette.Colors - 1));
            slices.Add(new DonutSlice((double)share, ChartPalette.Colors - 1));
            _report.Add((_localizer["Dash_ReportSection_Payments"], _localizer["Dash_PaymentOther"], DashboardReport.Number(amount)));
        }

        PaymentSlices = slices;
        HasPayments = payments.Count > 0;
        PaymentTotal = Money(payments.Sum(item => item.Amount));

        foreach (var product in DashboardCalculator.TopProducts(lines, period, rate, mode))
        {
            TopProducts.Add(new TopProductRow(product.Rank.ToString(culture), product.Name, product.Sku,
                product.Units.ToString("N0", culture), Money(product.Sales), product.Rank % 2 == 0));
            _report.Add((_localizer["Dash_ReportSection_Top"], $"{product.Name} ({product.Sku})", product.Units.ToString(CultureInfo.InvariantCulture)));
        }

        HasTopProducts = TopProducts.Count > 0;

        var categories = DashboardCalculator.ByCategory(lines, period, rate, mode);
        for (var i = 0; i < categories.Count; i++)
        {
            Categories.Add(new CategoryRow(categories[i].Category, Percent(categories[i].Share), Money(categories[i].Amount), (double)categories[i].Share, 0));
            _report.Add((_localizer["Dash_ReportSection_Categories"], categories[i].Category, DashboardReport.Number(categories[i].Amount)));
        }

        HasCategories = Categories.Count > 0;
    }

    private void BuildLowStock()
    {
        LowStockItems.Clear();
        var low = LowStock.Items(_data.LoadStock());
        HasLowStock = low.Count > 0;
        LowStockCount = _localizer.Format("Dash_LowStockCount", low.Count);
        var rows = low.Take(5).ToList();
        for (var i = 0; i < rows.Count; i++)
        {
            LowStockItems.Add(new LowStockRow(rows[i].Name, rows[i].Sku, _localizer.Format("Dash_Left", rows[i].OnHand), rows[i].OnHand <= 2, i % 2 == 1));
        }

        _report.Add((_localizer["Dash_ReportSection_Stock"], _localizer["Dash_LowStockTitle"], low.Count.ToString(CultureInfo.InvariantCulture)));
    }

    [RelayCommand]
    private void OpenInventory() => _navigation.NavigateTo(AppRoute.Inventory);

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (!_access.CanExport)
        {
            return;
        }

        var name = _localizer.Format("Dash_ReportFileName", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var path = await _export.SaveCsvAsync(name, DashboardReport.ToCsv(_report));
        ExportStatus = path is null ? string.Empty : _localizer["Dash_ReportSaved"];
    }

    /// <summary>"$1.2K", "$3.4M" or "$850": the symbol and a short number, for the chart axis.</summary>
    internal static string Compact(decimal value, string currency, CultureInfo culture)
    {
        var symbol = CurrencyFormat.Symbol(currency);
        var separator = symbol == currency ? " " : string.Empty;
        var (scaled, suffix) = value >= 1_000_000m ? (value / 1_000_000m, "M") : value >= 1_000m ? (value / 1_000m, "K") : (value, string.Empty);
        return symbol + separator + scaled.ToString(suffix.Length == 0 ? "0" : "0.#", culture) + suffix;
    }
}
