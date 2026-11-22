using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;
using Estoq.Core.Setup;

namespace Estoq.App.ViewModels.Setup;

/// <summary>One step of the setup wizard: its rail entry (number, title, state) and its own validation.</summary>
public abstract partial class WizardStepViewModel : ObservableObject
{
    private readonly string _stepKey;
    private readonly string _contentKey;

    protected WizardStepViewModel(ILocalizer localizer, string stepKey, string contentKey)
    {
        Localizer = localizer;
        _stepKey = stepKey;
        _contentKey = contentKey;
    }

    protected ILocalizer Localizer { get; }

    /// <summary>Errors show only after the owner tries to leave the step, then follow edits live.</summary>
    protected bool ShowErrors { get; private set; }

    public int Number { get; set; }

    public string NumberText => Number.ToString(CultureInfo.InvariantCulture);

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    [ObservableProperty]
    public partial bool IsDone { get; set; }

    public bool IsUpcoming => !IsCurrent && !IsDone;

    /// <summary>Rail title and subtitle.</summary>
    public string Title => Localizer[$"Step_{_stepKey}_Title"];

    public string Subtitle => Localizer[$"Step_{_stepKey}_Subtitle"];

    /// <summary>Page heading and description.</summary>
    public string Heading => Localizer[$"{_contentKey}_Heading"];

    public string Description => Localizer[$"{_contentKey}_Description"];

    /// <summary>Checks the step. Shows errors when it fails; true when the owner may continue.</summary>
    public bool Validate()
    {
        ShowErrors = true;
        return Evaluate();
    }

    /// <summary>Called each time the step becomes the current one.</summary>
    public virtual void OnEntered()
    {
    }

    /// <summary>Re-reads every localized string after a language change.</summary>
    public virtual void RefreshStrings() => OnPropertyChanged(string.Empty);

    /// <summary>Recomputes errors; true when the step is valid. Errors are blank unless <see cref="ShowErrors"/>.</summary>
    protected abstract bool Evaluate();

    protected void Revalidate()
    {
        if (ShowErrors)
        {
            Evaluate();
        }
    }

    /// <summary>The localized message for a failed rule, or blank when the value is fine or errors are not shown yet.</summary>
    protected string Describe(ValidationCode code)
    {
        if (!ShowErrors || code == ValidationCode.None)
        {
            return string.Empty;
        }

        return code == ValidationCode.PasswordTooShort
            ? Localizer.Format("Error_PasswordTooShort", SetupRules.MinPasswordLength)
            : Localizer[$"Error_{code}"];
    }

    partial void OnIsCurrentChanged(bool value) => OnPropertyChanged(nameof(IsUpcoming));

    partial void OnIsDoneChanged(bool value) => OnPropertyChanged(nameof(IsUpcoming));
}
