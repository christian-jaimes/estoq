using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Setup;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.Setup;

public sealed record CountryItem(string Code, string Name);

/// <summary>Step 3. Only the store name is required; the logo is a PNG or JPG up to 2 MB.</summary>
public sealed partial class StoreStepViewModel : WizardStepViewModel
{
    private readonly ILogoPickerService _logoPicker;
    private bool _languageTouched;

    public StoreStepViewModel(ILocalizer localizer, ILogoPickerService logoPicker)
        : base(localizer, "Store", "Store")
    {
        _logoPicker = logoPicker;
        BuildCountries();
        StoreLanguageIndex = localizer.Language == AppLanguage.Spanish ? 1 : 0;
        _languageTouched = false;
    }

    public ObservableCollection<CountryItem> Countries { get; } = [];

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

    /// <summary>ISO 3166 two-letter code of the selected country, or null.</summary>
    [ObservableProperty]
    public partial string? CountryCode { get; set; }

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StoreNameError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmailError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? LogoPath { get; set; }

    [ObservableProperty]
    public partial ImageSource? LogoPreview { get; set; }

    [ObservableProperty]
    public partial string LogoError { get; set; } = string.Empty;

    /// <summary>0 English, 1 Spanish. Follows the wizard language until the owner picks one here.</summary>
    [ObservableProperty]
    public partial int StoreLanguageIndex { get; set; }

    public AppLanguage StoreLanguage => StoreLanguageIndex == 1 ? AppLanguage.Spanish : AppLanguage.English;

    public bool HasLogo => LogoPath is not null;

    public bool HasNoLogo => LogoPath is null;

    public string LogoButtonText => Localizer[HasLogo ? "Store_ChangeLogo" : "Store_AddLogo"];

    protected override bool Evaluate()
    {
        var name = SetupRules.ValidateRequired(StoreName);
        var email = SetupRules.ValidateOptionalEmail(Email);
        StoreNameError = Describe(name);
        EmailError = Describe(email);
        return name == ValidationCode.None && email == ValidationCode.None;
    }

    public override void RefreshStrings()
    {
        BuildCountries();
        base.RefreshStrings();
        Revalidate();
    }

    public void FollowWizardLanguage(AppLanguage language)
    {
        if (_languageTouched)
        {
            return;
        }

        StoreLanguageIndex = language == AppLanguage.Spanish ? 1 : 0;
        _languageTouched = false;
    }

    [RelayCommand]
    private async Task PickLogoAsync()
    {
        var picked = await _logoPicker.PickAsync();
        if (picked is null)
        {
            return;
        }

        var problem = SetupRules.ValidateLogo(picked.FileName, picked.SizeInBytes);
        if (problem != ValidationCode.None)
        {
            LogoError = Localizer[$"Error_{problem}"];
            return;
        }

        LogoError = string.Empty;
        LogoPreview = await _logoPicker.LoadPreviewAsync(picked.Path);
        LogoPath = picked.Path;
    }

    [RelayCommand]
    private void RemoveLogo()
    {
        LogoPath = null;
        LogoPreview = null;
        LogoError = string.Empty;
    }

    /// <summary>Country names in the current language, sorted. The selection is kept.</summary>
    private void BuildCountries()
    {
        var selected = CountryCode;
        var items = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .Where(region => region.TwoLetterISORegionName.Length == 2 && !char.IsDigit(region.TwoLetterISORegionName[0]))
            .GroupBy(region => region.TwoLetterISORegionName)
            .Select(group => new CountryItem(group.Key, group.First().DisplayName))
            .OrderBy(country => country.Name, StringComparer.Create(Localizer.Culture, ignoreCase: true));

        Countries.Clear();
        foreach (var item in items)
        {
            Countries.Add(item);
        }

        CountryCode = selected;
    }

    partial void OnStoreLanguageIndexChanged(int value)
    {
        _languageTouched = true;
        OnPropertyChanged(nameof(StoreLanguage));
    }

    partial void OnStoreNameChanged(string value) => Revalidate();

    partial void OnEmailChanged(string value) => Revalidate();

    partial void OnLogoPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasLogo));
        OnPropertyChanged(nameof(HasNoLogo));
        OnPropertyChanged(nameof(LogoButtonText));
    }
}
