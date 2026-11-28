using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.App.ViewModels.AddProduct;
using Estoq.App.ViewModels.PointOfSale;
using Estoq.Core.Returns;
using Estoq.Core.Sales;

namespace Estoq.App.ViewModels.Returns;

/// <summary>
/// The Returns pop-up (F4, docs/features/returns-and-voids.md): find the original sale by receipt number (or return without
/// one), tick the items and quantities, pick Return or Exchange mode (Exchange also searches the new items), and save. The rules live in <see cref="ReturnRules"/>.
/// </summary>
public sealed partial class ReturnsViewModel : ObservableObject
{
    private const int MaxSuggestions = 6;

    private readonly IReturnsService _service;
    private readonly IPointOfSaleService _pointOfSale;
    private readonly ILocalizer _localizer;
    private readonly SaleCart _newItems = new();
    private SaleSettings _settings;
    private SettledSale? _sale;
    private bool _isResetting;

    public ReturnsViewModel(IReturnsService service, IPointOfSaleService pointOfSale, ILocalizer localizer)
    {
        _service = service;
        _pointOfSale = pointOfSale;
        _localizer = localizer;
        _settings = pointOfSale.GetSettings();
        SelectedCurrency = _settings.Rates.BaseCurrency;
        RebuildCurrencies();
        _localizer.LanguageChanged += (_, _) => RefreshText();
        Recalculate();
    }

    /// <summary>Raised when the confirmation is dismissed; the pop-up closes.</summary>
    public event EventHandler? CloseRequested;

    public ObservableCollection<ReturnLineViewModel> Lines { get; } = [];

    public ObservableCollection<ExchangeLineViewModel> ExchangeLines { get; } = [];

    /// <summary>The currency chips in the pop-up header: the currency the refund or balance is handed over in (gold = selected).</summary>
    public ObservableCollection<CurrencyChipViewModel> Currencies { get; } = [];

    /// <summary>Matches shown under the exchange search box.</summary>
    public ObservableCollection<SaleVariant> ExchangeSuggestions { get; } = [];

    public ObservableCollection<AddedItemViewModel> DoneReturned { get; } = [];

    public ObservableCollection<AddedItemViewModel> DoneTaken { get; } = [];

    [ObservableProperty]
    public partial string ReceiptText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReturnScanText { get; set; } = string.Empty;

    /// <summary>Why a scanned or searched item could not be added (shown under the search box in use).</summary>
    [ObservableProperty]
    public partial string ScanError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LookupError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasSale { get; set; }

    /// <summary>True while returning without a receipt: items come from the catalog at today's price.</summary>
    [ObservableProperty]
    public partial bool IsNoReceipt { get; set; }

    [ObservableProperty]
    public partial string SaleNumberText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SaleDateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExchange { get; set; }

    /// <summary>0 means no reason; 1 to 5 follow <see cref="ReturnReason"/>.</summary>
    [ObservableProperty]
    public partial int ReasonIndex { get; set; }

