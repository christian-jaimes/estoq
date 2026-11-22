using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;
using Estoq.Core.Setup;

namespace Estoq.App.ViewModels.Setup;

/// <summary>
/// Step 4. The base and default currency is USD unless changed; other currencies are found by search and added as chips.
/// The tax rate defaults to 0 % and prices are tax-excluded unless the toggle says otherwise.
/// </summary>
public sealed partial class PreferencesStepViewModel : WizardStepViewModel
{
    private const int MaxSuggestions = 8;

    public PreferencesStepViewModel(ILocalizer localizer)
        : base(localizer, "Preferences", "Prefs")
    {
        Accepted.CollectionChanged += (_, _) => NotifyAcceptedChanged();
    }

    public IReadOnlyList<CurrencyItem> AllCurrencies => CurrencyCatalog.All;

    /// <summary>Code of the base and default currency.</summary>
    [ObservableProperty]
    public partial string? BaseCurrencyCode { get; set; } = Currencies.Default;

    [ObservableProperty]
    public partial string BaseCurrencyError { get; set; } = string.Empty;

    /// <summary>Extra currencies accepted at the till, in the order added. Never contains the base currency.</summary>
    public ObservableCollection<CurrencyItem> Accepted { get; } = [];

    /// <summary>Search results for the currency box.</summary>
    public ObservableCollection<CurrencyItem> Suggestions { get; } = [];

    public bool HasAccepted => Accepted.Count > 0;

    public bool HasNoAccepted => Accepted.Count == 0;

    public string NoAcceptedText => Localizer.Format("Prefs_AcceptedNone", BaseCurrencyCode ?? Currencies.Default);

    /// <summary>Text as typed; starts at the default rate.</summary>
    [ObservableProperty]
    public partial string TaxRateText { get; set; } = TaxRate.Default.Percent.ToString(CultureInfo.InvariantCulture);

    [ObservableProperty]
    public partial string TaxRateError { get; set; } = string.Empty;

    /// <summary>True when entered prices already include tax.</summary>
    [ObservableProperty]
    public partial bool PricesIncludeTax { get; set; }

    /// <summary>Explains the current state of the toggle; shown beside it so a long translation wraps instead of clipping.</summary>
    public string TaxModeText => Localizer[PricesIncludeTax ? "Prefs_TaxIncludedOn" : "Prefs_TaxIncludedOff"];

    public TaxMode TaxMode => PricesIncludeTax ? TaxMode.Included : TaxMode.Excluded;

    public IReadOnlyList<string> AcceptedCurrencies =>
        Currencies.NormalizeAccepted(BaseCurrencyCode ?? Currencies.Default, Accepted.Select(item => item.Code));

    /// <summary>The parsed rate, or the default when the text is not valid (never reached after validation).</summary>
    public TaxRate TaxRate => TryParseRate(out var rate) ? rate : TaxRate.Default;

    /// <summary>Fills <see cref="Suggestions"/> with currencies matching the text by code or name, excluding the base and those already added.</summary>
    public void UpdateSuggestions(string? text)
    {
        Suggestions.Clear();
        var query = text?.Trim() ?? string.Empty;
        if (query.Length == 0)
        {
            return;
        }

        var matches = AllCurrencies
            .Where(item => item.Code != BaseCurrencyCode && Accepted.All(added => added.Code != item.Code))
            .Where(item => item.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Code.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .Take(MaxSuggestions);

        foreach (var match in matches)
        {
            Suggestions.Add(match);
        }
    }

    /// <summary>Adds a currency unless it is the base or already there.</summary>
    public void AddCurrency(CurrencyItem item)
    {
        if (item.Code == BaseCurrencyCode || Accepted.Any(added => added.Code == item.Code))
        {
            return;
        }

        Accepted.Add(item);
    }

    /// <summary>Adds the top search match for the typed text (Enter without choosing a suggestion).</summary>
    public void AddBestMatch(string? text)
    {
        UpdateSuggestions(text);
        if (Suggestions.FirstOrDefault() is { } best)
        {
            AddCurrency(best);
        }
    }

    public void RemoveCurrency(string? code)
    {
        if (Accepted.FirstOrDefault(item => item.Code == code) is { } item)
        {
            Accepted.Remove(item);
        }
    }

    protected override bool Evaluate()
    {
        var currency = Currencies.IsValidCode(BaseCurrencyCode) ? ValidationCode.None : ValidationCode.InvalidCurrency;
        var tax = TryParseRate(out _) ? ValidationCode.None : ValidationCode.InvalidTaxRate;
        BaseCurrencyError = Describe(currency);
        TaxRateError = Describe(tax);
        return currency == ValidationCode.None && tax == ValidationCode.None;
    }

    public override void RefreshStrings()
    {
        base.RefreshStrings();
        Revalidate();
    }

    /// <summary>Accepts the wizard language's decimal mark, and a dot as a fallback.</summary>
    private bool TryParseRate(out TaxRate rate) =>
        TaxRate.TryParse(TaxRateText, Localizer.Culture, out rate)
        || TaxRate.TryParse(TaxRateText, CultureInfo.InvariantCulture, out rate);

    private void NotifyAcceptedChanged()
    {
        OnPropertyChanged(nameof(HasAccepted));
        OnPropertyChanged(nameof(HasNoAccepted));
    }

    partial void OnBaseCurrencyCodeChanged(string? value)
    {
        // The base is always accepted, so it leaves the extra list when chosen as the base.
        if (Accepted.FirstOrDefault(item => item.Code == value) is { } clash)
        {
            Accepted.Remove(clash);
        }

        OnPropertyChanged(nameof(NoAcceptedText));
        Revalidate();
    }

    partial void OnTaxRateTextChanged(string value) => Revalidate();

    partial void OnPricesIncludeTaxChanged(bool value) => OnPropertyChanged(nameof(TaxModeText));
}
