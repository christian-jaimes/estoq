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
    private DateRange? _dates;
    private int _shown = BatchSize;

    // The ticked values of the multi-select filters (the dropdowns edit these sets), and the sort.
    private readonly HashSet<string> _methods = [];
    private readonly HashSet<string> _staff = [];
    private SalesSort _sort = SalesSort.Date;
    private bool _sortDescending = true;

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

        CanExport = RolePermissions.IsAllowed(_session.Role, Permission.ExportCsv);
        Reload();
        _localizer.LanguageChanged += OnLanguageChanged;
    }

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
    public partial string MethodLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StaffLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SortLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SortDirectionGlyph { get; set; } = "";

    [ObservableProperty]
    public partial string SortDirectionToolTip { get; set; } = string.Empty;

    /// <summary>True when a filter, the search or a non-default date range is active: shows "Clear all".</summary>
    [ObservableProperty]
    public partial bool HasActiveFilters { get; set; }

    /// <summary>The date filter button's text: "All dates", or "Date: first – last".</summary>
    [ObservableProperty]
    public partial string DateButtonText { get; set; } = string.Empty;

    /// <summary>True while a date range filters the table: the button takes the active (gold) look.</summary>
    [ObservableProperty]
    public partial bool HasDateFilter { get; set; }

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
        // Method ticks are held as words of the old language; staff names are the same in every language.
        _methods.Clear();
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

    /// <summary>The language of the dates shown.</summary>
    public CultureInfo Culture => _localizer.Culture;

    /// <summary>The date range the table is limited to, or null for all dates.</summary>
    public DateRange? Dates => _dates;

    /// <summary>Applies the range chosen in the date picker (null = all dates).</summary>
    public void ApplyDates(DateRange? range)
    {
        _dates = range;
        _shown = BatchSize;
        Reload();
    }

    // Dropdown contents. The page builds its flyouts from these when they open.

    public IReadOnlyList<string> MethodValues => PaymentMethods.All.Select(MethodWord).ToList();

    public IReadOnlyList<string> StaffValues =>
        SalesHistory.Apply(_sales.GetSales(), new SalesFilter(), _session.Role, _session.UserName ?? string.Empty, Today)
            .Select(sale => sale.Staff)
            .Concat(_staff)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>The ticked values of each multi-select filter. The dropdowns edit these sets and then call <see cref="FiltersChanged"/>.</summary>
    public ISet<string> SelectedMethods => _methods;

    public ISet<string> SelectedStaff => _staff;

    public IReadOnlyList<FilterOption> SortOptions =>
    [
        new FilterOption(nameof(SalesSort.Date), _localizer["Sales_SortDate"]),
        new FilterOption(nameof(SalesSort.Receipt), _localizer["Sales_SortReceipt"]),
        new FilterOption(nameof(SalesSort.Staff), _localizer["Sales_SortStaff"]),
        new FilterOption(nameof(SalesSort.Total), _localizer["Sales_SortTotal"]),
        new FilterOption(nameof(SalesSort.Items), _localizer["Sales_SortItems"]),
        new FilterOption(nameof(SalesSort.Tax), _localizer["Sales_SortTax"]),
        new FilterOption(nameof(SalesSort.Status), _localizer["Sales_SortStatus"]),
    ];

    public string SelectedSort => _sort.ToString();

    private string MethodWord(PaymentMethod method) => _localizer["Pos_Method_" + method];

    /// <summary>Called by a multi-select dropdown after it ticked or unticked a value.</summary>
    public void FiltersChanged() => OnFilterChanged();

    public void SelectSort(string? value)
    {
        _sort = Enum.TryParse<SalesSort>(value, out var parsed) ? parsed : SalesSort.Date;
        OnFilterChanged();
    }

    [RelayCommand]
    private void ToggleSortDirection()
    {
        _sortDescending = !_sortDescending;
        OnFilterChanged();
    }

    /// <summary>Clears every filter, the search and the date range (back to all sales). The sort stays as the user set it.</summary>
    [RelayCommand]
    private void ClearFilters()
    {
        _building = true;
        _methods.Clear();
        _staff.Clear();
        SearchText = string.Empty;
        _building = false;

        _dates = null;
        _shown = BatchSize;
        Reload();
    }

    private string FilterLabel(HashSet<string> ticked, string allKey) => ticked.Count switch
    {
        0 => _localizer[allKey],
        1 => ticked.First(),
        _ => _localizer.Format("Sales_FilterSelected", ticked.Count.ToString("N0", _localizer.Culture)),
    };

    private SalesFilter BuildFilter() => new(
        _dates,
        SearchText,
        PaymentMethods.All.Where(method => _methods.Contains(MethodWord(method))).ToList(),
        _staff.ToList());

    /// <summary>Applies the filters, then rebuilds the KPIs, the page of rows, the pager and the detail panel.</summary>
    private void Reload()
    {
        var culture = _localizer.Culture;
        var filter = BuildFilter();
        _filtered = SalesHistory.Sort(
            SalesHistory.Apply(_sales.GetSales(), filter, _session.Role, _session.UserName ?? string.Empty, Today),
            _sort,
            _sortDescending);

        MethodLabel = FilterLabel(_methods, "Sales_AllMethods");
        StaffLabel = FilterLabel(_staff, "Sales_AllStaff");
        SortLabel = SortOptions.First(option => option.Value == _sort.ToString()).Label;
        SortDirectionGlyph = _sortDescending ? "" : "";
        SortDirectionToolTip = _localizer[_sortDescending ? "Sales_SortDescending" : "Sales_SortAscending"];
        HasActiveFilters = _methods.Count > 0 || _staff.Count > 0
            || SearchText.Trim().Length > 0 || _dates is not null;

        // Figures describe everything the filters match, not only the page on screen.
        var summary = SalesSummary.Of(_filtered);
        TotalSalesText = CurrencyFormat.Amount(summary.TotalSales, Currency, culture);
        TransactionsText = summary.Transactions.ToString("N0", culture);
        ItemsSoldText = summary.ItemsSold.ToString("N0", culture);
        AverageSaleText = CurrencyFormat.Amount(summary.AverageSale, Currency, culture);
        DateRangeText = BuildRangeText(culture);
        HasDateFilter = _dates is not null;
        DateButtonText = _dates is { } dates
            ? _localizer.Format("Sales_DateLabel", dates.From.ToString("d", culture), dates.To.ToString("d", culture))
            : _localizer["Sales_AllDates"];

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

    private string BuildRangeText(CultureInfo culture) =>
        _dates is { } dates
            ? $"{dates.From.ToString("d", culture)} – {dates.To.ToString("d", culture)}"
            : _localizer["Sales_AllDates"];

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
