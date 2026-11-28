using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Inventory;
using Estoq.Core.Products;
using Estoq.Core.Roles;

namespace Estoq.App.ViewModels.Inventory;

/// <summary>
/// The Inventory page (docs/features/inventory.md): KPI cards, tabs, filters, sorting, the paged product table and the
/// detail panel of the selected product. A singleton, so filters and the selection survive a visit to another page.
/// </summary>
public sealed partial class InventoryViewModel : ObservableObject
{
    public const int PageSize = 10;

    private readonly IInventoryService _inventory;
    private readonly ICatalogService _catalog;
    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;
    private readonly IPopupService _popups;
    private readonly ICsvExportService _export;
    private readonly ShellViewModel _shell;

    private List<InventoryProduct> _products = [];
    private List<InventoryProduct> _filtered = [];
    private InventoryTab _tab = InventoryTab.All;
    private InventoryDetailTab _detailTab = InventoryDetailTab.Overview;
    private InventorySort _sort = InventorySort.Name;
    private string? _category;
    private string? _brand;
    private string? _selectedId;
    private int _page = 1;
    private bool _active;

    public InventoryViewModel(
        IInventoryService inventory,
        ICatalogService catalog,
        ILocalizer localizer,
        ISessionService session,
        IPopupService popups,
        ICsvExportService export,
        ShellViewModel shell)
    {
        _inventory = inventory;
        _catalog = catalog;
        _localizer = localizer;
        _session = session;
        _popups = popups;
        _export = export;
        _shell = shell;

        // The top bar search (Ctrl+K) filters this page while it is showing.
        _shell.PropertyChanged += (_, e) =>
        {
            if (_active && e.PropertyName == nameof(ShellViewModel.SearchText))
            {
                _page = 1;
                Apply();
            }
        };

        // Adding a product from the F2 pop-up changes what the page lists.
        _popups.Changed += (_, _) =>
        {
            if (_active && _popups.Current is null)
            {
                Refresh();
            }
        };

        _localizer.LanguageChanged += (_, _) =>
        {
            if (_active)
            {
                Apply();
            }
        };
    }

    public ObservableCollection<InventoryRowViewModel> Rows { get; } = [];

    public ObservableCollection<PageItemViewModel> PageItems { get; } = [];

    [ObservableProperty]
    public partial string TotalProductsText { get; set; } = "0";

    [ObservableProperty]
    public partial string TotalVariantsText { get; set; } = "0";

    [ObservableProperty]
    public partial string InStockText { get; set; } = "0";

    [ObservableProperty]
    public partial string LowStockText { get; set; } = "0";

    [ObservableProperty]
    public partial string OutOfStockText { get; set; } = "0";

    [ObservableProperty]
    public partial string ShowingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmptyText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool CanGoPrevious { get; set; }

    [ObservableProperty]
    public partial bool CanGoNext { get; set; }

    [ObservableProperty]
    public partial ProductDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial bool HasDetail { get; set; }

    [ObservableProperty]
    public partial bool CanExport { get; set; }

    [ObservableProperty]
    public partial string ExportMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasExportMessage { get; set; }

    [ObservableProperty]
    public partial string CategoryLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BrandLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SortLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SortDirectionGlyph { get; set; } = "";

    [ObservableProperty]
    public partial string SortDirectionToolTip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasActiveFilters { get; set; }

    public bool IsAllTab
    {
        get => _tab == InventoryTab.All;
        set { if (value) SelectTab(InventoryTab.All); }
    }

    public bool IsLowTab
    {
        get => _tab == InventoryTab.Low;
        set { if (value) SelectTab(InventoryTab.Low); }
    }

    public bool IsOutTab
    {
        get => _tab == InventoryTab.Out;
        set { if (value) SelectTab(InventoryTab.Out); }
    }

    public bool IsOverviewTab
    {
        get => _detailTab == InventoryDetailTab.Overview;
        set { if (value) SelectDetailTab(InventoryDetailTab.Overview); }
    }

