using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Roles;
using Estoq.Core.Settings;

namespace Estoq.App.ViewModels.Settings;

/// <summary>
/// The Settings pop-up (docs/features/settings.md): a list of sections on the left (only those the role may open) and the
/// selected section on the right. Account and Store edits are saved together by Save changes; Staff and Exchange rates
/// apply as they are made. A singleton, like the other pop-ups; <see cref="Reset"/> runs each time it opens.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ISessionService _session;
    private readonly ILocalizer _localizer;

    public SettingsViewModel(
        ISettingsService settings,
        ISessionService session,
        ILocalizer localizer,
        AccountSettingsViewModel account,
        StoreSettingsViewModel store,
        StaffSettingsViewModel staff,
        ExchangeRatesViewModel rates)
    {
        _settings = settings;
        _session = session;
        _localizer = localizer;
        Account = account;
        Store = store;
        Staff = staff;
        Rates = rates;

        Sections =
        [
            new SettingsSectionItemViewModel(SettingsSection.Account, ""),
            new SettingsSectionItemViewModel(SettingsSection.Store, ""),
            new SettingsSectionItemViewModel(SettingsSection.Staff, ""),
            new SettingsSectionItemViewModel(SettingsSection.ExchangeRates, ""),
        ];

        Account.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(AccountSettingsViewModel.IsDirty)) { OnPropertyChanged(nameof(CanSave)); } };
        Store.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(StoreSettingsViewModel.IsDirty)) { OnPropertyChanged(nameof(CanSave)); } };
        _settings.Changed += (_, _) => RefreshHistory();
        _localizer.LanguageChanged += (_, _) => RefreshStrings();
    }

    /// <summary>Raised after a successful save; the pop-up closes.</summary>
    public event EventHandler? CloseRequested;

    public AccountSettingsViewModel Account { get; }

    public StoreSettingsViewModel Store { get; }

    public StaffSettingsViewModel Staff { get; }

    public ExchangeRatesViewModel Rates { get; }

    /// <summary>Every section; <see cref="VisibleSections"/> holds the ones the role may open.</summary>
    public IReadOnlyList<SettingsSectionItemViewModel> Sections { get; }

    public ObservableCollection<SettingsSectionItemViewModel> VisibleSections { get; } = [];

    public ObservableCollection<HistoryRowViewModel> HistoryRows { get; } = [];

    [ObservableProperty]
    public partial SettingsSection Selected { get; set; } = SettingsSection.Account;

    public bool IsAccount => Selected == SettingsSection.Account;

    public bool IsStore => Selected == SettingsSection.Store;

    public bool IsStaff => Selected == SettingsSection.Staff;

    public bool IsRates => Selected == SettingsSection.ExchangeRates;

    /// <summary>Heading and description of the selected section, shown at the top of the right card.</summary>
    public string SectionTitle => _localizer[$"Settings_{Selected}_Heading"];

    public string SectionDescription => _localizer[$"Settings_{Selected}_Description"];

    /// <summary>Save changes is on only when Account or Store has unsaved edits.</summary>
    public bool CanSave => Account.IsDirty || (SettingsSections.CanOpen(_session.Role, SettingsSection.Store) && Store.IsDirty);

    /// <summary>Owners and managers read the history of Store and Staff.</summary>
    public bool CanShowHistory => SettingsSections.CanSeeHistory(_session.Role) && Selected is SettingsSection.Store or SettingsSection.Staff;

    [ObservableProperty]
    public partial bool IsHistoryOpen { get; set; }

    public bool HasHistoryRows => HistoryRows.Count > 0;

    public bool HasNoHistoryRows => HistoryRows.Count == 0;

    /// <summary>Starts a fresh visit: everything reloaded, Account selected, no unsaved edits, history closed.</summary>
    public void Reset()
    {
        Account.Reset();
        Store.Reset();
        Staff.Reset();
        Rates.Reset();
        IsHistoryOpen = false;
        RefreshStrings();
        Select(SettingsSection.Account);
    }

    public void RefreshStrings()
    {
        var role = _session.Role;
        foreach (var section in Sections)
        {
            section.Title = _localizer[$"Settings_{section.Section}_Title"];
            section.Subtitle = _localizer[$"Settings_{section.Section}_Subtitle"];
        }

        VisibleSections.Clear();
        foreach (var section in Sections.Where(s => SettingsSections.CanOpen(role, s.Section)))
        {
            VisibleSections.Add(section);
        }

        Account.RefreshStrings();
        Store.RefreshStrings();
        Staff.RefreshStrings();
        Rates.RefreshStrings();
        RaiseSectionChanged();
        RefreshHistory();
    }

    [RelayCommand]
    private void Select(SettingsSection section)
    {
        if (!SettingsSections.CanOpen(_session.Role, section))
        {
            return;
        }

        Selected = section;
    }

    [RelayCommand]
    private void ToggleHistory()
    {
        IsHistoryOpen = !IsHistoryOpen;
        RefreshHistory();
    }

    /// <summary>Validates and saves Account and Store together. Selects the section that has a problem and stays open.</summary>
    [RelayCommand]
    private void Save()
    {
        var storeAllowed = SettingsSections.CanOpen(_session.Role, SettingsSection.Store);
        if (Account.IsDirty && !Account.Validate())
        {
            Select(SettingsSection.Account);
            return;
        }

        if (storeAllowed && Store.IsDirty && !Store.Validate())
        {
            Select(SettingsSection.Store);
            return;
        }

        if (storeAllowed && Store.IsDirty && !Store.Apply())
        {
            Select(SettingsSection.Store);
            return;
        }

        var language = Account.IsDirty ? Account.Apply() : null;
        CloseRequested?.Invoke(this, EventArgs.Empty);

        // The language switch rebuilds the screens, so it goes last, after the pop-up has been asked to close.
        if (language is { } chosen)
        {
            _localizer.SetLanguage(chosen);
            _localizer.Remember();
        }
    }

    partial void OnSelectedChanged(SettingsSection value)
    {
        foreach (var section in Sections)
        {
            section.IsSelected = section.Section == value;
        }

        RaiseSectionChanged();
        RefreshHistory();
    }

    private void RaiseSectionChanged()
    {
        OnPropertyChanged(nameof(IsAccount));
        OnPropertyChanged(nameof(IsStore));
        OnPropertyChanged(nameof(IsStaff));
        OnPropertyChanged(nameof(IsRates));
        OnPropertyChanged(nameof(SectionTitle));
        OnPropertyChanged(nameof(SectionDescription));
        OnPropertyChanged(nameof(CanShowHistory));
        OnPropertyChanged(nameof(CanSave));
    }

    private void RefreshHistory()
    {
        HistoryRows.Clear();
        if (IsHistoryOpen && CanShowHistory)
        {
            var index = 0;
            foreach (var change in _settings.History(Selected))
            {
                HistoryRows.Add(new HistoryRowViewModel(change, _localizer, index++ % 2 == 1));
            }
        }

        OnPropertyChanged(nameof(HasHistoryRows));
        OnPropertyChanged(nameof(HasNoHistoryRows));
    }
}
