using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Products;
using Estoq.Core.Roles;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.AddProduct;

/// <summary>
/// State and rules of the Add product pop-up (F2, docs/features/add-product.md), the legacy flow: the owner picks master
/// data and types a model label; the name, variants, SKUs and internal barcodes are built from it by <c>estoq.Core</c>.
/// A singleton, like the wizard steps; <see cref="Reset"/> starts a fresh form each time the pop-up opens.
/// </summary>
public sealed partial class AddProductViewModel : ObservableObject
{
    private const int MaxImages = 30;

    private readonly ICatalogService _catalog;
    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;
    private readonly ILogoPickerService _imagePicker;

    private readonly Dictionary<string, VariantRowViewModel> _rows = [];
    private readonly HashSet<string> _removed = [];
    private bool _loading;
    private bool _touched;
    private string _sizesFor = string.Empty;
    private long _nextBarcode;

    public AddProductViewModel(ICatalogService catalog, ILocalizer localizer, ISessionService session, ILogoPickerService imagePicker)
    {
        _catalog = catalog;
        _localizer = localizer;
        _session = session;
        _imagePicker = imagePicker;
        Reset();
    }

    /// <summary>Raised after a product was saved; the pop-up closes.</summary>
    public event EventHandler? Saved;

    public ObservableCollection<string> ProductTypeNames { get; } = [];

    public ObservableCollection<string> CategoryNames { get; } = [];

    public ObservableCollection<string> BrandNames { get; } = [];

    public ObservableCollection<MasterDataSuggestion> FilteredProductTypes { get; } = [];

    public ObservableCollection<MasterDataSuggestion> FilteredCategories { get; } = [];

    public ObservableCollection<MasterDataSuggestion> FilteredBrands { get; } = [];

    public ObservableCollection<ChoiceItemViewModel> Sizes { get; } = [];

    /// <summary>Every colour, for the "More colours" list.</summary>
    public ObservableCollection<ChoiceItemViewModel> Colours { get; } = [];

    /// <summary>The swatches shown in the card: quick picks plus any other colour that is ticked.</summary>
    public ObservableCollection<ChoiceItemViewModel> VisibleColours { get; } = [];

    /// <summary>The colours matching the search box in "More colours".</summary>
    public ObservableCollection<ChoiceItemViewModel> FilteredColours { get; } = [];

    public ObservableCollection<VariantRowViewModel> Variants { get; } = [];

    public ObservableCollection<ProductImageViewModel> Images { get; } = [];

    public string CurrencyCode => _catalog.DefaultCurrencyCode;

    public CultureInfo PriceCulture => _localizer.Culture;

    /// <summary>Cost price and margin are hidden from cashiers (docs/product/roles-and-permissions.md).</summary>
    [ObservableProperty]
    public partial bool ShowCost { get; set; }