    public bool IsVariantsTab
    {
        get => _detailTab == InventoryDetailTab.Variants;
        set { if (value) SelectDetailTab(InventoryDetailTab.Variants); }
    }

    public bool IsStockTab
    {
        get => _detailTab == InventoryDetailTab.Stock;
        set { if (value) SelectDetailTab(InventoryDetailTab.Stock); }
    }

    public bool IsHistoryTab
    {
        get => _detailTab == InventoryDetailTab.History;
        set { if (value) SelectDetailTab(InventoryDetailTab.History); }
    }

    /// <summary>Called when the page is shown: reads the data fresh and starts reacting to the top bar search.</summary>
    public void Activate()
    {
        _active = true;
        Refresh();
    }

    public void Deactivate() => _active = false;

    // Menu contents. The page builds its flyouts from these when they open.

    public IReadOnlyList<FilterOption> CategoryOptions =>
        [new FilterOption(null, _localizer["Inventory_AllCategories"]), .. _products.Select(p => p.Category).Where(c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase).Select(c => new FilterOption(c, c))];

    public IReadOnlyList<FilterOption> BrandOptions =>
        [new FilterOption(null, _localizer["Inventory_AllBrands"]), .. _products.Select(p => p.Brand).Where(b => b.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b, StringComparer.CurrentCultureIgnoreCase).Select(b => new FilterOption(b, b))];

    public IReadOnlyList<FilterOption> SortOptions =>
    [
        new FilterOption(nameof(InventorySort.Name), _localizer["Inventory_SortName"]),
        new FilterOption(nameof(InventorySort.Quantity), _localizer["Inventory_SortQuantity"]),
        new FilterOption(nameof(InventorySort.Price), _localizer["Inventory_SortPrice"]),
        new FilterOption(nameof(InventorySort.Updated), _localizer["Inventory_SortUpdated"]),
    ];

    public string? SelectedCategory => _category;

    public string? SelectedBrand => _brand;

    public string SelectedSort => _sort.ToString();

    public bool SortDescending { get; private set; }

    [RelayCommand]
    private void AddProduct() => _shell.OpenAddItemsCommand.Execute(null);

    [RelayCommand]
    private void PreviousPage() => GoToPage(_page - 1);

    [RelayCommand]
    private void NextPage() => GoToPage(_page + 1);

    [RelayCommand]
    private void ToggleSortDirection()
    {
        SortDescending = !SortDescending;
        _page = 1;
        Apply();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _category = null;
        _brand = null;
        _tab = InventoryTab.All;
        _shell.SearchText = string.Empty;
        _page = 1;
        RaiseTabChanged();
        Apply();
    }

    /// <summary>Writes exactly the filtered table (every page) to a CSV file of the user's choice.</summary>
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (!CanExport)
        {
            return;
        }

        var culture = _localizer.Culture;
        string[] headers =
        [
            _localizer["Inventory_ColProduct"],
            _localizer["Inventory_ColCode"],
            _localizer["Inventory_ColCategory"],
            _localizer["Inventory_ColBrand"],
            _localizer["Inventory_ColVariants"],
            _localizer["Inventory_ColStock"],
            _localizer["Inventory_ColStatus"],
        ];
        var rows = _filtered.Select(product => (IReadOnlyList<string>)
        [
            product.Name,
            product.Code,
            product.Category,
            product.Brand,
            product.Variants.Count.ToString(culture),
            product.Variants.Sum(v => v.OnHand).ToString(culture),
            StatusOf(product).Text,
        ]).ToList();

        var csv = CsvText.Build(headers, rows);
        var fileName = $"inventory-{DateTime.Now:yyyy-MM-dd}";
        string? saved;
        try
        {
            saved = await _export.SaveAsync(fileName, csv);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetExportMessage(_localizer["Inventory_ExportFailed"]);
            return;
        }