    [ObservableProperty]
    public partial string Note { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SelectedCurrency { get; set; }

    /// <summary>Title of the first items table: the items to return, or the receipt items to exchange.</summary>
    [ObservableProperty]
    public partial string ItemsTitle { get; set; } = string.Empty;

    /// <summary>The summary's first two figures (items and subtotal, or credit and new items); the third is the amount.</summary>
    [ObservableProperty]
    public partial string Kpi1Label { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Kpi1Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Kpi2Label { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Kpi2Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    /// <summary>The amount in the chosen currency; empty while that is the base currency.</summary>
    [ObservableProperty]
    public partial string SettleText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CappedNote { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanSave { get; set; }

    [ObservableProperty]
    public partial string PrimaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial string DoneNumberText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneAmountLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneAmountText { get; set; } = string.Empty;

    /// <summary>True once a sale is found or "No receipt" is chosen: the items card shows rows instead of the prompt.</summary>
    public bool HasItemsSource => HasSale || IsNoReceipt;

    public bool IsRefund => !IsExchange;

    public bool HasLookupError => LookupError.Length > 0;

    public bool HasScanError => ScanError.Length > 0;

    public bool HasCappedNote => CappedNote.Length > 0;

    public bool ShowSettle => SettleText.Length > 0;

    public bool HasLines => Lines.Count > 0;

    public bool HasExchangeLines => ExchangeLines.Count > 0;

    public bool ShowDoneTaken => DoneTaken.Count > 0;

    /// <summary>Starts a fresh return: nothing found, nothing ticked, Return mode selected.</summary>
    public void Reset()
    {
        _isResetting = true;
        _settings = _pointOfSale.GetSettings();
        SelectedCurrency = _settings.Rates.BaseCurrency;
        RebuildCurrencies();

        _sale = null;
        _newItems.Clear();
        Lines.Clear();
        ExchangeLines.Clear();
        DoneReturned.Clear();
        DoneTaken.Clear();
        ReceiptText = string.Empty;
        ReturnScanText = string.Empty;
        ExchangeSuggestions.Clear();
        ScanError = string.Empty;
        LookupError = string.Empty;
        HasSale = false;
        IsNoReceipt = false;
        SaleNumberText = string.Empty;
        SaleDateText = string.Empty;
        IsExchange = false;
        ReasonIndex = 0;
        Note = string.Empty;
        IsCompleted = false;
        _isResetting = false;
        Recalculate();
    }

    [RelayCommand]
    private void FindSale()
    {
        LookupError = string.Empty;
        var text = ReceiptText.Trim();
        if (text.Length == 0)
        {
            return;
        }

        var sale = _service.FindSale(text);
        if (sale is null)
        {
            LookupError = _localizer["Ret_ErrNotFound"];
            return;
        }

        if (!ReturnRules.IsWithinWindow(sale.Date, DateTime.Now, _service.ReturnWindowDays))
        {
            LookupError = _localizer.Format("Ret_ErrWindow", _service.ReturnWindowDays ?? 0);
            return;
        }

        _sale = sale;
        HasSale = true;
        SaleNumberText = $"#{sale.Number}";
        SaleDateText = sale.Date.ToString("g", _localizer.Culture);
        Lines.Clear();
        var ticked = false;
        foreach (var line in sale.Lines)
        {
            // Lines already returned stay in the table, disabled and marked, so the same item cannot be returned twice.
            var row = new ReturnLineViewModel(Lines.Count + 1, line.VariantId, line.Name, line.Detail, line.Sku, line.UnitPrice,
                line.Returnable, !ticked && line.Returnable > 0, FormatBase, Recalculate, line.QuantityReturned, line.QuantitySold);
            ticked |= row.IsSelected;
            Lines.Add(row);
        }

        RefreshLineStatus();

        Recalculate();
    }

    [RelayCommand]
    private void StartNoReceipt()
    {
        LookupError = string.Empty;
        ScanError = string.Empty;
        _sale = null;
        HasSale = false;
        IsNoReceipt = true;
        Lines.Clear();
        Recalculate();
    }

    /// <summary>Back to the receipt lookup, dropping the found sale or the no-receipt items.</summary>
    [RelayCommand]
    private void ChangeSale()
    {
        _sale = null;
        HasSale = false;
        IsNoReceipt = false;
        LookupError = string.Empty;
        ScanError = string.Empty;
        ReturnScanText = string.Empty;
        Lines.Clear();
        Recalculate();
    }

    [RelayCommand]
    private void SelectRefund() => IsExchange = false;

    [RelayCommand]
    private void SelectExchange() => IsExchange = true;

    /// <summary>Enter in the scan box (return without a receipt): adds the item at today's price.</summary>
    [RelayCommand]
    private void ScanReturnItem()
    {
        var variant = Find(ReturnScanText);
        if (variant is null)
        {
            ScanError = ReturnScanText.Trim().Length == 0 ? string.Empty : _localizer.Format("Ret_ErrNoItem", ReturnScanText.Trim());
            return;
        }

        ScanError = string.Empty;
        ReturnScanText = string.Empty;
        var existing = Lines.FirstOrDefault(line => line.VariantId == variant.VariantId);
        if (existing is not null)
        {
            existing.IncreaseCommand.Execute(null);
            return;
        }

        var row = new ReturnLineViewModel(Lines.Count + 1, variant.VariantId, variant.Name, variant.Sku, variant.Sku, variant.UnitPrice,
            SaleCart.MaxQuantity, true, FormatBase, Recalculate);
        row.RemoveRequested += (_, _) =>
        {
            Lines.Remove(row);
            Recalculate();
        };
        Lines.Add(row);
        Recalculate();
    }

    /// <summary>Fills the suggestions for the text typed in the exchange search box.</summary>
    public void UpdateExchangeSuggestions(string text)
    {
        ExchangeSuggestions.Clear();
        var query = text.Trim();
        if (query.Length == 0)
        {
            return;
        }

        foreach (var match in _pointOfSale.Search(query, MaxSuggestions))
        {
            ExchangeSuggestions.Add(match);
        }
    }

    /// <summary>Adds an exchange item the customer takes; returns false when nothing matched.</summary>
    public bool AddExchangeItem(SaleVariant variant)
    {
        ScanError = string.Empty;
        _newItems.Add(variant.VariantId, variant.Name, variant.Sku, variant.UnitPrice);
        ExchangeSuggestions.Clear();
        Recalculate();
        return true;
    }

    /// <summary>Enter in the exchange search box without choosing a suggestion: a scanned code or the best match.</summary>
    public bool AddExchangeByQuery(string text)
    {
        var variant = Find(text);
        if (variant is null)
        {
            ScanError = text.Trim().Length == 0 ? string.Empty : _localizer.Format("Ret_ErrNoItem", text.Trim());
            return false;
        }

        return AddExchangeItem(variant);
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave)
        {
            return;
        }

        var returned = SelectedLines();
        var amounts = ReturnRules.Calculate(returned, IsNoReceipt ? null : _sale?.Basis);
        var newTotal = NewItemsTotal();
        var balance = IsExchange ? ReturnRules.ExchangeBalance(amounts.Refund, newTotal) : 0m;
        var currency = SelectedCurrency ?? _settings.Rates.BaseCurrency;
        var settleBase = IsExchange ? Math.Abs(balance) : amounts.Refund;
        var rate = _settings.Rates.RateOf(currency);
        var request = new ReturnRequest(
            IsNoReceipt ? null : _sale?.Number,
            IsExchange ? ReturnType.Exchange : ReturnType.Refund,
            ReasonIndex > 0 ? (ReturnReason)(ReasonIndex - 1) : null,
            Note.Trim(),
            returned,
            IsExchange ? _newItems.Lines : [],
            amounts.Refund,
            balance,
            currency,
            _settings.Rates.FromBase(settleBase, currency),
            rate);
        var receipt = _service.Save(request);

        DoneReturned.Clear();
        foreach (var line in returned)
        {
            DoneReturned.Add(DoneRow(line.Name, line.Sku, line.UnitPrice, line.Quantity, DoneReturned.Count));
        }

        DoneTaken.Clear();
        foreach (var line in request.NewItems)
        {
            DoneTaken.Add(DoneRow(line.Name, line.Sku, line.UnitPrice, line.Quantity, DoneTaken.Count));
        }

        DoneNumberText = _localizer.Format("Ret_DoneNumber", receipt.ReturnNumber);
        DoneSubtitle = _localizer[IsExchange ? "Ret_DoneSubtitleExchange" : "Ret_DoneSubtitleRefund"];
        (DoneAmountLabel, DoneAmountText) = AmountParts(IsExchange, balance, amounts.Refund, currency);
        OnPropertyChanged(nameof(ShowDoneTaken));
        IsCompleted = true;
    }

    /// <summary>Closes the confirmation and the pop-up.</summary>
    [RelayCommand]
    private void Done() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ProcessAnother() => Reset();

    partial void OnHasSaleChanged(bool value) => OnPropertyChanged(nameof(HasItemsSource));

    partial void OnIsNoReceiptChanged(bool value) => OnPropertyChanged(nameof(HasItemsSource));

    partial void OnLookupErrorChanged(string value) => OnPropertyChanged(nameof(HasLookupError));

    partial void OnScanErrorChanged(string value) => OnPropertyChanged(nameof(HasScanError));

    partial void OnCappedNoteChanged(string value) => OnPropertyChanged(nameof(HasCappedNote));

    partial void OnSettleTextChanged(string value) => OnPropertyChanged(nameof(ShowSettle));

    partial void OnIsExchangeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRefund));
        Recalculate();
    }

