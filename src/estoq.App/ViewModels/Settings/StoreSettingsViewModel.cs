using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.App.ViewModels.Setup;
using Estoq.Core.Roles;
using Estoq.Core.Settings;
using Estoq.Core.Setup;
using Estoq.Core.SignIn;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.Settings;

/// <summary>
/// The Store section (mockup 10): logo, store details, currencies, tax and store language, plus the owner-only automatic
/// log out time. Same field rules as the setup wizard. Edits stay local until <see cref="Apply"/>.
/// </summary>
public sealed partial class StoreSettingsViewModel : ObservableObject
{
    private const int MaxSuggestions = 8;

    private readonly ISettingsService _settings;
    private readonly ISessionService _session;
    private readonly ILocalizer _localizer;
    private readonly ILogoPickerService _picker;
    private StoreProfile _saved;
    private TimeSpan _savedTimeout;
    private int _savedLowStock;
    private bool _loading;

    public StoreSettingsViewModel(ISettingsService settings, ISessionService session, ILocalizer localizer, ILogoPickerService picker)
    {
        _settings = settings;
        _session = session;
        _localizer = localizer;
        _picker = picker;
        _saved = settings.GetStore();
        _savedTimeout = settings.InactivityTimeout;
        _savedLowStock = settings.LowStockThreshold;
        Accepted.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasAccepted));
            OnPropertyChanged(nameof(HasNoAccepted));
            OnPropertyChanged(nameof(IsDirty));
        };
    }

    public ObservableCollection<CountryItem> Countries { get; } = [];

    public IReadOnlyList<CurrencyItem> AllCurrencies => CurrencyCatalog.All;

    public ObservableCollection<CurrencyItem> Accepted { get; } = [];

    public ObservableCollection<CurrencyItem> Suggestions { get; } = [];

    public bool HasAccepted => Accepted.Count > 0;

    public bool HasNoAccepted => Accepted.Count == 0;

    public string NoAcceptedText => _localizer.Format("Prefs_AcceptedNone", BaseCurrencyCode ?? Currencies.Default);

    [ObservableProperty]
    public partial string StoreName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TaxId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string City { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PostalCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? CountryCode { get; set; }

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? BaseCurrencyCode { get; set; } = Currencies.Default;

    [ObservableProperty]
    public partial string TaxRateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool PricesIncludeTax { get; set; }

    public string TaxModeText => _localizer[PricesIncludeTax ? "Prefs_TaxIncludedOn" : "Prefs_TaxIncludedOff"];

    /// <summary>0 English, 1 Spanish.</summary>
    [ObservableProperty]
    public partial int StoreLanguageIndex { get; set; }

    [ObservableProperty]
    public partial string? LogoPath { get; set; }

    [ObservableProperty]
    public partial ImageSource? LogoPreview { get; set; }

    [ObservableProperty]
    public partial string LogoError { get; set; } = string.Empty;

    public bool HasLogo => LogoPath is not null;

    public bool HasNoLogo => LogoPath is null;

    public string LogoButtonText => _localizer[HasLogo ? "Store_ChangeLogo" : "Store_AddLogo"];

    // Automatic log out: owner only.
    [ObservableProperty]
    public partial bool CanEditTimeout { get; set; }

    [ObservableProperty]
    public partial string TimeoutText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TimeoutError { get; set; } = string.Empty;

    // Low stock threshold: owner only (shares CanEditTimeout, which means "is the owner").
    [ObservableProperty]
    public partial string LowStockText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LowStockError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StoreNameError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmailError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BaseCurrencyError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TaxRateError { get; set; } = string.Empty;

    public bool IsDirty => !_loading && (!ProfileFields().SequenceEqual(_saved.ToFields()) || TimeoutMinutesOrSaved() != _savedTimeout.TotalMinutes
        || LowStockOrSaved() != _savedLowStock);

    /// <summary>Loads the saved store details and drops unsaved edits.</summary>
    public void Reset()
    {
        _loading = true;
        _saved = _settings.GetStore();
        _savedTimeout = _settings.InactivityTimeout;
        _savedLowStock = _settings.LowStockThreshold;
        BuildCountries();
        StoreName = _saved.Name;
        TaxId = _saved.TaxId;
        Address = _saved.Address;
        City = _saved.City;
        PostalCode = _saved.PostalCode;
        CountryCode = _saved.CountryCode;
        Phone = _saved.Phone;
        Email = _saved.Email;
        BaseCurrencyCode = _saved.BaseCurrency;
        Accepted.Clear();
        foreach (var code in _saved.AcceptedCurrencies)
        {
            Accepted.Add(CurrencyCatalog.Find(code) ?? new CurrencyItem(code, code));
        }

        TaxRateText = _saved.TaxRate.Percent.ToString("0.##", _localizer.Culture);
        PricesIncludeTax = _saved.TaxMode == TaxMode.Included;
        StoreLanguageIndex = _saved.StoreLanguage == AppLanguage.Spanish ? 1 : 0;
        LogoPath = _saved.LogoPath;
        LogoPreview = null;
        LogoError = string.Empty;
        CanEditTimeout = _session.Role == UserRole.Owner;
        TimeoutText = _savedTimeout.TotalMinutes.ToString("0.##", _localizer.Culture);
        LowStockText = _savedLowStock.ToString(_localizer.Culture);
        StoreNameError = EmailError = BaseCurrencyError = TaxRateError = TimeoutError = LowStockError = string.Empty;
        Suggestions.Clear();
        _loading = false;
        OnPropertyChanged(nameof(NoAcceptedText));
        OnPropertyChanged(nameof(IsDirty));
        _ = LoadPreviewAsync();
    }

    public void RefreshStrings()
    {
        BuildCountries();
        OnPropertyChanged(nameof(TaxModeText));
        OnPropertyChanged(nameof(NoAcceptedText));
        OnPropertyChanged(nameof(LogoButtonText));
    }

    /// <summary>Checks the fields; shows errors and returns false when something is wrong.</summary>
    public bool Validate()
    {
        var name = StoreRules.ValidateName(StoreName);
        var email = StoreRules.ValidateEmail(Email);
        var currency = Currencies.IsValidCode(BaseCurrencyCode) ? ValidationCode.None : ValidationCode.InvalidCurrency;
        var tax = TryParseRate(out _) ? ValidationCode.None : ValidationCode.InvalidTaxRate;
        StoreNameError = Describe(name);
        EmailError = Describe(email);
        BaseCurrencyError = Describe(currency);
        TaxRateError = Describe(tax);
        var timeoutOk = !CanEditTimeout || TimeoutIsValid();
        TimeoutError = timeoutOk ? string.Empty : _localizer.Format("Settings_TimeoutInvalid",
            (int)InactivityPolicy.MinTimeout.TotalMinutes, (int)InactivityPolicy.MaxTimeout.TotalMinutes);
        var lowStockOk = !CanEditTimeout || LowStockIsValid();
        LowStockError = lowStockOk ? string.Empty : _localizer.Format("Settings_LowStockInvalid", LowStockPolicy.MinThreshold, LowStockPolicy.MaxThreshold);
        return name == ValidationCode.None && email == ValidationCode.None && currency == ValidationCode.None
            && tax == ValidationCode.None && timeoutOk && lowStockOk;
    }

    /// <summary>Saves the store details and, for the owner, the log out time.</summary>
    public bool Apply()
    {
        if (_settings.SaveStore(BuildProfile()) != ValidationCode.None)
        {
            return false;
        }

        if (CanEditTimeout && TimeoutIsValid())
        {
            _settings.SaveInactivityTimeout(TimeSpan.FromMinutes(TimeoutMinutes()));
        }

        if (CanEditTimeout && LowStockIsValid())
        {
            _settings.SaveLowStockThreshold(LowStockValue());
        }

        return true;
    }

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

    public void AddCurrency(CurrencyItem item)
    {
        if (item.Code != BaseCurrencyCode && Accepted.All(added => added.Code != item.Code))
        {
            Accepted.Add(item);
        }
    }

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

    [RelayCommand]
    private async Task PickLogoAsync()
    {
        var picked = await _picker.PickAsync();
        if (picked is null)
        {
            return;
        }

        var problem = SetupRules.ValidateLogo(picked.FileName, picked.SizeInBytes);
        if (problem != ValidationCode.None)
        {
            LogoError = _localizer[$"Error_{problem}"];
            return;
        }

        LogoError = string.Empty;
        LogoPreview = await _picker.LoadPreviewAsync(picked.Path);
        LogoPath = picked.Path;
    }

    [RelayCommand]
    private void RemoveLogo()
    {
        LogoPath = null;
        LogoPreview = null;
        LogoError = string.Empty;
    }

    private StoreProfile BuildProfile() => new(
        StoreName,
        TaxId.Trim(),
        Address.Trim(),
        City.Trim(),
        PostalCode.Trim(),
        CountryCode,
        Phone.Trim(),
        Email.Trim(),
        BaseCurrencyCode ?? Currencies.Default,
        Currencies.NormalizeAccepted(BaseCurrencyCode ?? Currencies.Default, Accepted.Select(item => item.Code)),
        TryParseRate(out var rate) ? rate : _saved.TaxRate,
        PricesIncludeTax ? TaxMode.Included : TaxMode.Excluded,
        StoreLanguageIndex == 1 ? AppLanguage.Spanish : AppLanguage.English,
        LogoPath);

    private IReadOnlyDictionary<string, string> ProfileFields() => BuildProfile().ToFields();

    private bool TryParseRate(out TaxRate rate) =>
        TaxRate.TryParse(TaxRateText, _localizer.Culture, out rate) || TaxRate.TryParse(TaxRateText, CultureInfo.InvariantCulture, out rate);

    private bool TimeoutIsValid() =>
        double.TryParse(TimeoutText, NumberStyles.AllowDecimalPoint, _localizer.Culture, out var minutes)
        && minutes == Math.Floor(minutes) && InactivityPolicy.IsValidTimeout(TimeSpan.FromMinutes(minutes));

    private double TimeoutMinutes() =>
        double.TryParse(TimeoutText, NumberStyles.AllowDecimalPoint, _localizer.Culture, out var minutes) ? minutes : _savedTimeout.TotalMinutes;

    private bool LowStockIsValid() =>
        int.TryParse(LowStockText, NumberStyles.None, _localizer.Culture, out var value) && LowStockPolicy.IsValid(value);

    private int LowStockValue() =>
        int.TryParse(LowStockText, NumberStyles.None, _localizer.Culture, out var value) ? value : _savedLowStock;

    private int LowStockOrSaved() => CanEditTimeout ? LowStockValue() : _savedLowStock;

    private double TimeoutMinutesOrSaved() => CanEditTimeout ? TimeoutMinutes() : _savedTimeout.TotalMinutes;

    private string Describe(ValidationCode code) => code == ValidationCode.None ? string.Empty : _localizer[$"Error_{code}"];

    private void BuildCountries()
    {
        var selected = CountryCode;
        var items = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .Where(region => region.TwoLetterISORegionName.Length == 2 && !char.IsDigit(region.TwoLetterISORegionName[0]))
            .GroupBy(region => region.TwoLetterISORegionName)
            .Select(group => new CountryItem(group.Key, group.First().DisplayName))
            .OrderBy(country => country.Name, StringComparer.Create(_localizer.Culture, ignoreCase: true));
        Countries.Clear();
        foreach (var item in items)
        {
            Countries.Add(item);
        }

        CountryCode = selected;
    }

    private async Task LoadPreviewAsync()
    {
        if (LogoPath is { } path)
        {
            LogoPreview = await _picker.LoadPreviewAsync(path);
        }
    }

    private void Touch() => OnPropertyChanged(nameof(IsDirty));

    partial void OnStoreNameChanged(string value) { StoreNameError = string.Empty; Touch(); }

    partial void OnEmailChanged(string value) { EmailError = string.Empty; Touch(); }

    partial void OnTaxRateTextChanged(string value) { TaxRateError = string.Empty; Touch(); }

    partial void OnTaxIdChanged(string value) => Touch();

    partial void OnAddressChanged(string value) => Touch();

    partial void OnCityChanged(string value) => Touch();

    partial void OnPostalCodeChanged(string value) => Touch();

    partial void OnCountryCodeChanged(string? value) => Touch();

    partial void OnPhoneChanged(string value) => Touch();

    partial void OnStoreLanguageIndexChanged(int value) => Touch();

    partial void OnLowStockTextChanged(string value) { LowStockError = string.Empty; Touch(); }

    partial void OnTimeoutTextChanged(string value) { TimeoutError = string.Empty; Touch(); }

    partial void OnPricesIncludeTaxChanged(bool value)
    {
        OnPropertyChanged(nameof(TaxModeText));
        Touch();
    }

    partial void OnLogoPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasLogo));
        OnPropertyChanged(nameof(HasNoLogo));
        OnPropertyChanged(nameof(LogoButtonText));
        Touch();
    }

    partial void OnBaseCurrencyCodeChanged(string? value)
    {
        if (Accepted.FirstOrDefault(item => item.Code == value) is { } clash)
        {
            Accepted.Remove(clash);
        }

        BaseCurrencyError = string.Empty;
        OnPropertyChanged(nameof(NoAcceptedText));
        Touch();
    }
}
