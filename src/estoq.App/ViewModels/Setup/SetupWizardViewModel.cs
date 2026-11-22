using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Roles;
using Estoq.Core.Setup;

namespace Estoq.App.ViewModels.Setup;

/// <summary>
/// First-run wizard: Welcome, Owner, Store, Preferences, Recovery key. Back keeps entered values; Next is blocked until the
/// step is valid. Registered as a singleton so a language change can rebuild the page without losing input.
/// </summary>
public sealed partial class SetupWizardViewModel : ObservableObject
{
    private readonly ISetupService _setup;
    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;
    private bool _isCompleting;

    public SetupWizardViewModel(
        ISetupService setup,
        ILocalizer localizer,
        ISessionService session,
        WelcomeStepViewModel welcome,
        OwnerStepViewModel owner,
        StoreStepViewModel store,
        PreferencesStepViewModel preferences,
        RecoveryStepViewModel recovery)
    {
        _setup = setup;
        _localizer = localizer;
        _session = session;
        Welcome = welcome;
        Owner = owner;
        Store = store;
        Preferences = preferences;
        Recovery = recovery;

        Steps = [welcome, owner, store, preferences, recovery];
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].Number = i + 1;
        }

        Recovery.ConfirmationChanged += (_, _) => NextCommand.NotifyCanExecuteChanged();
        _localizer.LanguageChanged += OnLanguageChanged;
        ShowStep(0);
    }

    public WelcomeStepViewModel Welcome { get; }

    public OwnerStepViewModel Owner { get; }

    public StoreStepViewModel Store { get; }

    public PreferencesStepViewModel Preferences { get; }

    public RecoveryStepViewModel Recovery { get; }

    public IReadOnlyList<WizardStepViewModel> Steps { get; }

    /// <summary>Raised after the language changed and every string was refreshed; the page rebuilds itself.</summary>
    public event EventHandler? LanguageApplied;

    /// <summary>Raised once setup is accepted; the app then opens the main window content.</summary>
    public event EventHandler? Completed;

    [ObservableProperty]
    public partial int CurrentIndex { get; set; }

    [ObservableProperty]
    public partial string FinishError { get; set; } = string.Empty;

    public WizardStepViewModel Current => Steps[CurrentIndex];

    public bool IsWelcomeStep => CurrentIndex == 0;

    public bool IsOwnerStep => CurrentIndex == 1;

    public bool IsStoreStep => CurrentIndex == 2;

    public bool IsPreferencesStep => CurrentIndex == 3;

    public bool IsRecoveryStep => CurrentIndex == 4;

    public bool IsLastStep => CurrentIndex == Steps.Count - 1;

    public bool IsBackVisible => CurrentIndex > 0;

    public bool HasFinishError => FinishError.Length > 0;

    public double ProgressMaximum => Steps.Count;

    public double ProgressValue => CurrentIndex + 1;

    public string StepOfText => _localizer.Format("Wizard_StepOf", CurrentIndex + 1, Steps.Count);

    public string NextText => _localizer[CurrentIndex switch
    {
        0 => "Wizard_Start",
        _ when IsLastStep => "Wizard_Finish",
        _ => "Wizard_Next",
    }];

    /// <summary>The forward chevron is hidden on Finish.</summary>
    public bool ShowNextArrow => !IsLastStep;

    public string BackText => _localizer["Wizard_Back"];

    public string WizardTitle => _localizer["Wizard_Title"];

    public string WizardTagline => _localizer["Wizard_Tagline"];

    private bool CanGoNext() => !IsLastStep || Recovery.HasConfirmed;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextAsync()
    {
        if (!Current.Validate())
        {
            return;
        }

        if (!IsLastStep)
        {
            ShowStep(CurrentIndex + 1);
            return;
        }

        await FinishAsync();
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentIndex > 0)
        {
            ShowStep(CurrentIndex - 1);
        }
    }

    private async Task FinishAsync()
    {
        var request = new SetupRequest(
            Owner.FullName.Trim(),
            Owner.Email.Trim(),
            Owner.UserCode,
            Store.StoreName.Trim(),
            Store.TaxId.Trim(),
            Store.Address.Trim(),
            Store.City.Trim(),
            Store.PostalCode.Trim(),
            Store.CountryCode,
            Store.Phone.Trim(),
            Store.Email.Trim(),
            Store.LogoPath,
            Preferences.BaseCurrencyCode ?? Currencies.Default,
            Preferences.AcceptedCurrencies,
            Preferences.TaxRate,
            Preferences.TaxMode,
            Welcome.SelectedLanguage,
            Store.StoreLanguage);

        var result = await _setup.CompleteAsync(request, Owner.Password);
        if (result != ValidationCode.None)
        {
            FinishError = _localizer["Wizard_FinishError"];
            return;
        }

        FinishError = string.Empty;

        // Setup creates the owner, who is signed in straight away.
        _session.SignIn(new SessionUser(request.OwnerName, UserRole.Owner));

        // From here on the whole app follows the store language. The wizard must not rebuild itself for this change.
        _isCompleting = true;
        _localizer.SetLanguage(request.StoreLanguage);
        _localizer.Remember();
        Owner.ClearSecrets();
        Recovery.Clear();
        Completed?.Invoke(this, EventArgs.Empty);
    }

    private void ShowStep(int index)
    {
        CurrentIndex = index;
        for (var i = 0; i < Steps.Count; i++)
        {
            Steps[i].IsCurrent = i == index;
            Steps[i].IsDone = i < index;
        }

        Current.OnEntered();
        FinishError = string.Empty;
        NextCommand.NotifyCanExecuteChanged();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (_isCompleting)
        {
            return;
        }

        foreach (var step in Steps)
        {
            step.RefreshStrings();
        }

        Store.FollowWizardLanguage(_localizer.Language);
        OnPropertyChanged(string.Empty);
        LanguageApplied?.Invoke(this, EventArgs.Empty);
    }

    partial void OnCurrentIndexChanged(int value) => OnPropertyChanged(string.Empty);

    partial void OnFinishErrorChanged(string value) => OnPropertyChanged(nameof(HasFinishError));
}
