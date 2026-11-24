using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Sales;
using Estoq.Core.Setup;

namespace Estoq.App.ViewModels.PointOfSale;

/// <summary>
/// State of the F1 point of sale pop-up (docs/features/point-of-sale.md). A singleton, so the cart survives a
/// language change or an accidental close. Money rules live in <c>Estoq.Core.Sales</c>; this class only wires them to the screen.
/// </summary>
public sealed partial class PointOfSaleViewModel : ObservableObject
{
    private const int MaxSuggestions = 6;

    private readonly IPointOfSaleService _service;
    private readonly ILocalizer _localizer;
    private readonly SaleCart _cart = new();
    private readonly Dictionary<string, int> _onHand = [];
    private readonly List<PaymentLine> _payments = [];

    private SaleSettings _settings;
    private bool _wasPointOfSaleOpen;
    private decimal _discountPercent;
    private PaymentMethod _method = PaymentMethod.Cash;
    private string _currency;

    public PointOfSaleViewModel(IPointOfSaleService service, ILocalizer localizer, IPopupService popups)
    {
        _service = service;
        _localizer = localizer;
        _settings = service.GetSettings();
        _currency = _settings.Rates.BaseCurrency;
        _wasPointOfSaleOpen = popups.Current == PopupKind.PointOfSale;
        BuildChoices();
        Recalculate();

        _localizer.LanguageChanged += (_, _) =>
        {
            BuildChoices();
            Recalculate();
        };
        popups.Changed += OnPopupChanged;
    }

    public ObservableCollection<CartLineViewModel> Lines { get; } = [];

    public ObservableCollection<SaleVariant> Suggestions { get; } = [];

    public ObservableCollection<CurrencyChipViewModel> Currencies { get; } = [];

    public ObservableCollection<PaymentMethodViewModel> Methods { get; } = [];

    public ObservableCollection<PaymentLineViewModel> PaymentLines { get; } = [];

    public ObservableCollection<CartLineViewModel> CompletedSaleLines { get; } = [];

    public ObservableCollection<PaymentLineViewModel> CompletedPaymentLines { get; } = [];

    public ObservableCollection<DiscountPresetViewModel> DiscountPresets { get; } = [];

    [ObservableProperty]
    public partial double DiscountFlyoutWidth { get; set; } = 224;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScanMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AmountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiscountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ApprovalCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiscountError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool NeedsApproval { get; set; }

    [ObservableProperty]
    public partial bool IsCartEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool HasDiscount { get; set; }

    [ObservableProperty]
    public partial bool HasPaymentLines { get; set; }

    [ObservableProperty]
    public partial bool CanAddPayment { get; set; }

    [ObservableProperty]
    public partial bool NeedsCashForChange { get; set; }

    [ObservableProperty]
    public partial bool HasAmountToReturn { get; set; }

    [ObservableProperty]
    public partial bool HasRemainingToPay { get; set; }

    [ObservableProperty]
    public partial bool CanComplete { get; set; }

    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial bool ShowShortcutHints { get; set; }