    partial void OnSelectedCurrencyChanged(string? value)
    {
        foreach (var chip in Currencies)
        {
            chip.IsSelected = string.Equals(chip.Code, value, StringComparison.OrdinalIgnoreCase);
        }

        if (!_isResetting)
        {
            Recalculate();
        }
    }

    /// <summary>The "Returned" pill of lines that earlier returns took back, fully or in part.</summary>
    private void RefreshLineStatus()
    {
        foreach (var line in Lines)
        {
            line.SetStatus(line.ReturnedQuantity == 0
                ? string.Empty
                : line.CanReturn ? _localizer.Format("Ret_ReturnedSome", line.ReturnedQuantity) : _localizer["Ret_Returned"]);
        }
    }

    private void RebuildCurrencies()
    {
        Currencies.Clear();
        foreach (var code in _settings.Currencies)
        {
            Currencies.Add(new CurrencyChipViewModel(code, CurrencyFormat.ChipLabel(code),
                string.Equals(code, SelectedCurrency, StringComparison.OrdinalIgnoreCase), chosen => SelectedCurrency = chosen));
        }
    }

    private SaleVariant? Find(string text)
    {
        var query = text.Trim();
        return query.Length == 0 ? null : _pointOfSale.FindByCode(query) ?? _pointOfSale.Search(query, 1).FirstOrDefault();
    }

