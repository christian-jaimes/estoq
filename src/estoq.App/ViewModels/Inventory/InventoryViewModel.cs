using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.App.ViewModels.AddProduct;
using Estoq.Core.Inventory;
using Estoq.Core.Products;
using Estoq.Core.Roles;
using Estoq.Core.Settings;

namespace Estoq.App.ViewModels.Inventory;

/// <summary>
/// The Inventory page (docs/features/inventory.md): KPI cards, tabs, filters, sorting, the paged product table and the
/// detail panel of the selected product. A singleton, so filters and the selection survive a visit to another page.
/// </summary>
public sealed partial class InventoryViewModel : ObservableObject
{
    /// <summary>Rows added each time the table is scrolled near its end.</summary>
    public const int PageSize = 25;

    private readonly IInventoryService _inventory;
    private readonly ICatalogService _catalog;
    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;
    private readonly IPopupService _popups;
    private readonly ICsvExportService _export;
    private readonly ShellViewModel _shell;
    private readonly AddProductViewModel _addProduct;
    private readonly IStockSettings _stockSettings;

    private List<InventoryProduct> _products = [];
    private List<InventoryProduct> _filtered = [];
    private InventoryTab _tab = InventoryTab.All;
    private InventoryDetailTab _detailTab = InventoryDetailTab.Overview;
    private InventorySort _sort = InventorySort.Name;
    private readonly HashSet<string> _categories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _brands = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _types = new(StringComparer.OrdinalIgnoreCase);
    private string? _selectedId;
    private int _shown = PageSize;
    private bool _active;