        if (saved is not null)
        {
            // TODO(back end): an export writes an audit log entry (docs/product/roles-and-permissions.md).
            SetExportMessage(_localizer.Format("Inventory_ExportDone", rows.Count.ToString("N0", culture), saved));
        }
    }

    /// <summary>Closes the floating detail panel and clears the row highlight.</summary>
    [RelayCommand]
    private void CloseDetail()
    {
        _selectedId = null;
        foreach (var item in Rows)
        {
            item.IsSelected = false;
        }

        ShowDetail();
    }

    /// <summary>A click on a row: opens its panel, updates the panel from another row, or closes it when the row is already open.</summary>
    public void ToggleRow(InventoryRowViewModel? row)
    {
        if (row is not null && HasDetail && _selectedId == row.Id)
        {
            CloseDetail();
            return;
        }

        Select(row);
    }

    public void Select(InventoryRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        _selectedId = row.Id;
        foreach (var item in Rows)
        {
            item.IsSelected = item.Id == row.Id;
        }

        ShowDetail();
    }

    public void SelectCategory(string? value)
    {
        _category = value;
        _page = 1;
        Apply();
    }

    public void SelectBrand(string? value)
    {
        _brand = value;
        _page = 1;
        Apply();
    }

    public void SelectSort(string? value)
    {
        _sort = Enum.TryParse<InventorySort>(value, out var parsed) ? parsed : InventorySort.Name;
        _page = 1;
        Apply();
    }

    public void GoToPage(int page)
    {
        var last = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        var target = Math.Clamp(page, 1, last);
        if (target == _page)
        {
            return;
        }

        _page = target;
        BuildPage();
    }

    private void SelectTab(InventoryTab tab)
    {
        if (_tab == tab)
        {
            return;
        }

        _tab = tab;
        _page = 1;
        RaiseTabChanged();
        Apply();
    }

    private void SelectDetailTab(InventoryDetailTab tab)
    {
        if (_detailTab == tab)
        {
            return;
        }

        _detailTab = tab;
        OnPropertyChanged(nameof(IsOverviewTab));
        OnPropertyChanged(nameof(IsVariantsTab));
        OnPropertyChanged(nameof(IsStockTab));
        OnPropertyChanged(nameof(IsHistoryTab));
    }

    private void RaiseTabChanged()
    {
        OnPropertyChanged(nameof(IsAllTab));
        OnPropertyChanged(nameof(IsLowTab));
        OnPropertyChanged(nameof(IsOutTab));
    }

    private void Refresh()
    {
        CanExport = RolePermissions.IsAllowed(_session.Role, Permission.ExportCsv);
        _products = _inventory.GetProducts().ToList();
        Apply();
    }

    /// <summary>Applies the tab, filters, search and sort, then shows the current page.</summary>
    private void Apply()
    {
        var culture = _localizer.Culture;
        var variants = _products.SelectMany(p => p.Variants.Select(v => StockLevels.Classify(v.OnHand, p.LowStockThreshold))).ToList();
        TotalProductsText = _products.Count.ToString("N0", culture);
        TotalVariantsText = variants.Count.ToString("N0", culture);
        InStockText = variants.Count(s => s == StockStatus.InStock).ToString("N0", culture);
        LowStockText = variants.Count(s => s == StockStatus.Low).ToString("N0", culture);
        OutOfStockText = variants.Count(s => s == StockStatus.Out).ToString("N0", culture);

        var search = _active ? ProductNaming.Key(_shell.SearchText) : string.Empty;
        IEnumerable<InventoryProduct> query = _products;

        if (_category is not null)
        {
            query = query.Where(p => string.Equals(p.Category, _category, StringComparison.OrdinalIgnoreCase));
        }

        if (_brand is not null)
        {
            query = query.Where(p => string.Equals(p.Brand, _brand, StringComparison.OrdinalIgnoreCase));
        }

        if (_tab != InventoryTab.All)
        {
            query = query.Where(p =>
            {
                var status = StockLevels.ClassifyProduct(p.Variants.Select(v => v.OnHand), p.LowStockThreshold);
                return _tab == InventoryTab.Low ? status == StockStatus.Low : status == StockStatus.Out;
            });
        }

        if (search.Length > 0)
        {
            query = query.Where(p => Matches(p, search));
        }

        _filtered = Sort(query).ToList();

        CategoryLabel = _category ?? _localizer["Inventory_AllCategories"];
        BrandLabel = _brand ?? _localizer["Inventory_AllBrands"];
        SortLabel = SortOptions.First(o => o.Value == _sort.ToString()).Label;
        SortDirectionGlyph = SortDescending ? "" : "";
        SortDirectionToolTip = _localizer[SortDescending ? "Inventory_SortDescending" : "Inventory_SortAscending"];
        HasActiveFilters = _category is not null || _brand is not null
            || _tab != InventoryTab.All || search.Length > 0;

        // The panel opens only when a row is clicked. It stays while its product is still listed, and closes when not.
        if (_selectedId is not null && _filtered.All(p => p.Id != _selectedId))
        {
            _selectedId = null;
        }

        var selectedIndex = _filtered.FindIndex(p => p.Id == _selectedId);
        var last = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        _page = Math.Clamp(_page, 1, last);
        if (selectedIndex >= 0 && _page == 1 && selectedIndex >= PageSize)
        {
            // A selection that survived a filter change stays visible on the page it is on.
            _page = (selectedIndex / PageSize) + 1;
        }

        BuildPage();
    }

    private void BuildPage()
    {
        var culture = _localizer.Culture;
        var pageProducts = _filtered.Skip((_page - 1) * PageSize).Take(PageSize).ToList();

        Rows.Clear();
        for (var index = 0; index < pageProducts.Count; index++)
        {
            var product = pageProducts[index];
            var (kind, text) = StatusOf(product);
            Rows.Add(new InventoryRowViewModel(
                product.Id,
                product.Name,
                product.Code,
                product.Category,
                product.Brand,
                product.Variants.Count.ToString("N0", culture),
                product.Variants.Sum(v => v.OnHand).ToString("N0", culture),
                kind,
                text,
                index % 2 == 1,
                product.Id == _selectedId));
        }

        IsEmpty = Rows.Count == 0;
        EmptyText = _localizer[HasActiveFilters ? "Inventory_EmptyFiltered" : "Inventory_Empty"];
        var first = _filtered.Count == 0 ? 0 : ((_page - 1) * PageSize) + 1;
        var lastShown = first == 0 ? 0 : first + pageProducts.Count - 1;
        ShowingText = _filtered.Count == 0
            ? _localizer["Inventory_ShowingNone"]
            : _localizer.Format("Inventory_Showing", first.ToString("N0", culture), lastShown.ToString("N0", culture), _filtered.Count.ToString("N0", culture));

        var pages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
        CanGoPrevious = _page > 1;
        CanGoNext = _page < pages;
        BuildPageItems(pages);
        ShowDetail();
    }

    private void BuildPageItems(int pages)
    {
        PageItems.Clear();
        var shown = new SortedSet<int> { 1, pages, _page - 1, _page, _page + 1 };
        var previous = 0;
        foreach (var page in shown.Where(p => p >= 1 && p <= pages))
        {
            if (previous != 0 && page - previous > 1)
            {
                PageItems.Add(new PageItemViewModel("…", 0, false, true));
            }

            PageItems.Add(new PageItemViewModel(page.ToString("N0", _localizer.Culture), page, page == _page, false));
            previous = page;
        }
    }

    private void ShowDetail()
    {
        var product = _selectedId is null ? null : _products.FirstOrDefault(p => p.Id == _selectedId);
        if (product is null)
        {
            // Detail keeps its last content so the panel still shows it while it slides out.
            HasDetail = false;
            return;
        }

        var culture = _localizer.Culture;
        var currency = _catalog.DefaultCurrencyCode;
        var showCost = RolePermissions.IsAllowed(_session.Role, Permission.SeeCostsAndMargins);
        var total = product.Variants.Sum(v => v.OnHand);
        var sizes = product.Variants.Select(v => v.Size).Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var colours = product.Variants.Select(v => v.Colour).Where(c => !string.IsNullOrEmpty(c)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var prices = product.Variants.Select(v => v.SellingPrice).Distinct().ToList();
        var costs = product.Variants.Select(v => v.CostPrice).Distinct().ToList();

        var overview = new List<DetailLine>
        {
            new(_localizer["Inventory_ColCategory"], Dash(product.Category), false),
            new(_localizer["Inventory_DetailType"], Dash(product.ProductType), false),
            new(_localizer["Inventory_ColBrand"], Dash(product.Brand), false),
            new(_localizer["Inventory_DetailSupplier"], Dash(product.Supplier), false),
            new(_localizer["Inventory_DetailModel"], Dash(product.ModelLabel), false),
            new(_localizer["Inventory_DetailSellingPrice"], PriceRange(prices, currency, culture), false),
        };

        if (showCost)
        {
            overview.Add(new DetailLine(_localizer["Inventory_DetailCostPrice"], PriceRange(costs, currency, culture), false));
            string margin;
            if (prices.Count == 1 && costs.Count == 1)
            {
                var estimate = ProductPricing.Estimate(costs[0], prices[0]);
                var percent = estimate.Percent is { } value ? $" ({value.ToString("N2", culture)}%)" : string.Empty;
                margin = CurrencyFormat.Amount(estimate.Amount, currency, culture) + percent;
            }
            else
            {
                margin = _localizer["Inventory_Varies"];
            }

            overview.Add(new DetailLine(_localizer["Inventory_DetailMargin"], margin, false));
        }

        overview.Add(new DetailLine(_localizer["Inventory_ColVariants"], product.Variants.Count.ToString("N0", culture), true));
        if (sizes > 0)
        {
            overview.Add(new DetailLine(_localizer["Inventory_DetailSizes"], sizes.ToString("N0", culture), false));
        }

        if (colours > 0)
        {
            overview.Add(new DetailLine(_localizer["Inventory_DetailColours"], colours.ToString("N0", culture), false));
        }

        overview.Add(new DetailLine(_localizer["Inventory_ColStock"], total.ToString("N0", culture), false));
        overview.Add(new DetailLine(_localizer["Inventory_DetailThreshold"], product.LowStockThreshold.ToString("N0", culture), false));

        var variants = product.Variants
            .OrderBy(v => v.Size, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(v => v.Colour, StringComparer.CurrentCultureIgnoreCase)
            .Select((v, index) =>
            {
                return new VariantRowViewModel(
                    VariantTitle(v),
                    v.Sku,
                    v.Barcode,
                    v.Barcode.Length > 0,
                    v.OnHand.ToString("N0", culture),
                    CurrencyFormat.Amount(v.SellingPrice, currency, culture),
                    index % 2 == 1);
            })
            .ToList();

        var history = _inventory.GetMovements(product.Id)
            .Select((m, index) => new MovementRowViewModel(
                MovementName(m.Type),
                (m.Quantity > 0 ? "+" : string.Empty) + m.Quantity.ToString("N0", culture),
                m.Location,
                m.CreatedAt == default ? "—" : m.CreatedAt.ToString("d MMM yyyy, HH:mm", culture),
                index % 2 == 1))
            .ToList();

        Detail = new ProductDetailViewModel(product.Name, product.Code, overview, Swatches(product), variants, history);
        HasDetail = true;
    }

    private IEnumerable<InventoryProduct> Sort(IEnumerable<InventoryProduct> products)
    {
        var byName = StringComparer.Create(_localizer.Culture, ignoreCase: true);
        var ordered = _sort switch
        {
            InventorySort.Quantity => SortDescending
                ? products.OrderByDescending(p => p.Variants.Sum(v => v.OnHand))
                : products.OrderBy(p => p.Variants.Sum(v => v.OnHand)),
            InventorySort.Price => SortDescending
                ? products.OrderByDescending(p => p.Variants.Min(v => v.SellingPrice))
                : products.OrderBy(p => p.Variants.Min(v => v.SellingPrice)),
            InventorySort.Updated => SortDescending
                ? products.OrderByDescending(p => p.UpdatedAt)
                : products.OrderBy(p => p.UpdatedAt),
            _ => SortDescending
                ? products.OrderByDescending(p => p.Name, byName)
                : products.OrderBy(p => p.Name, byName),
        };

        // Equal values keep a stable, readable order.
        return ordered.ThenBy(p => p.Name, byName);
    }

    /// <summary>The search matches name, SKU, either barcode or supplier (docs/features/inventory.md).</summary>
    private static bool Matches(InventoryProduct product, string key) =>
        ProductNaming.Key(product.Name).Contains(key, StringComparison.Ordinal)
        || ProductNaming.Key(product.Brand).Contains(key, StringComparison.Ordinal)
        || ProductNaming.Key(product.Supplier).Contains(key, StringComparison.Ordinal)
        || ProductNaming.Key(product.Code).Contains(key, StringComparison.Ordinal)
        || product.Variants.Any(v => ProductNaming.Key(v.Sku).Contains(key, StringComparison.Ordinal)
            || ProductNaming.Key(v.Barcode).Contains(key, StringComparison.Ordinal));

    private (string Kind, string Text) StatusOf(InventoryProduct product)
    {
        return StockLevels.ClassifyProduct(product.Variants.Select(v => v.OnHand), product.LowStockThreshold) switch
        {
            StockStatus.Out => (StatusKinds.Danger, _localizer["Inventory_StatusOut"]),
            StockStatus.Low => (StatusKinds.Warning, _localizer["Inventory_StatusLow"]),
            _ => (StatusKinds.Success, _localizer["Inventory_StatusActive"]),
        };
    }


    /// <summary>A colour circle with the size inside for every variant, grouped by colour in size order.</summary>
    private List<VariantSwatchViewModel> Swatches(InventoryProduct product)
    {
        var sizeOrder = _catalog.Sizes.Select(size => size.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return product.Variants
            .OrderBy(v => v.Colour ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(v => v.Size is null ? -1 : sizeOrder.FindIndex(name => string.Equals(name, v.Size, StringComparison.OrdinalIgnoreCase)) is var index and >= 0 ? index : int.MaxValue)
            .ThenBy(v => v.Size, StringComparer.CurrentCultureIgnoreCase)
            .Select(v =>
            {
                var hex = _catalog.Colours.FirstOrDefault(c => string.Equals(c.Key, v.Colour, StringComparison.OrdinalIgnoreCase))?.Hex ?? string.Empty;
                var colour = v.Colour ?? _localizer["Inventory_NoColour"];
                var size = v.Size ?? _localizer["Inventory_OneSize"];
                return new VariantSwatchViewModel(hex, ShortSize(v.Size), SwatchInk.UseDarkText(hex), $"{colour} · {size}");
            })
            .ToList();
    }

    /// <summary>The size as written inside a circle: short sizes as they are, long ones such as "One Size" as initials ("OS").</summary>
    private static string ShortSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return string.Empty;
        }

        var text = size.Trim();
        return text.Length <= 5
            ? text
            : string.Concat(text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0])));
    }

    private string VariantTitle(InventoryVariant variant)
    {
        var parts = new[] { variant.Size, variant.Colour }.Where(part => !string.IsNullOrEmpty(part)).ToArray();
        return parts.Length == 0 ? _localizer["Inventory_OneSize"] : string.Join(" · ", parts);
    }

    private string MovementName(string type)
    {
        var key = "Inventory_Move_" + type;
        var text = _localizer[key];
        return text == key ? type : text;
    }

    private string PriceRange(IReadOnlyList<decimal> values, string currency, CultureInfo culture)
    {
        if (values.Count == 0)
        {
            return "—";
        }

        var low = values.Min();
        var high = values.Max();
        return low == high
            ? CurrencyFormat.Amount(low, currency, culture)
            : $"{CurrencyFormat.Amount(low, currency, culture)} – {CurrencyFormat.Amount(high, currency, culture)}";
    }

    private static string Dash(string text) => text.Length == 0 ? "—" : text;

    private void SetExportMessage(string message)
    {
        ExportMessage = message;
        HasExportMessage = true;
    }
}