    private List<ReturnedLine> SelectedLines() => Lines
        .Where(line => line.IsSelected && line.Quantity > 0)
        .Select(line => new ReturnedLine(line.VariantId, line.Name, line.Sku, line.UnitPrice, line.Quantity))
        .ToList();

    /// <summary>The new items with the store's current tax; an exchange has no discount.</summary>
    private decimal NewItemsTotal() =>
        SaleTotals.Calculate(_newItems.Subtotal, 0m, _settings.TaxRate, _settings.TaxMode).Total;

    private void SetNewItem(string variantId, int quantity)
    {
        _newItems.SetQuantity(variantId, quantity);
        Recalculate();
    }

    /// <summary>Redraws every number and the primary button after any change to the ticks, quantities, type or currency.</summary>
    private void Recalculate()
    {
        var returned = SelectedLines();
        var amounts = ReturnRules.Calculate(returned, IsNoReceipt ? null : _sale?.Basis);
        var count = returned.Sum(line => line.Quantity);
        var currency = SelectedCurrency ?? _settings.Rates.BaseCurrency;
        var newTotal = NewItemsTotal();
        var balance = ReturnRules.ExchangeBalance(amounts.Refund, newTotal);

        ItemsTitle = _localizer[IsExchange ? "Ret_ItemsTitleExchange" : "Ret_ItemsTitle"];
        if (IsExchange)
        {
            Kpi1Label = _localizer["Ret_KpiCredit"];
            Kpi1Text = FormatBase(amounts.Refund);
            Kpi2Label = _localizer["Ret_KpiNewItems"];
            Kpi2Text = FormatBase(newTotal);
        }
        else
        {
            Kpi1Label = _localizer["Ret_KpiItems"];
            Kpi1Text = count.ToString(CultureInfo.InvariantCulture);
            Kpi2Label = _localizer["Ret_KpiSubtotal"];
            Kpi2Text = FormatBase(amounts.Value);
        }
        CappedNote = amounts.IsCapped ? _localizer["Ret_Capped"] : string.Empty;
        (AmountLabel, AmountText) = AmountParts(IsExchange, balance, amounts.Refund, null);

        var settleBase = IsExchange ? Math.Abs(balance) : amounts.Refund;
        var isBase = string.Equals(currency, _settings.Rates.BaseCurrency, StringComparison.OrdinalIgnoreCase);
        SettleText = isBase || settleBase == 0m ? string.Empty : FormatIn(settleBase, currency);

        PrimaryText = _localizer[IsExchange ? "Ret_PrimaryExchange" : "Ret_PrimaryRefund"];
        CanSave = !IsCompleted && count > 0 && (!IsExchange || !_newItems.IsEmpty);

        RefreshExchangeLines();
        OnPropertyChanged(nameof(HasLines));
    }

    private void RefreshExchangeLines()
    {
        ExchangeLines.Clear();
        foreach (var line in _newItems.Lines)
        {
            ExchangeLines.Add(new ExchangeLineViewModel(ExchangeLines.Count + 1, line.VariantId, line.Name, line.Sku, line.Quantity, FormatBase(line.LineTotal), SetNewItem));
        }

        OnPropertyChanged(nameof(HasExchangeLines));
    }

    /// <summary>The label and amount of the summary's highlighted row, in the base currency or the settle currency.</summary>
    private (string Label, string Text) AmountParts(bool exchange, decimal balance, decimal refund, string? currency)
    {
        string Format(decimal baseAmount) => currency is null || string.Equals(currency, _settings.Rates.BaseCurrency, StringComparison.OrdinalIgnoreCase)
            ? FormatBase(baseAmount)
            : FormatIn(baseAmount, currency);

        if (!exchange)
        {
            return (_localizer["Ret_RefundAmount"], Format(refund));
        }

        var label = balance > 0m ? "Ret_BalancePays" : balance < 0m ? "Ret_BalanceReceives" : "Ret_BalanceEven";
        return (_localizer[label], Format(Math.Abs(balance)));
    }

    private AddedItemViewModel DoneRow(string name, string sku, decimal price, int quantity, int index) => new(
        name, string.Empty, sku, FormatBase(price), quantity.ToString(CultureInfo.InvariantCulture), FormatBase(Money.Round(price * quantity)), index % 2 == 1);

    private void RefreshText()
    {
        foreach (var line in Lines)
        {
            line.Refresh();
        }

        RefreshLineStatus();

        if (!IsCompleted)
        {
            Recalculate();
        }
    }

    private string FormatBase(decimal amount) => CurrencyFormat.Amount(amount, _settings.Rates.BaseCurrency, _localizer.Culture);

    private string FormatIn(decimal baseAmount, string currency) =>
        CurrencyFormat.Amount(_settings.Rates.FromBase(baseAmount, currency), currency, _localizer.Culture);
}
