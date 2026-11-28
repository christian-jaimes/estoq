using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Settings;

namespace Estoq.App.ViewModels.Settings;

/// <summary>One currency in the Exchange rates table.</summary>
public sealed class RateRowViewModel
{
    public RateRowViewModel(string code, string name, bool isBase, string rateText, string updatedText, string byText, bool isAlternate)
    {
        Code = code;
        Name = name;
        IsBase = isBase;
        IsNotBase = !isBase;
        RateText = rateText;
        UpdatedText = updatedText;
        ByText = byText;
        IsAlternateRow = isAlternate;
    }

    public string Code { get; }

    public string Name { get; }

    public bool IsBase { get; }

    public bool IsNotBase { get; }

    public string RateText { get; }

    public string UpdatedText { get; }

    public string ByText { get; }

    public bool IsAlternateRow { get; }
}

/// <summary>One recorded rate in a currency's history.</summary>
public sealed class RateHistoryRowViewModel
{
    public RateHistoryRowViewModel(RateEntry entry, ILocalizer localizer, bool isAlternate)
    {
        RateText = entry.Rate.ToString("0.######", localizer.Culture);
        EffectiveText = entry.EffectiveAt.ToLocalTime().ToString("g", localizer.Culture);
        By = entry.SetBy;
        IsAlternateRow = isAlternate;
    }

    public string RateText { get; }

    public string EffectiveText { get; }

    public string By { get; }

    public bool IsAlternateRow { get; }
}

/// <summary>
/// The Exchange rates section: the rate in force for each accepted currency, an "Update rate" form and a history per
/// currency. Open to every role. Rates are appended, never overwritten, and apply at once (not with Save changes).
/// </summary>
public sealed partial class ExchangeRatesViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILocalizer _localizer;

    public ExchangeRatesViewModel(ISettingsService settings, ILocalizer localizer)
    {
        _settings = settings;
        _localizer = localizer;
        _settings.Changed += (_, _) => Refresh();
    }

    public ObservableCollection<RateRowViewModel> Rows { get; } = [];

    /// <summary>The currencies that can get a rate (every accepted currency except the base).</summary>
    public ObservableCollection<string> Editable { get; } = [];

    public ObservableCollection<RateHistoryRowViewModel> History { get; } = [];

    [ObservableProperty]
    public partial string BaseCurrency { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SelectedCurrency { get; set; }

    [ObservableProperty]
    public partial string NewRateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Error { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notice { get; set; } = string.Empty;

    /// <summary>The currency whose history is open, or null.</summary>
    [ObservableProperty]
    public partial string? HistoryCurrency { get; set; }

    public bool HasHistory => HistoryCurrency is not null;

    public bool HasEditable => Editable.Count > 0;

    public string HistoryTitle => HistoryCurrency is null ? string.Empty : _localizer.Format("Settings_RatesHistoryTitle", HistoryCurrency, BaseCurrency);

    /// <summary>Explains the rate: "How many EUR for 1 USD".</summary>
    public string RateLabel => SelectedCurrency is null ? _localizer["Settings_RatesNewRate"] : _localizer.Format("Settings_RatesNewRateFor", SelectedCurrency, BaseCurrency);

    public void Reset()
    {
        NewRateText = Error = Notice = string.Empty;
        HistoryCurrency = null;
        SelectedCurrency = null;
        Refresh();
    }

    public void RefreshStrings() => Refresh();

    [RelayCommand]
    private void Update()
    {
        if (SelectedCurrency is null)
        {
            Error = _localizer["Settings_RatesPickCurrency"];
            return;
        }

        var ok = ExchangeRateBook.TryParseRate(NewRateText, _localizer.Culture, out var rate)
            || ExchangeRateBook.TryParseRate(NewRateText, CultureInfo.InvariantCulture, out rate);
        if (!ok)
        {
            Error = _localizer["Settings_RatesInvalid"];
            return;
        }

        if (!_settings.UpdateRate(SelectedCurrency, rate, DateTimeOffset.Now))
        {
            Error = _localizer["Settings_RatesInvalid"];
            return;
        }

        Error = string.Empty;
        Notice = _localizer.Format("Settings_RatesSaved", SelectedCurrency, rate.ToString("0.######", _localizer.Culture), BaseCurrency);
        NewRateText = string.Empty;
    }

    [RelayCommand]
    private void StartUpdate(RateRowViewModel? row)
    {
        if (row is null || row.IsBase)
        {
            return;
        }

        SelectedCurrency = row.Code;
        Notice = Error = string.Empty;
    }

    [RelayCommand]
    private void ShowHistory(RateRowViewModel? row)
    {
        HistoryCurrency = HistoryCurrency == row?.Code ? null : row?.Code;
        Refresh();
    }

    [RelayCommand]
    private void CloseHistory()
    {
        HistoryCurrency = null;
        Refresh();
    }

    private void Refresh()
    {
        var rates = _settings.CurrentRates();
        BaseCurrency = rates.FirstOrDefault(r => r.IsBase)?.Currency ?? string.Empty;

        Rows.Clear();
        var index = 0;
        foreach (var rate in rates)
        {
            var name = CurrencyCatalog.Find(rate.Currency)?.Name ?? rate.Currency;
            Rows.Add(rate.IsBase
                ? new RateRowViewModel(rate.Currency, name, true, "1", string.Empty, string.Empty, index % 2 == 1)
                : new RateRowViewModel(
                    rate.Currency,
                    name,
                    false,
                    rate.Entry is null ? "—" : rate.Entry.Rate.ToString("0.######", _localizer.Culture),
                    rate.Entry is null ? _localizer["Settings_RatesNotSet"] : rate.Entry.EffectiveAt.ToLocalTime().ToString("g", _localizer.Culture),
                    rate.Entry?.SetBy ?? string.Empty,
                    index % 2 == 1));
            index++;
        }

        var editable = rates.Where(r => !r.IsBase).Select(r => r.Currency).ToList();
        Editable.Clear();
        foreach (var code in editable)
        {
            Editable.Add(code);
        }

        if (SelectedCurrency is not null && !editable.Contains(SelectedCurrency))
        {
            SelectedCurrency = null;
        }

        SelectedCurrency ??= editable.FirstOrDefault();

        History.Clear();
        if (HistoryCurrency is { } currency)
        {
            var i = 0;
            foreach (var entry in _settings.RateHistory(currency))
            {
                History.Add(new RateHistoryRowViewModel(entry, _localizer, i++ % 2 == 1));
            }
        }

        OnPropertyChanged(nameof(HasEditable));
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HistoryTitle));
        OnPropertyChanged(nameof(RateLabel));
    }

    partial void OnSelectedCurrencyChanged(string? value)
    {
        OnPropertyChanged(nameof(RateLabel));
        Error = string.Empty;
    }

    partial void OnHistoryCurrencyChanged(string? value)
    {
        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HistoryTitle));
    }
}
