using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.App.ViewModels.Returns;
using Estoq.Core.Roles;
using Estoq.Core.Sales;

namespace Estoq.App.ViewModels.Sales;

/// <summary>
/// State of the Sales page (docs/features/sales.md): KPI cards, period tabs, filters, the table (rows load on scroll) and the floating sale detail.
/// All filtering, ranges, paging and figures come from <c>Estoq.Core.Sales</c>; this class formats them and wires the buttons.
/// </summary>
public sealed partial class SalesViewModel : ObservableObject, IDisposable
{
    /// <summary>Rows added each time the table is scrolled near its end (no pager).</summary>
    public const int BatchSize = 25;

    private readonly ISalesService _sales;
    private readonly ISessionService _session;
    private readonly IPopupService _popups;
    private readonly ReturnsViewModel _returns;
    private readonly ILocalizer _localizer;
    private readonly IReceiptPrinter _printer;
    private readonly ICsvExportService _exporter;

    private IReadOnlyList<SaleRecord> _filtered = [];
    private SalesPeriod _period = SalesPeriod.All;
    private int _shown = BatchSize;

    // True while the view model changes its own bound state (rebuilding lists, moving selections), so those changes
    // are not mistaken for something the user did.
    private bool _building;

    public SalesViewModel(
        ISalesService sales,
        ISessionService session,
        IPopupService popups,
        ReturnsViewModel returns,
        ILocalizer localizer,
        IReceiptPrinter printer,
        ICsvExportService exporter)
    {
        _sales = sales;
        _session = session;
        _popups = popups;
        _returns = returns;
        _localizer = localizer;
        _printer = printer;
        _exporter = exporter;

        var today = Today;
        _building = true;
        CustomToDate = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue));
        CustomFromDate = new DateTimeOffset(today.AddDays(-29).ToDateTime(TimeOnly.MinValue));
        _building = false;

        CanExport = RolePermissions.IsAllowed(_session.Role, Permission.ExportCsv);
        BuildChoices();
        Reload();
        _localizer.LanguageChanged += OnLanguageChanged;
    }

    public ObservableCollection<PeriodTabViewModel> Tabs { get; } = [];

    public ObservableCollection<SalesFilterOption> MethodOptions { get; } = [];

    public ObservableCollection<SalesFilterOption> StaffOptions { get; } = [];

    public ObservableCollection<SalesFilterOption> StatusOptions { get; } = [];

    public ObservableCollection<SaleRowViewModel> Rows { get; } = [];

    public ObservableCollection<SaleDetailLineViewModel> DetailLines { get; } = [];

    public ObservableCollection<SalePaymentViewModel> DetailPayments { get; } = [];

    /// <summary>Export is for managers and owners (docs/product/roles-and-permissions.md).</summary>
    public bool CanExport { get; }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private string Currency => _sales.BaseCurrency;

    // Filters

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SalesFilterOption? SelectedMethod { get; set; }

    [ObservableProperty]
    public partial SalesFilterOption? SelectedStaff { get; set; }

    [ObservableProperty]
    public partial SalesFilterOption? SelectedStatus { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? CustomFromDate { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? CustomToDate { get; set; }

    [ObservableProperty]
    public partial string DateRangeText { get; set; } = string.Empty;

    // KPI cards

    [ObservableProperty]
    public partial string TotalSalesText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TransactionsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ItemsSoldText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AverageSaleText { get; set; } = string.Empty;

    // Table and pager

    [ObservableProperty]
    public partial SaleRowViewModel? Selected { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial string FooterText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NoticeText { get; set; } = string.Empty;

    public bool HasNotice => NoticeText.Length > 0;

    // Sale detail (HasSelection is true while it is open)

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial string DetailReceipt { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SaleStatus DetailStatus { get; set; }

    [ObservableProperty]
    public partial string DetailStatusKind { get; set; } = "neutral";

    [ObservableProperty]
    public partial string DetailStatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailDateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailStaffText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailItemsHeader { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailSubtotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailDiscountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailTaxLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailTaxText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DetailTotalText { get; set; } = string.Empty;

    public void Dispose() => _localizer.LanguageChanged -= OnLanguageChanged;

    partial void OnSearchTextChanged(string value) => OnFilterChanged();

    partial void OnSelectedMethodChanged(SalesFilterOption? value) => OnFilterChanged();

    partial void OnSelectedStaffChanged(SalesFilterOption? value) => OnFilterChanged();

    partial void OnSelectedStatusChanged(SalesFilterOption? value) => OnFilterChanged();

    // Choosing a date in the range picker means the period is now a custom one.
    partial void OnCustomFromDateChanged(DateTimeOffset? value) => OnCustomDateChanged();

    partial void OnCustomToDateChanged(DateTimeOffset? value) => OnCustomDateChanged();

    partial void OnNoticeTextChanged(string value) => OnPropertyChanged(nameof(HasNotice));

    partial void OnSelectedChanged(SaleRowViewModel? oldValue, SaleRowViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
        }

        if (!_building)
        {
            ShowDetail(newValue);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        BuildChoices();
        Reload();
    }

    private void OnFilterChanged()
    {
        if (_building)
        {
            return;
        }

        _shown = BatchSize;
        Reload();
    }

    private void OnCustomDateChanged()
    {
        if (_building || CustomFromDate is null || CustomToDate is null)
        {
            return;
        }

        _period = SalesPeriod.Custom;
        _building = true;
        foreach (var tab in Tabs)
        {
            tab.IsSelected = tab.Period == SalesPeriod.Custom;
        }

        _building = false;
        _shown = BatchSize;
        Reload();
    }

    private void OnTabSelected(SalesPeriod period)
    {
        if (_building || period == _period)
        {
            return;
        }

        _period = period;
        _shown = BatchSize;
        Reload();
    }

    /// <summary>Builds the tabs and drop-downs in the current language, keeping what the user had chosen.</summary>
    private void BuildChoices()
    {
        var methodKey = SelectedMethod?.Key ?? string.Empty;
        var staffKey = SelectedStaff?.Key ?? string.Empty;
        var statusKey = SelectedStatus?.Key ?? string.Empty;

        _building = true;

        Tabs.Clear();
        foreach (var (period, key) in new[]
        {
            (SalesPeriod.All, "Sales_TabAll"),
            (SalesPeriod.Today, "Sales_TabToday"),
            (SalesPeriod.ThisWeek, "Sales_TabWeek"),
            (SalesPeriod.ThisMonth, "Sales_TabMonth"),
            (SalesPeriod.Custom, "Sales_TabCustom"),
        })
        {
            Tabs.Add(new PeriodTabViewModel(period, _localizer[key], period == _period, OnTabSelected));
        }

        MethodOptions.Clear();
        MethodOptions.Add(new SalesFilterOption(string.Empty, _localizer["Sales_AllMethods"]));
        foreach (var method in PaymentMethods.All)
        {
            MethodOptions.Add(new SalesFilterOption(method.ToString(), _localizer["Pos_Method_" + method]));
        }

        StatusOptions.Clear();
        StatusOptions.Add(new SalesFilterOption(string.Empty, _localizer["Sales_AllStatus"]));
        foreach (var status in Enum.GetValues<SaleStatus>())
        {
            StatusOptions.Add(new SalesFilterOption(status.ToString(), _localizer["Sales_Status_" + status]));
        }

        StaffOptions.Clear();
        StaffOptions.Add(new SalesFilterOption(string.Empty, _localizer["Sales_AllStaff"]));
        var visible = SalesHistory.Apply(_sales.GetSales(), new SalesFilter(), _session.Role, _session.UserName ?? string.Empty, Today);
        foreach (var name in visible.Select(sale => sale.Staff).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.CurrentCultureIgnoreCase))
        {
            StaffOptions.Add(new SalesFilterOption(name, name));
        }

        SelectedMethod = MethodOptions.FirstOrDefault(option => option.Key == methodKey) ?? MethodOptions[0];
        SelectedStaff = StaffOptions.FirstOrDefault(option => option.Key == staffKey) ?? StaffOptions[0];
        SelectedStatus = StatusOptions.FirstOrDefault(option => option.Key == statusKey) ?? StatusOptions[0];

        _building = false;
    }

    private SalesFilter BuildFilter() => new(
        _period,
        CustomFromDate is { } from ? DateOnly.FromDateTime(from.Date) : null,
        CustomToDate is { } to ? DateOnly.FromDateTime(to.Date) : null,
        SearchText,
        Enum.TryParse<PaymentMethod>(SelectedMethod?.Key, out var method) ? method : null,
        SelectedStaff?.Key,
        Enum.TryParse<SaleStatus>(SelectedStatus?.Key, out var status) ? status : null);

    /// <summary>Applies the filters, then rebuilds the KPIs, the page of rows, the pager and the detail panel.</summary>
    private void Reload()
    {
        var culture = _localizer.Culture;
        var filter = BuildFilter();
        _filtered = SalesHistory.Apply(_sales.GetSales(), filter, _session.Role, _session.UserName ?? string.Empty, Today);

        // Figures describe everything the filters match, not only the page on screen.
        var summary = SalesSummary.Of(_filtered);
        TotalSalesText = CurrencyFormat.Amount(summary.TotalSales, Currency, culture);
        TransactionsText = summary.Transactions.ToString("N0", culture);
        ItemsSoldText = summary.ItemsSold.ToString("N0", culture);
        AverageSaleText = CurrencyFormat.Amount(summary.AverageSale, Currency, culture);
        DateRangeText = BuildRangeText(filter, culture);

        var previouslySelected = Selected?.ReceiptNumber;
        _building = true;
        Rows.Clear();
        AddRows(0, Math.Min(_shown, _filtered.Count));

        IsEmpty = Rows.Count == 0;
        // The detail is open only after a click: it stays on the same sale when that sale is still listed, otherwise it closes.
        Selected = previouslySelected is null ? null : Rows.FirstOrDefault(row => row.ReceiptNumber == previouslySelected);
        _building = false;
        UpdateFooter();

        ShowDetail(Selected);
    }

    private void AddRows(int from, int to)
    {
        for (var index = from; index < to; index++)
        {
            Rows.Add(new SaleRowViewModel(_filtered[index], Currency, _localizer, Rows.Count % 2 == 1));
        }
    }

    private void UpdateFooter()
    {
        var culture = _localizer.Culture;
        FooterText = _filtered.Count == 0
            ? _localizer["Sales_ShowingNone"]
            : _localizer.Format("Sales_Showing", Rows.Count.ToString("N0", culture), _filtered.Count.ToString("N0", culture));
    }

    /// <summary>Whether more sales match than the table shows so far.</summary>
    public bool HasMore => Rows.Count < _filtered.Count;

    /// <summary>Adds the next batch of rows (called when the table is scrolled near its end).</summary>
    public void LoadMore()
    {
        if (!HasMore)
        {
            return;
        }

        var from = Rows.Count;
        _shown = Math.Min(from + BatchSize, _filtered.Count);
        AddRows(from, _shown);
        IsEmpty = Rows.Count == 0;
        UpdateFooter();
    }

    private string BuildRangeText(SalesFilter filter, CultureInfo culture)
    {
        var (from, to) = SalesHistory.RangeOf(filter.Period, Today, filter.CustomFrom, filter.CustomTo);
        return from is { } start && to is { } end
            ? $"{start.ToString("d", culture)} – {end.ToString("d", culture)}"
            : _localizer["Sales_AllDates"];
    }

    private void ShowDetail(SaleRowViewModel? row)
    {
        DetailLines.Clear();
        DetailPayments.Clear();
        HasSelection = row is not null;
        if (row is null)
        {
            return;
        }

        var sale = row.Sale;
        var culture = _localizer.Culture;
        DetailReceipt = sale.ReceiptNumber;
        DetailStatus = sale.Status;
        DetailStatusKind = row.StatusKind;
        DetailStatusText = row.StatusText;
        DetailDateText = row.DateText;
        DetailStaffText = _localizer.Format("Sales_StaffLine", sale.Staff);
        DetailItemsHeader = _localizer.Format("Sales_ItemsHeader", sale.ItemCount.ToString("N0", culture));

        foreach (var line in sale.Lines)
        {
            DetailLines.Add(new SaleDetailLineViewModel(
                line.Name,
                line.Sku,
                CurrencyFormat.Amount(line.UnitPrice, Currency, culture),
                line.Quantity > 1 ? $"x{line.Quantity.ToString(culture)}" : string.Empty,
                CurrencyFormat.Amount(line.LineTotal, Currency, culture),
                DetailLines.Count % 2 == 1));
        }

        DetailSubtotalText = CurrencyFormat.Amount(sale.Totals.Subtotal, Currency, culture);
        DetailDiscountText = "-" + CurrencyFormat.Amount(sale.Totals.Discount, Currency, culture);
        DetailTaxLabel = _localizer.Format("Sales_TaxLabel", sale.TaxPercent.ToString("0.##", culture));
        DetailTaxText = CurrencyFormat.Amount(sale.Totals.Tax, Currency, culture);
        DetailTotalText = CurrencyFormat.Amount(sale.Totals.Total, Currency, culture);

        foreach (var payment in sale.Payments)
        {
            var isBase = string.Equals(payment.Currency, Currency, StringComparison.OrdinalIgnoreCase);
            DetailPayments.Add(new SalePaymentViewModel(
                _localizer["Pos_Method_" + payment.Method],
                CurrencyFormat.Amount(payment.Amount, payment.Currency, culture),
                isBase ? string.Empty : _localizer.Format("Sales_PaymentRate", Currency, payment.Rate.ToString("0.####", culture), payment.Currency),
                !isBase));
        }
    }

    /// <summary>Opens the till (F1).</summary>
    [RelayCommand]
    private void NewSale() => _popups.Show(PopupKind.PointOfSale);

    /// <summary>Writes exactly the filtered table (every page) to a CSV file and records the export for the audit log.</summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (!CanExport || _filtered.Count == 0)
        {
            return;
        }

        var headers = new[]
        {
            _localizer["Sales_ColReceipt"],
            _localizer["Sales_ColDate"],
            _localizer["Sales_ColStaff"],
            _localizer["Sales_ColMethod"],
            _localizer["Sales_ColStatus"],
            _localizer["Sales_ColItems"],
            _localizer["Sales_ColTotal"],
            _localizer["Sales_ColTax"],
        };
        var csv = SalesCsv.Build(
            _filtered,
            headers,
            method => _localizer["Pos_Method_" + method],
            status => _localizer["Sales_Status_" + status]);

        var fileName = await _exporter.SaveAsync(_localizer["Sales_ExportFileName"], csv);
        if (fileName is null)
        {
            return;
        }

        _sales.RecordExport(_filtered.Count);
        NoticeText = _localizer.Format("Sales_Exported", _filtered.Count.ToString("N0", _localizer.Culture), fileName);
    }

    /// <summary>Prints the filtered table as a plain-text report.</summary>
    [RelayCommand]
    private async Task PrintReportAsync()
    {
        var culture = _localizer.Culture;
        var lines = new List<string>
        {
            _localizer["Sales_ReportTitle"],
            DateRangeText,
            string.Empty,
            $"{_localizer["Sales_KpiTotal"]}: {TotalSalesText}",
            $"{_localizer["Sales_KpiTransactions"]}: {TransactionsText}",
            $"{_localizer["Sales_KpiItems"]}: {ItemsSoldText}",
            $"{_localizer["Sales_KpiAverage"]}: {AverageSaleText}",
            string.Empty,
        };
        lines.AddRange(_filtered.Select(sale =>
            $"{sale.ReceiptNumber}  {SaleRowViewModel.FormatDate(sale.SoldAt, culture)}  {sale.ItemCount.ToString(culture)}  "
            + $"{CurrencyFormat.Amount(sale.Totals.Total, Currency, culture)}  {_localizer["Pos_Method_" + sale.PrimaryMethod]}  "
            + $"{sale.Staff}  {_localizer["Sales_Status_" + sale.Status]}"));

        await PrintAsync(_localizer["Sales_ReportTitle"], string.Join(Environment.NewLine, lines));
    }

    /// <summary>Prints the receipt of the selected sale (F8).</summary>
    [RelayCommand]
    private async Task PrintReceiptAsync()
    {
        if (Selected is { } row)
        {
            await PrintReceiptOfAsync(row);
        }
    }

    public async Task PrintReceiptOfAsync(SaleRowViewModel row)
    {
        var sale = row.Sale;
        var culture = _localizer.Culture;
        var lines = new List<string> { sale.ReceiptNumber, row.DateText, _localizer.Format("Sales_StaffLine", sale.Staff), string.Empty };
        lines.AddRange(sale.Lines.Select(line =>
            $"{line.Name}  {line.Quantity.ToString(culture)} x {CurrencyFormat.Amount(line.UnitPrice, Currency, culture)}"));
        lines.Add(string.Empty);
        lines.Add($"{_localizer["Sales_Subtotal"]}: {CurrencyFormat.Amount(sale.Totals.Subtotal, Currency, culture)}");
        lines.Add($"{_localizer["Sales_Discount"]}: -{CurrencyFormat.Amount(sale.Totals.Discount, Currency, culture)}");
        lines.Add($"{_localizer.Format("Sales_TaxLabel", sale.TaxPercent.ToString("0.##", culture))}: {CurrencyFormat.Amount(sale.Totals.Tax, Currency, culture)}");
        lines.Add($"{_localizer["Sales_Total"]}: {CurrencyFormat.Amount(sale.Totals.Total, Currency, culture)}");
        lines.Add(string.Empty);
        lines.AddRange(sale.Payments.Select(payment =>
            $"{_localizer["Pos_Method_" + payment.Method]}: {CurrencyFormat.Amount(payment.Amount, payment.Currency, culture)}"));

        await PrintAsync(_localizer.Format("Sales_ReceiptTitle", sale.ReceiptNumber), string.Join(Environment.NewLine, lines));
    }

    private async Task PrintAsync(string title, string text)
    {
        NoticeText = await _printer.PrintAsync(title, text) ? string.Empty : _localizer["Sales_PrintUnavailable"];
    }

    /// <summary>Opens the detail of a row (the row menu's View).</summary>
    public void View(SaleRowViewModel row) => Selected = row;

    /// <summary>A click on a row: opens its detail, updates it from another row, or closes it when that row is already open.</summary>
    public void ToggleRow(SaleRowViewModel row) => Selected = ReferenceEquals(Selected, row) ? null : row;

    /// <summary>Closes the floating detail and clears the row highlight.</summary>
    [RelayCommand]
    private void CloseDetail() => Selected = null;

    /// <summary>Starts a return from a sale: opens the returns pop-up with this receipt already looked up.</summary>
    public void StartReturn(SaleRowViewModel row)
    {
        if (SalesHistory.CanReturn(row.Sale))
        {
            _returns.PreloadReceipt(row.ReceiptNumber);
            _popups.Show(PopupKind.Returns);
        }
    }

    /// <summary>Whether the row menu offers a return for this sale.</summary>
    public static bool CanReturn(SaleRowViewModel row) => SalesHistory.CanReturn(row.Sale);
}