    public InventoryViewModel(
        IInventoryService inventory,
        ICatalogService catalog,
        ILocalizer localizer,
        ISessionService session,
        IPopupService popups,
        ICsvExportService export,
        ShellViewModel shell,
        AddProductViewModel addProduct,
        IStockSettings stockSettings)
    {
        _inventory = inventory;
        _catalog = catalog;
        _localizer = localizer;
        _session = session;
        _popups = popups;
        _export = export;
        _shell = shell;
        _addProduct = addProduct;
        _stockSettings = stockSettings;

        // The top bar search (Ctrl+K) filters this page while it is showing.
        _shell.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.SearchText))
            {
                OnPropertyChanged(nameof(SearchText));
            }

            if (_active && e.PropertyName == nameof(ShellViewModel.SearchText))
            {
                _shown = PageSize;
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
    public partial ProductDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial bool HasDetail { get; set; }

    [ObservableProperty]
    public partial bool CanExport { get; set; }

    /// <summary>Managers and owners can edit a product's details and prices.</summary>
    [ObservableProperty]
    public partial bool CanEdit { get; set; }

    [ObservableProperty]
    public partial string ExportMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasExportMessage { get; set; }

    [ObservableProperty]
    public partial string CategoryLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BrandLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TypeLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SortLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SortDirectionGlyph { get; set; } = "";

    [ObservableProperty]
    public partial string SortDirectionToolTip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasActiveFilters { get; set; }

    /// <summary>The search box on this page shares its text with the top bar search.</summary>
    public string SearchText
    {
        get => _shell.SearchText;
        set => _shell.SearchText = value ?? string.Empty;
    }

    public bool IsInStockFilter => _tab == InventoryTab.In;

    public bool IsLowFilter => _tab == InventoryTab.Low;

    public bool IsOutFilter => _tab == InventoryTab.Out;

    /// <summary>Total products / total variants cards: clear the stock filter.</summary>
    [RelayCommand]
    private void ShowAll() => SelectTab(InventoryTab.All);

    /// <summary>The stock cards filter the table; clicking the active card again goes back to all.</summary>
    [RelayCommand]
    private void FilterInStock() => SelectTab(_tab == InventoryTab.In ? InventoryTab.All : InventoryTab.In);

    [RelayCommand]
    private void FilterLow() => SelectTab(_tab == InventoryTab.Low ? InventoryTab.All : InventoryTab.Low);

    [RelayCommand]
    private void FilterOut() => SelectTab(_tab == InventoryTab.Out ? InventoryTab.All : InventoryTab.Out);

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

        // Entering the page starts with nothing selected, so no detail panel is open.
        _selectedId = null;
        Refresh();
    }

    public void Deactivate() => _active = false;

    // Menu contents. The page builds its flyouts from these when they open.

    // Each list is limited to what the other filters leave, plus anything already ticked so it can be unticked.
    public IReadOnlyList<string> CategoryValues => Distinct(Filter(true, false, false).Select(p => p.Category).Concat(_categories));

    public IReadOnlyList<string> TypeValues => Distinct(Filter(false, true, false).Select(p => p.ProductType).Concat(_types));

    public IReadOnlyList<string> BrandValues => Distinct(Filter(false, false, true).Select(p => p.Brand).Concat(_brands));

    private static List<string> Distinct(IEnumerable<string> values) =>
        values.Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase).ToList();

    public IReadOnlyList<FilterOption> SortOptions =>
    [
        new FilterOption(nameof(InventorySort.Name), _localizer["Inventory_SortName"]),
        new FilterOption(nameof(InventorySort.Quantity), _localizer["Inventory_SortQuantity"]),
        new FilterOption(nameof(InventorySort.Price), _localizer["Inventory_SortPrice"]),
        new FilterOption(nameof(InventorySort.Updated), _localizer["Inventory_SortUpdated"]),
    ];

    /// <summary>The checked values of each multi-select filter. The dropdowns edit these sets and then call <see cref="FiltersChanged"/>.</summary>
    public ISet<string> SelectedCategories => _categories;

    public ISet<string> SelectedBrands => _brands;

    public ISet<string> SelectedTypes => _types;

    public string SelectedSort => _sort.ToString();

    public bool SortDescending { get; private set; }

    [RelayCommand]
    private void AddProduct() => _shell.OpenAddItemsCommand.Execute(null);

    [RelayCommand]
    private void ToggleSortDirection()
    {
        SortDescending = !SortDescending;
        _shown = PageSize;
        Apply();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _categories.Clear();
        _brands.Clear();
        _types.Clear();
        _tab = InventoryTab.All;
        _shell.SearchText = string.Empty;
        _shown = PageSize;
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
        // Exactly what the table shows: the filtered list up to the rows loaded so far.
        var rows = _filtered.Take(Rows.Count).Select(product => (IReadOnlyList<string>)
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

    /// <summary>Edit button of the detail panel: opens the Add product pop-up in edit mode for the selected product.</summary>
    [RelayCommand]
    private void EditProduct() => OpenEditor(_selectedId);

    /// <summary>Double-click on a row: opens the editor for that product.</summary>
    public void EditRow(InventoryRowViewModel? row)
    {
        if (row is not null)
        {
            OpenEditor(row.Id);
        }
    }

    private void OpenEditor(string? productId)
    {
        if (!CanEdit || productId is null)
        {
            return;
        }

        var product = _products.FirstOrDefault(p => p.Id == productId);
        if (product is null)
        {
            return;
        }

        // The detail panel closes while the editor is open, so the page is clean when the editor closes.
        CloseDetail();
        _addProduct.PrepareEdit(product);
        _popups.Show(PopupKind.AddItems);
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

    /// <summary>Called by a multi-select dropdown after it ticked or unticked a value.</summary>
    public void FiltersChanged()
    {
        _shown = PageSize;
        Apply();
    }

    public void SelectSort(string? value)
    {
        _sort = Enum.TryParse<InventorySort>(value, out var parsed) ? parsed : InventorySort.Name;
        _shown = PageSize;
        Apply();
    }

    /// <summary>True while more products match than the table lists so far.</summary>
    public bool HasMore => _shown < _filtered.Count;

    /// <summary>Lazy load: called when the table is scrolled near its end; appends the next batch of rows.</summary>
    public void LoadMore()
    {
        if (!HasMore)
        {
            return;
        }

        var from = Math.Min(_shown, _filtered.Count);
        _shown += PageSize;
        AddRows(from, Math.Min(_shown, _filtered.Count));
        UpdateShowing();
    }

    /// <summary>Nothing ticked: "All ..."; one: its name; several: how many.</summary>
    private string FilterLabel(HashSet<string> selected, string allKey) => selected.Count switch
    {
        0 => _localizer[allKey],
        1 => selected.First(),
        _ => _localizer.Format("Inventory_FilterSelected", selected.Count.ToString("N0", _localizer.Culture)),
    };

    private void SelectTab(InventoryTab tab)
    {
        if (_tab == tab)
        {
            return;
        }

        _tab = tab;
        _shown = PageSize;
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
        OnPropertyChanged(nameof(IsInStockFilter));
        OnPropertyChanged(nameof(IsLowFilter));
        OnPropertyChanged(nameof(IsOutFilter));
    }

    private void Refresh()
    {
        CanExport = RolePermissions.IsAllowed(_session.Role, Permission.ExportCsv);
        CanEdit = RolePermissions.IsAllowed(_session.Role, Permission.EditProductDetailsAndPrices);
        _products = _inventory.GetProducts().ToList();
        Apply();
    }

    /// <summary>Applies the tab, filters, search and sort, then shows the current page.</summary>
    /// <summary>
    /// The products that pass the active filters, the KPI card and the search. A dropdown asks for the list without its own
    /// filter, so it offers only values that still exist among what the other filters leave.
    /// </summary>
    private IEnumerable<InventoryProduct> Filter(bool skipCategory, bool skipType, bool skipBrand)
    {
        var search = _active ? ProductNaming.Key(_shell.SearchText) : string.Empty;
        IEnumerable<InventoryProduct> query = _products;

        if (!skipCategory && _categories.Count > 0)
        {
            query = query.Where(p => _categories.Contains(p.Category));
        }

        if (!skipType && _types.Count > 0)
        {
            query = query.Where(p => _types.Contains(p.ProductType));
        }

        if (!skipBrand && _brands.Count > 0)
        {
            query = query.Where(p => _brands.Contains(p.Brand));
        }

        if (_tab != InventoryTab.All)
        {
            // Same unit as the KPI counts: a product is listed when at least one of its variants has that status.
            var wanted = _tab switch
            {
                InventoryTab.In => StockLevel.InStock,
                InventoryTab.Low => StockLevel.LowStock,
                _ => StockLevel.NoStock,
            };
            query = query.Where(p => p.Variants.Any(v => StockLevels.Classify(v.OnHand, _stockSettings.LowStockThreshold) == wanted));
        }

        if (search.Length > 0)
        {
            query = query.Where(p => Matches(p, search));
        }

        return query;
    }

    private void Apply()
    {
        var culture = _localizer.Culture;
        var variants = _products.SelectMany(p => p.Variants.Select(v => StockLevels.Classify(v.OnHand, _stockSettings.LowStockThreshold))).ToList();
        TotalProductsText = _products.Count.ToString("N0", culture);
        TotalVariantsText = variants.Count.ToString("N0", culture);
        InStockText = variants.Count(s => s == StockLevel.InStock).ToString("N0", culture);
        LowStockText = variants.Count(s => s == StockLevel.LowStock).ToString("N0", culture);
        OutOfStockText = variants.Count(s => s == StockLevel.NoStock).ToString("N0", culture);

        var search = _active ? ProductNaming.Key(_shell.SearchText) : string.Empty;
        var query = Filter(skipCategory: false, skipType: false, skipBrand: false);

        _filtered = Sort(query).ToList();

        CategoryLabel = FilterLabel(_categories, "Inventory_AllCategories");
        TypeLabel = FilterLabel(_types, "Inventory_AllTypes");
        BrandLabel = FilterLabel(_brands, "Inventory_AllBrands");
        SortLabel = SortOptions.First(o => o.Value == _sort.ToString()).Label;
        SortDirectionGlyph = SortDescending ? "" : "";
        SortDirectionToolTip = _localizer[SortDescending ? "Inventory_SortDescending" : "Inventory_SortAscending"];
        HasActiveFilters = _categories.Count > 0 || _brands.Count > 0 || _types.Count > 0
            || _tab != InventoryTab.All || search.Length > 0;

        // The panel opens only when a row is clicked. It stays while its product is still listed, and closes when not.
        if (_selectedId is not null && _filtered.All(p => p.Id != _selectedId))
        {
            _selectedId = null;
        }

        var selectedIndex = _filtered.FindIndex(p => p.Id == _selectedId);
        if (selectedIndex >= _shown)
        {
            // A selection that survived a filter change stays in the list.
            _shown = ((selectedIndex / PageSize) + 1) * PageSize;
        }

        BuildRows();
    }

    private void BuildRows()
    {
        Rows.Clear();
        AddRows(0, Math.Min(_shown, _filtered.Count));
        IsEmpty = Rows.Count == 0;
        EmptyText = _localizer[HasActiveFilters ? "Inventory_EmptyFiltered" : "Inventory_Empty"];
        UpdateShowing();
        ShowDetail();
    }

    private void AddRows(int from, int to)
    {
        var culture = _localizer.Culture;
        var currency = _catalog.DefaultCurrencyCode;
        for (var index = from; index < to; index++)
        {
            var product = _filtered[index];
            var (kind, text) = StatusOf(product);
            Rows.Add(new InventoryRowViewModel(
                product.Id,
                product.Name,
                product.Code,
                product.Category,
                product.Brand,
                product.Variants.Count.ToString("N0", culture),
                product.Variants.Sum(v => v.OnHand).ToString("N0", culture),
                PriceRange(product.Variants.Select(v => v.SellingPrice).Distinct().ToList(), currency, culture),
                kind,
                text,
                index % 2 == 1,
                product.Id == _selectedId));
        }
    }

    private void UpdateShowing()
    {
        var culture = _localizer.Culture;
        ShowingText = _filtered.Count == 0
            ? _localizer["Inventory_ShowingNone"]
            : _localizer.Format("Inventory_Showing", Rows.Count.ToString("N0", culture), _filtered.Count.ToString("N0", culture));
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
        overview.Add(new DetailLine(_localizer["Inventory_DetailThreshold"], _stockSettings.LowStockThreshold.ToString("N0", culture), false));

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
        return StockLevels.ClassifyProduct(product.Variants.Select(v => v.OnHand), _stockSettings.LowStockThreshold) switch
        {
            StockLevel.NoStock => (StatusKinds.Danger, _localizer["Inventory_StatusOut"]),
            StockLevel.LowStock => (StatusKinds.Warning, _localizer["Inventory_StatusLow"]),
            _ => (StatusKinds.Success, _localizer["Inventory_StatusInStock"]),
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