    [ObservableProperty]
    public partial string ItemsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PriceHeader { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalHeader { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SubtotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiscountTotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TaxLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TaxInfo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TaxText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BalanceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RemainingToPayText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiscountHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneNumberText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneTotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DonePaidText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneTaxLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneTaxText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneChangeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool DoneHasChange { get; set; }

    public bool HasSuggestions => Suggestions.Count > 0;

    public bool HasScanMessage => ScanMessage.Length > 0;

    public bool HasDiscountError => DiscountError.Length > 0;

    public string CurrentCurrencySymbol => CurrencyFormat.Symbol(_currency);

    public string AmountDecimalSeparator => _localizer.Culture.NumberFormat.NumberDecimalSeparator;

    public bool IsSaleOpen => !IsCompleted;

    private void OnPopupChanged(object? sender, EventArgs e)
    {
        var isPointOfSaleOpen = sender is IPopupService popups && popups.Current == PopupKind.PointOfSale;
        if (isPointOfSaleOpen && !IsCompleted)
        {
            Reload();
        }
        else if (!isPointOfSaleOpen && _wasPointOfSaleOpen)
        {
            ResetSale();
            IsCompleted = false;
            Recalculate();
        }

        _wasPointOfSaleOpen = isPointOfSaleOpen;
    }

    partial void OnSearchTextChanged(string value)
    {
        ScanMessage = string.Empty;
        Suggestions.Clear();
        if (value.Trim().Length >= 2)
        {
            foreach (var match in _service.Search(value, MaxSuggestions))
            {
                Suggestions.Add(match);
            }
        }

        OnPropertyChanged(nameof(HasSuggestions));
    }

    partial void OnScanMessageChanged(string value) => OnPropertyChanged(nameof(HasScanMessage));

    partial void OnDiscountErrorChanged(string value) => OnPropertyChanged(nameof(HasDiscountError));

    partial void OnIsCompletedChanged(bool value) => OnPropertyChanged(nameof(IsSaleOpen));

    partial void OnAmountTextChanged(string value) => CanAddPayment = TryGetEntry(out var amount) && amount > 0m;

    partial void OnDiscountTextChanged(string value)
    {
        DiscountError = string.Empty;
        if (TryParsePercent(value, out var percent))
        {
            NeedsApproval = DiscountPolicy.Evaluate(_service.CurrentRole, percent, _settings.DiscountCapPercent)
                == DiscountDecision.NeedsApproval;
        }
        else
        {
            NeedsApproval = false;
        }
    }

    /// <summary>Enter in the scan field: an exact barcode or SKU adds the item, otherwise the single or first match does.</summary>
    [RelayCommand]
    private void Scan()
    {
        var text = SearchText.Trim();
        if (text.Length == 0)
        {
            return;
        }

        var variant = _service.FindByCode(text)
            ?? (Suggestions.Count > 0 ? Suggestions[0] : _service.Search(text, 1).FirstOrDefault());
        if (variant is null)
        {
            ScanMessage = _localizer.Format("Pos_NoMatch", text);
            return;
        }

        AddVariant(variant);
    }

    /// <summary>Adds a variant chosen from the suggestion list.</summary>
    public void AddVariant(SaleVariant variant)
    {
        _onHand[variant.VariantId] = variant.OnHand;
        _cart.Add(variant.VariantId, variant.Name, variant.Sku, variant.UnitPrice);
        SearchText = string.Empty;
        Recalculate();
    }

    public void SetQuantity(string variantId, int quantity)
    {
        _cart.SetQuantity(variantId, quantity);
        Recalculate();
    }

    [RelayCommand]
    private void ClearSale()
    {
        ResetSale();
        Recalculate();
    }

    private void SelectCurrency(string code)
    {
        _currency = code;
        foreach (var chip in Currencies)
        {
            chip.IsSelected = chip.Code == code;
        }

        Recalculate();
    }

    public void SelectMethod(PaymentMethod method)
    {
        _method = method;
        AmountText = string.Empty;
        foreach (var tile in Methods)
        {
            tile.IsSelected = tile.Method == method;
        }
    }

    /// <summary>Applies the typed discount when the role allows it, or when a manager approval code is given.</summary>
    public bool TryApplyDiscount()
    {
        if (string.IsNullOrWhiteSpace(DiscountText))
        {
            RemoveDiscount();
            return true;
        }

        if (!TryParsePercent(DiscountText, out var percent))
        {
            DiscountError = _localizer["Pos_DiscountInvalid"];
            return false;
        }

        switch (DiscountPolicy.Evaluate(_service.CurrentRole, percent, _settings.DiscountCapPercent))
        {
            case DiscountDecision.Invalid:
                DiscountError = _localizer["Pos_DiscountInvalid"];
                return false;
            case DiscountDecision.NeedsApproval when !_service.ApproveAsManager(ApprovalCode.Trim()):
                DiscountError = _localizer[ApprovalCode.Trim().Length == 0 ? "Pos_DiscountNeedsCode" : "Pos_DiscountBadCode"];
                return false;
        }

        _discountPercent = percent;
        ApprovalCode = string.Empty;
        Recalculate();
        return true;
    }

    /// <summary>Applies one of the fixed discount presets using the same role and approval rules as manual discounts.</summary>
    public bool TryApplyDiscountPreset(decimal percent)
    {
        DiscountText = percent.ToString("0.##", _localizer.Culture);
        return TryApplyDiscount();
    }

    [RelayCommand]
    private void RemoveDiscount()
    {
        _discountPercent = 0m;
        DiscountText = string.Empty;
        ApprovalCode = string.Empty;
        Recalculate();
    }

    /// <summary>Opens the discount flyout with the current value.</summary>
    public void PrepareDiscountEditor()
    {
        RefreshDiscountPresets();
        DiscountText = string.Empty;
        ApprovalCode = string.Empty;
        DiscountError = string.Empty;
    }

    public bool AddPayment(PaymentMethod method)
    {
        if (!TryGetEntry(out var amount) || amount <= 0m)
        {
            return false;
        }

        _method = method;
        foreach (var tile in Methods)
        {
            tile.IsSelected = tile.Method == method;
        }

        _payments.Add(new PaymentLine(method, _currency, amount));
        AmountText = string.Empty;
        Recalculate();
        return true;
    }

    [RelayCommand]
    private void Complete()
    {
        if (!CanComplete)
        {
            return;
        }

        var totals = CurrentTotals();
        var saleLines = _cart.Lines.ToList();
        var payments = AllPayments();
        var receipt = _service.Complete(new SaleRequest(saleLines, _discountPercent, totals, payments, _settings.Rates));

        CompletedSaleLines.Clear();
        var number = 0;
        foreach (var line in saleLines)
        {
            number++;
            CompletedSaleLines.Add(new CartLineViewModel(
                number, line.VariantId, line.Name, line.Sku, line.Quantity,
                CurrencyFormat.Amount(_settings.Rates.FromBase(line.UnitPrice, _currency), _currency, _localizer.Culture),
                Show(line.LineTotal), null, static (_, _) => { }));
        }

        CompletedPaymentLines.Clear();
        foreach (var payment in payments)
        {
            CompletedPaymentLines.Add(new PaymentLineViewModel(
                _localizer[$"Pos_Method_{payment.Method}"],
                CurrencyFormat.Amount(payment.Amount, payment.Currency, _localizer.Culture),
                false,
                static () => { }));
        }

        DoneNumberText = _localizer.Format("Pos_DoneNumber", receipt.SaleNumber);
        DoneTotalText = Show(receipt.Totals.Total);
        DonePaidText = Show(PaymentSettlement.Calculate(receipt.Totals.Total, payments, _settings.Rates).Paid);
        DoneTaxLabel = TaxLabel;
        DoneTaxText = Show(receipt.Totals.Tax);
        DoneHasChange = receipt.Change > 0m;
        DoneChangeText = Show(receipt.Change);
        IsCompleted = true;
    }

    public string BuildReceiptText()
    {
        var lines = new List<string> { DoneNumberText, string.Empty };
        lines.AddRange(CompletedSaleLines.Select(line => $"{line.Name}  {line.QuantityText} x {line.ReceiptPriceText}"));
        if (CompletedPaymentLines.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add(_localizer["Pos_PaymentMethod"]);
            lines.AddRange(CompletedPaymentLines.Select(payment => $"{payment.Method}: {payment.Amount}"));
            lines.Add($"{_localizer["Pos_AmountPaid"]}: {DonePaidText}");
        }

        lines.Add(string.Empty);
        lines.Add($"{DoneTaxLabel}: {DoneTaxText}");
        lines.Add($"{_localizer["Pos_Total"]}: {DoneTotalText}");
        if (CompletedPaymentLines.Count > 0)
        {
            lines.AddRange(CompletedPaymentLines.Select(payment => $"{payment.Method}: {payment.Amount}"));
            lines.Add($"{_localizer["Pos_AmountPaid"]}: {DonePaidText}");
        }

        if (DoneHasChange)
        {
            lines.Add($"{_localizer["Pos_ChangeDue"]}: {DoneChangeText}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Starts the next sale: everything cleared, the selected currency and method kept.</summary>
    [RelayCommand]
    private void NewSale()
    {
        ResetSale();
        IsCompleted = false;
        Reload();
    }

    private void Reload()
    {
        _settings = _service.GetSettings();
        if (!_settings.Currencies.Contains(_currency))
        {
            _currency = _settings.Rates.BaseCurrency;
        }

        BuildChoices();
        Recalculate();
    }

    private void ResetSale()
    {
        _cart.Clear();
        _onHand.Clear();
        _payments.Clear();
        _discountPercent = 0m;
        SearchText = string.Empty;
        ScanMessage = string.Empty;
        AmountText = string.Empty;
        DiscountText = string.Empty;
        ApprovalCode = string.Empty;
    }

    private void BuildChoices()
    {
        Currencies.Clear();
        foreach (var code in _settings.Currencies)
        {
            Currencies.Add(new CurrencyChipViewModel(code, CurrencyFormat.ChipLabel(code), code == _currency, SelectCurrency));
        }

        Methods.Clear();
        var usage = _service.GetPaymentMethodUsage();
        foreach (var method in PaymentMethods.All.OrderByDescending(method => usage.GetValueOrDefault(method)))
        {
            Methods.Add(new PaymentMethodViewModel(method, _localizer[$"Pos_Method_{method}"], method == _method));
        }

        RefreshDiscountPresets();
    }

    private void RefreshDiscountPresets()
    {
        var presets = _service.GetDiscountPresets()
            .Where(preset => preset.IsActive)
            .OrderBy(preset => preset.Percent)
            .ToList();
        const double buttonMinWidth = 54;
        const double spacing = 8;
        const double horizontalPadding = 34; // 32 px inner padding plus the 1 px border on each side.
        DiscountFlyoutWidth = Math.Max(224, presets.Count * buttonMinWidth
            + Math.Max(0, presets.Count - 1) * spacing + horizontalPadding);
        var buttonWidth = presets.Count == 0
            ? 0
            : (DiscountFlyoutWidth - horizontalPadding - Math.Max(0, presets.Count - 1) * spacing) / presets.Count;

        DiscountPresets.Clear();
        foreach (var preset in presets)
        {
            DiscountPresets.Add(new DiscountPresetViewModel(preset.Percent, buttonWidth, _localizer.Culture));
        }
    }

    private SaleTotals CurrentTotals() =>
        SaleTotals.Calculate(_cart.Subtotal, _discountPercent, _settings.TaxRate, _settings.TaxMode);

    private bool TryGetEntry(out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(AmountText))
        {
            amount = 0m;
            return true;
        }

        return CurrencyFormat.TryParseAmount(AmountText, _localizer.Culture, out amount);
    }

    private List<PaymentLine> AllPayments() => [.. _payments];

    private string Show(decimal baseAmount) =>
        CurrencyFormat.Amount(_settings.Rates.FromBase(baseAmount, _currency), _currency, _localizer.Culture);

    private bool TryParsePercent(string? text, out decimal percent)
    {
        percent = 0m;
        var trimmed = text?.Trim().TrimEnd('%').Trim();
        return !string.IsNullOrEmpty(trimmed)
            && decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, _localizer.Culture, out percent);
    }

    /// <summary>Recomputes every figure and row from the cart, discount, payments and the amount being typed.</summary>
    private void Recalculate()
    {
        var rates = _settings.Rates;
        var totals = CurrentTotals();
        var settlement = PaymentSettlement.Calculate(totals.Total, _payments, rates);

        Lines.Clear();
        var number = 0;
        foreach (var line in _cart.Lines)
        {
            number++;
            var onHand = _onHand.GetValueOrDefault(line.VariantId);
            Lines.Add(new CartLineViewModel(
                number,
                line.VariantId,
                line.Name,
                line.Sku,
                line.Quantity,
                CurrencyFormat.Amount(rates.FromBase(line.UnitPrice, _currency), _currency, _localizer.Culture),
                Show(line.LineTotal),
                StockNote(onHand, line.Quantity),
                SetQuantity));
        }

        PaymentLines.Clear();
        for (var i = 0; i < _payments.Count; i++)
        {
            var index = i;
            var payment = _payments[i];
            PaymentLines.Add(new PaymentLineViewModel(
                _localizer[$"Pos_Method_{payment.Method}"],
                CurrencyFormat.Amount(payment.Amount, payment.Currency, _localizer.Culture),
                i % 2 == 1,
                () =>
                {
                    _payments.RemoveAt(index);
                    Recalculate();
                }));
        }

        IsCartEmpty = _cart.IsEmpty;
        HasDiscount = _discountPercent > 0m;
        HasPaymentLines = _payments.Count > 0;
        HasAmountToReturn = settlement.Change > 0m;
        HasRemainingToPay = HasPaymentLines && settlement.Remaining > 0m;
        ItemsText = _cart.ItemCount == 1 ? _localizer["Pos_ItemOne"] : _localizer.Format("Pos_Items", _cart.ItemCount);
        PriceHeader = _localizer.Format("Pos_ColPrice", _currency);
        TotalHeader = _localizer.Format("Pos_ColTotal", _currency);
        SubtotalText = Show(totals.Subtotal);
        DiscountTotalText = HasDiscount
            ? $"- {Show(totals.Discount)} ({_discountPercent.ToString("0.##", _localizer.Culture)} %)"
            : Show(0m);
        TaxLabel = _localizer.Format("Pos_Tax", _settings.TaxRate.Percent.ToString("0.##", _localizer.Culture));
        TaxInfo = _localizer[_settings.TaxMode == TaxMode.Included ? "Pos_TaxIncludedInfo" : "Pos_TaxExcludedInfo"];
        TaxText = Show(totals.Tax);
        TotalText = Show(totals.Total);
        DiscountHint = _localizer.Format("Pos_DiscountHint", _settings.DiscountCapPercent.ToString("0.##", _localizer.Culture));

        BalanceText = Show(settlement.Change);
        RemainingToPayText = Show(settlement.Remaining);
        NeedsCashForChange = HasAmountToReturn;

        CanComplete = !_cart.IsEmpty && settlement.IsCovered;
    }

    private string? StockNote(int onHand, int quantity) => StockCheck.Evaluate(onHand, quantity) switch
    {
        StockLevel.OutOfStock => _localizer["Pos_StockOut"],
        StockLevel.Partial => _localizer.Format("Pos_StockPartial", onHand),
        _ => null,
    };
}