    [ObservableProperty]
    public partial string ProductType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Brand { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ModelLabel { get; set; } = string.Empty;

    /// <summary>Generated from brand, product type and model label until the owner edits it.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SupplierBarcode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CostText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SellingText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ColourSearch { get; set; } = string.Empty;

    /// <summary>Name of the existing product with the same brand, product type and model label; empty when none.</summary>
    [ObservableProperty]
    public partial string DuplicateName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DuplicateMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ImageSource? MainImage { get; set; }

    [ObservableProperty]
    public partial string ValidationText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GeneralDisclaimer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PricingDisclaimer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VariantsDisclaimer { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanSave { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial string SummaryCategory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SummaryBrand { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SummarySelling { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SummaryMargin { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMarginNegative { get; set; }

    [ObservableProperty]
    public partial string SummaryVariants { get; set; } = string.Empty;

    public bool HasDuplicate => DuplicateName.Length > 0;

    public bool ShowValidation => ValidationText.Length > 0 && !HasDuplicate;

    public bool ShowGeneralDisclaimer => GeneralDisclaimer.Length > 0;

    public bool ShowPricingDisclaimer => PricingDisclaimer.Length > 0;

    public bool ShowVariantsDisclaimer => VariantsDisclaimer.Length > 0;

    public bool HasMainImage => MainImage is not null;

    public bool HasNoMainImage => MainImage is null;

    public bool CanAddImage => Images.Count < MaxImages;

    public bool HasImages => Images.Count > 0;

    public string ImagePosition
    {
        get
        {
            var index = Images.ToList().FindIndex(i => ReferenceEquals(i.Preview, MainImage));
            return $"{Math.Max(0, index) + 1} / {Images.Count}";
        }
    }

    public bool HasVariants => Variants.Count > 0;

    public bool HasNoVariants => Variants.Count == 0;

    public bool IsMarginPositive => !IsMarginNegative;

    /// <summary>Starts a fresh form with the current master data and the signed-in role's permissions.</summary>
    public void Reset()
    {
        _loading = true;
        _touched = false;
        _rows.Clear();
        _removed.Clear();
        _sizesFor = string.Empty;
        _nextBarcode = _catalog.NextBarcodeSequence;

        Fill(ProductTypeNames, _catalog.ProductTypes.Select(t => t.Name));
        Fill(CategoryNames, _catalog.Categories);
        Fill(BrandNames, _catalog.Brands);
        FillSuggestions(FilteredProductTypes, ProductTypeNames);
        FillSuggestions(FilteredCategories, CategoryNames);
        FillSuggestions(FilteredBrands, BrandNames);

        ShowCost = RolePermissions.IsAllowed(_session.Role, Permission.SeeCostsAndMargins);
        ProductType = string.Empty;
        Category = string.Empty;
        Brand = string.Empty;
        ModelLabel = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        SupplierBarcode = string.Empty;
        CostText = string.Empty;
        SellingText = string.Empty;
        ColourSearch = string.Empty;
        Images.Clear();
        MainImage = null;
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(CanAddImage));

        Sizes.Clear();
        Colours.Clear();
        foreach (var colour in _catalog.Colours)
        {
            var item = new ChoiceItemViewModel(colour.Key, _localizer[$"Colour_{colour.Key}"], colour.Hex, colour.IsQuickPick);
            item.PropertyChanged += OnChoiceChanged;
            Colours.Add(item);
        }

        RefreshColourLists();
        RebuildVariants();
        UpdateDuplicate();
        UpdateSummary();
        _loading = false;
        Validate();
    }

    partial void OnProductTypeChanged(string value)
    {
        var info = FindType(value);
        var key = info?.Name ?? string.Empty;
        if (key != _sizesFor)
        {
            _sizesFor = key;
            Sizes.Clear();
            foreach (var size in info?.Sizes ?? Array.Empty<string>())
            {
                var item = new ChoiceItemViewModel(size, size);
                item.PropertyChanged += OnChoiceChanged;
                Sizes.Add(item);
            }

            // A different product type has its own size set, so the SKUs built from the old type no longer apply.
            _rows.Clear();
            _removed.Clear();
            RebuildVariants();
        }

        if (!_loading)
        {
            RegenerateSkus();
        }

        OnFormChanged(updateName: true);
    }

    partial void OnCategoryChanged(string value) => OnFormChanged();

    partial void OnBrandChanged(string value) => OnFormChanged(updateName: true);

    partial void OnModelLabelChanged(string value) => OnFormChanged(updateName: true);

    partial void OnDescriptionChanged(string value) => OnFormChanged();

    partial void OnSupplierBarcodeChanged(string value) => OnFormChanged();

    partial void OnCostTextChanged(string value) => OnFormChanged();

    partial void OnSellingTextChanged(string value) => OnFormChanged();

    partial void OnIsMarginNegativeChanged(bool value) => OnPropertyChanged(nameof(IsMarginPositive));

    partial void OnColourSearchChanged(string value) => RefreshColourLists();

    partial void OnDuplicateNameChanged(string value)
    {
        OnPropertyChanged(nameof(HasDuplicate));
        OnPropertyChanged(nameof(ShowValidation));
    }

    partial void OnValidationTextChanged(string value) => OnPropertyChanged(nameof(ShowValidation));

    partial void OnGeneralDisclaimerChanged(string value) => OnPropertyChanged(nameof(ShowGeneralDisclaimer));

    partial void OnPricingDisclaimerChanged(string value) => OnPropertyChanged(nameof(ShowPricingDisclaimer));

    partial void OnVariantsDisclaimerChanged(string value) => OnPropertyChanged(nameof(ShowVariantsDisclaimer));

    partial void OnMainImageChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(HasMainImage));
        OnPropertyChanged(nameof(HasNoMainImage));
        OnPropertyChanged(nameof(ImagePosition));
        foreach (var image in Images) image.IsSelected = ReferenceEquals(image.Preview, value);
    }

    [RelayCommand]
    private void ResetName()
    {
        SetName(GeneratedName());
        OnFormChanged();
    }

    /// <summary>Adds a size the product type does not list (the "Add size" button) and ticks it.</summary>
    public void AddCustomSize(string? text)
    {
        var size = ProductNaming.Clean(text).ToUpperInvariant();
        if (size.Length == 0)
        {
            return;
        }

        var existing = Sizes.FirstOrDefault(s => ProductNaming.Key(s.Value) == ProductNaming.Key(size));
        if (existing is null)
        {
            existing = new ChoiceItemViewModel(size, size);
            existing.PropertyChanged += OnChoiceChanged;
            Sizes.Add(existing);
        }

        existing.IsSelected = true;
    }

    [RelayCommand]
    private void RemoveVariant(VariantRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        _removed.Add(row.Key);
        _rows.Remove(row.Key);
        Variants.Remove(row);
        OnVariantsChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddImage))]
    private async Task AddImageAsync()
    {
        await AddImagesAsync(await _imagePicker.PickManyAsync());
    }

    public Task AddDroppedImagesAsync(IEnumerable<string> paths) => AddImagesAsync(paths
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => new PickedLogo(path, System.IO.Path.GetFileName(path), 0)));

