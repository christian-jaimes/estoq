using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;
using Estoq.Core.Setup;

namespace Estoq.App.ViewModels.Setup;

/// <summary>Step 1. The language chosen here is the app language for the rest of the wizard and for the owner.</summary>
public sealed partial class WelcomeStepViewModel : WizardStepViewModel
{
    public WelcomeStepViewModel(ILocalizer localizer)
        : base(localizer, "Welcome", "Welcome")
    {
        LanguageIndex = localizer.Language == AppLanguage.Spanish ? 1 : 0;
    }

    /// <summary>0 English, 1 Spanish.</summary>
    [ObservableProperty]
    public partial int LanguageIndex { get; set; }

    public AppLanguage SelectedLanguage => LanguageIndex == 1 ? AppLanguage.Spanish : AppLanguage.English;

    protected override bool Evaluate() => true;

    partial void OnLanguageIndexChanged(int value)
    {
        OnPropertyChanged(nameof(SelectedLanguage));
        if (value is 0 or 1)
        {
            Localizer.SetLanguage(SelectedLanguage);
        }
    }
}