    private async Task AddImagesAsync(IEnumerable<PickedLogo> picked)
    {
        var startingCount = Images.Count;
        foreach (var file in picked.Take(MaxImages - Images.Count))
        {
            if (Images.Any(image => string.Equals(image.Path, file.Path, StringComparison.OrdinalIgnoreCase))) continue;
            var preview = await _imagePicker.LoadPreviewAsync(file.Path);
            if (preview is null) continue;
            Images.Add(new ProductImageViewModel(file.Path, preview, SelectProductImage, RemoveProductImage));
            MainImage ??= preview;
        }

        if (Images.Count == startingCount) return;
        OnPropertyChanged(nameof(ImagePosition));
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(CanAddImage));
        AddImageCommand.NotifyCanExecuteChanged();
        Touch();
    }

    [RelayCommand]
    private void SelectImage(ProductImageViewModel? image)
    {
        if (image is not null)
        {
            MainImage = image.Preview;
            OnPropertyChanged(nameof(ImagePosition));
        }
    }

    private void SelectProductImage(ProductImageViewModel image) => SelectImage(image);

    public void StepImage(int delta)
    {
        if (Images.Count == 0) return;
        var index = Images.ToList().FindIndex(i => ReferenceEquals(i.Preview, MainImage));
        index = (index + delta + Images.Count) % Images.Count;
        MainImage = Images[index].Preview;
        OnPropertyChanged(nameof(ImagePosition));
    }

    [RelayCommand]
    private void RemoveSelectedImage()
    {
        var image = Images.FirstOrDefault(i => ReferenceEquals(i.Preview, MainImage));
        RemoveImage(image);
    }

    public void RemoveProductImage(ProductImageViewModel? image) => RemoveImage(image);

    [RelayCommand]
    private void RemoveMainImage()
    {
        var main = Images.FirstOrDefault(i => ReferenceEquals(i.Preview, MainImage));
        if (main is null)
        {
            return;
        }

        Images.Remove(main);
        MainImage = Images.FirstOrDefault()?.Preview;
        OnPropertyChanged(nameof(ImagePosition));
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(CanAddImage));
        AddImageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RemoveImage(ProductImageViewModel? image)
    {
        if (image is null || !Images.Remove(image)) return;
        if (ReferenceEquals(image.Preview, MainImage)) MainImage = Images.FirstOrDefault()?.Preview;
        OnPropertyChanged(nameof(ImagePosition));
        OnPropertyChanged(nameof(HasImages));
        OnPropertyChanged(nameof(CanAddImage));
        AddImageCommand.NotifyCanExecuteChanged();
    }

    public void SetVariantPrice(VariantRowViewModel row, string text)
    {
        if (row.PriceText == text) return;
        row.PriceText = text;
        Touch();
        Validate();
    }

    /// <summary>Saves the product and its variants in one step, then raises <see cref="Saved"/>.</summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!CanSave || IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var cost = ShowCost ? ParsePrice(CostText, out _) : null;
            var product = new NewProduct(
                ProductNaming.Clean(ProductType),
                ProductNaming.Clean(Category),
                ProductNaming.Clean(Brand),
                ProductNaming.Clean(ModelLabel),
                ProductNaming.Clean(Name),
                Description.Trim(),
                SupplierBarcode.Trim(),
                cost,
                ParsePrice(SellingText, out _) ?? 0m,
                Images.Select(i => i.Path).ToList(),
                Variants
                    .Select(v => new NewVariant(v.Size, v.Colour, v.Sku.Trim(), v.InternalBarcode, v.OpeningStockUnits,
                        string.IsNullOrWhiteSpace(v.PriceText) ? null : decimal.Parse(v.PriceText.Trim(), NumberStyles.Number, _localizer.Culture)))
                    .ToList());
            await _catalog.SaveAsync(product);
            Saved?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void OnChoiceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_loading || e.PropertyName != nameof(ChoiceItemViewModel.IsSelected) || sender is not ChoiceItemViewModel item)
        {
            return;
        }

        if (item.IsSelected)
        {
            // Ticking a size or colour again brings back variants the owner had deleted for it.
            _removed.RemoveWhere(key => key.Split('|').Contains(item.Value));
        }

        if (item.Hex is not null)
        {
            RefreshColourLists();
        }

        RebuildVariants();
        OnFormChanged();
    }

    private void OnFormChanged(bool updateName = false)
    {
        if (_loading)
        {
            return;
        }

        Touch();
        if (updateName)
        {
            SetName(GeneratedName());

            UpdateDuplicate();
        }

        UpdateSummary();
        Validate();
    }

    private void Touch() => _touched = true;

    private string GeneratedName() => ProductNaming.GenerateName(Brand, ProductType, ModelLabel);

    private void SetName(string name)
    {
        Name = name;
    }

    private ProductTypeInfo? FindType(string? name)
    {
        var key = ProductNaming.Key(name);
        return key.Length == 0 ? null : _catalog.ProductTypes.FirstOrDefault(t => ProductNaming.Key(t.Name) == key);
    }

    private void UpdateDuplicate()
    {
        var found = ProductDuplicateCheck.Find(_catalog.ExistingProducts, new ProductIdentity(Brand, ProductType, ModelLabel));
        DuplicateName = found?.Name ?? string.Empty;
        DuplicateMessage = found is null ? string.Empty : _localizer.Format("Add_Duplicate", found.Name);
    }

    private void RefreshColourLists()
    {
        VisibleColours.Clear();
        foreach (var colour in Colours.Where(c => c.IsQuickPick || c.IsSelected))
        {
            VisibleColours.Add(colour);
        }

        var search = ProductNaming.Key(ColourSearch);
        FilteredColours.Clear();
        foreach (var colour in Colours.Where(c => search.Length == 0 || ProductNaming.Key(c.Label).Contains(search, StringComparison.Ordinal)))
        {
            FilteredColours.Add(colour);
        }
    }

    private void RebuildVariants()
    {
        var sizes = Sizes.Where(s => s.IsSelected).ToList();
        var colours = Colours.Where(c => c.IsSelected).ToList();
        var slots = VariantPlanner.Plan(sizes.Select(s => s.Value), colours.Select(c => c.Value));

        var rows = new List<VariantRowViewModel>(slots.Count);
        var taken = new HashSet<string>(_catalog.TakenSkus, StringComparer.OrdinalIgnoreCase);
        foreach (var slot in slots)
        {
            var key = $"{slot.Size}|{slot.Colour}";
            if (_removed.Contains(key))
            {
                continue;
            }

            if (!_rows.TryGetValue(key, out var row))
            {
                var colour = colours.FirstOrDefault(c => c.Value == slot.Colour);
                row = new VariantRowViewModel(
                    slot.Size,
                    slot.Colour,
                    VariantLabel(slot.Size, colour?.Label),
                    colour?.Hex,
                    SkuGenerator.Generate(ProductType, slot.Colour, slot.Size, taken),
                    InternalBarcode.FromSequence(_nextBarcode++));
                row.PropertyChanged += OnRowChanged;
                _rows[key] = row;
            }

            taken.Add(row.Sku);
            rows.Add(row);
        }

        // A row keeps its place in the cache only while its size and colour are still ticked.
        foreach (var stale in _rows.Where(r => !rows.Contains(r.Value) && !_removed.Contains(r.Key)).Select(r => r.Key).ToList())
        {
            _rows.Remove(stale);
        }

        Variants.Clear();
        foreach (var row in rows)
        {
            Variants.Add(row);
        }

        OnVariantsChanged();
    }

    private string VariantLabel(string? size, string? colourLabel) => (size, colourLabel) switch
    {
        (null, null) => _localizer["Add_PlainVariant"],
        (null, _) => colourLabel!,
        (_, null) => size,
        _ => $"{colourLabel} – {size}",
    };

    private void OnRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (e.PropertyName == nameof(VariantRowViewModel.Sku) && sender is VariantRowViewModel row)
        {
            row.IsSkuOverridden = true;
        }

        if (e.PropertyName is nameof(VariantRowViewModel.Sku) or nameof(VariantRowViewModel.OpeningStock))
        {
            Touch();
            Validate();
        }
    }

    /// <summary>The SKU starts with the product type, so a changed type rebuilds every SKU the owner did not type over.</summary>
    private void RegenerateSkus()
    {
        var taken = new HashSet<string>(_catalog.TakenSkus, StringComparer.OrdinalIgnoreCase);
        taken.UnionWith(Variants.Where(v => v.IsSkuOverridden).Select(v => v.Sku));
        _loading = true;
        try
        {
            foreach (var row in Variants.Where(v => !v.IsSkuOverridden))
            {
                row.Sku = SkuGenerator.Generate(ProductType, row.Colour, row.Size, taken);
                taken.Add(row.Sku);
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnVariantsChanged()
    {
        OnPropertyChanged(nameof(HasVariants));
        OnPropertyChanged(nameof(HasNoVariants));
        UpdateSummary();
        Validate();
    }

    private void UpdateSummary()
    {
        SummaryCategory = ProductNaming.Clean(Category);
        SummaryBrand = ProductNaming.Clean(Brand);

        var selling = ParsePrice(SellingText, out var sellingBad);
        SummarySelling = selling is { } s && !sellingBad ? Money(s) : Dash;

        var cost = ParsePrice(CostText, out var costBad);
        if (ShowCost && cost is { } c && selling is { } price && !costBad && !sellingBad)
        {
            var margin = ProductPricing.Estimate(c, price);
            SummaryMargin = margin.Percent is { } p
                ? $"{margin.Amount.ToString("N0", _localizer.Culture)} ({p.ToString("N0", _localizer.Culture)}%)"
                : Dash;
            IsMarginNegative = margin.Amount < 0;
        }
        else
        {
            SummaryMargin = Dash;
            IsMarginNegative = false;
        }

        var sizeCount = Sizes.Count(x => x.IsSelected);
        var colourCount = Colours.Count(x => x.IsSelected);
        var total = Variants.Count;
        SummaryVariants = (sizeCount, colourCount) switch
        {
            (0, 0) => total.ToString(CultureInfo.CurrentCulture),
            (_, 0) => $"{total} ({Plural(sizeCount, "Add_SizeOne", "Add_SizeMany")})",
            (0, _) => $"{total} ({Plural(colourCount, "Add_ColourOne", "Add_ColourMany")})",
            _ => $"{total} ({Plural(sizeCount, "Add_SizeOne", "Add_SizeMany")} × {Plural(colourCount, "Add_ColourOne", "Add_ColourMany")})",
        };
    }

    private const string Dash = "—";

    private string Plural(int count, string oneKey, string manyKey) => $"{count} {_localizer[count == 1 ? oneKey : manyKey]}";

    private string Money(decimal amount) => $"{amount.ToString("N2", _localizer.Culture)} {_catalog.DefaultCurrencyCode}";

    private decimal? ParsePrice(string text, out bool invalid)
    {
        invalid = false;
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (decimal.TryParse(text.Trim(), NumberStyles.Number, _localizer.Culture, out var value))
        {
            return value;
        }

        invalid = true;
        return null;
    }

    private bool IsValidPrice(string text) => decimal.TryParse(text.Trim(), NumberStyles.Number, _localizer.Culture, out var value) && value >= 0;

    private void Validate()
    {
        var selling = ParsePrice(SellingText, out var sellingBad);
        var costBad = false;
        decimal? cost = null;
        if (ShowCost)
        {
            cost = ParsePrice(CostText, out costBad);
        }

        var draft = new ProductDraft(
            ProductType,
            Category,
            Brand,
            ModelLabel,
            Name,
            cost,
            selling,
            Variants.Select(v => new VariantDraft(v.Sku, v.OpeningStockUnits)).ToList());

        var code = sellingBad || costBad ? ProductValidationCode.PriceInvalid : ProductRules.Validate(draft);
        var variantPriceBad = Variants.Any(v => v.PriceText.Length > 0 && !IsValidPrice(v.PriceText));
        string message;
        var section = "";
        if (code != ProductValidationCode.None)
        {
            message = _localizer[$"Error_{code}"];
            section = code switch
            {
                ProductValidationCode.ProductTypeRequired or ProductValidationCode.CategoryRequired or
                    ProductValidationCode.BrandRequired or ProductValidationCode.ModelLabelRequired or
                    ProductValidationCode.NameRequired => "general",
                ProductValidationCode.SellingPriceRequired or ProductValidationCode.PriceInvalid => "pricing",
                ProductValidationCode.SkuRequired or ProductValidationCode.SkuDuplicate or
                    ProductValidationCode.StockInvalid => "variants",
                _ => "",
            };
        }
        else if (HasDuplicate)
        {
            message = DuplicateMessage;
        }
        else if (Variants.Count == 0)
        {
            message = _localizer["Error_NoVariants"];
            section = "variants";
        }
        else if (variantPriceBad)
        {
            message = _localizer["Error_PriceInvalid"];
            section = "variants";
        }
        else if (Variants.Any(v => _catalog.TakenSkus.Contains(v.Sku.Trim())))
        {
            message = _localizer[$"Error_{ProductValidationCode.SkuDuplicate}"];
            section = "variants";
        }
        else
        {
            message = string.Empty;
        }

        CanSave = message.Length == 0 && !IsSaving;

        // Keep feedback beside its section heading so field errors never change the form's height.
        GeneralDisclaimer = _touched && section == "general" ? $"— {message}" : string.Empty;
        PricingDisclaimer = _touched && section == "pricing" ? $"— {message}" : string.Empty;
        VariantsDisclaimer = _touched && section == "variants" ? $"— {message}" : string.Empty;
        ValidationText = _touched ? message : string.Empty;
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static void Fill(ObservableCollection<string> target, IEnumerable<string> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private static void FillSuggestions(ObservableCollection<MasterDataSuggestion> target, IEnumerable<string> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(new MasterDataSuggestion(value, false));
    }
}

/// <summary>A selectable master-data value, or an inline create action for an unmatched value.</summary>
public sealed record MasterDataSuggestion(string Value, bool IsCreate)
{
    public string DisplayText => IsCreate ? $"+ {Value}" : Value;
}
