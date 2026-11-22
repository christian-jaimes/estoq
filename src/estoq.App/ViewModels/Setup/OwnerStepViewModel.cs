using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Setup;

namespace Estoq.App.ViewModels.Setup;

/// <summary>Step 2. The owner administers the store, so an email is required. Sign-in is by the generated six-digit code.</summary>
public sealed partial class OwnerStepViewModel : WizardStepViewModel
{
    private readonly IClipboardService _clipboard;

    public OwnerStepViewModel(ILocalizer localizer, ISetupService setup, IClipboardService clipboard)
        : base(localizer, "Owner", "Owner")
    {
        _clipboard = clipboard;
        UserCode = setup.GenerateUserCode();
    }

    [ObservableProperty]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FullNameError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmailError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PasswordError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPasswordError { get; set; } = string.Empty;

    /// <summary>Generated once when the wizard opens, shown and copyable.</summary>
    public string UserCode { get; }

    [ObservableProperty]
    public partial bool IsCodeCopied { get; set; }

    public string CopyText => Localizer[IsCodeCopied ? "Common_Copied" : "Common_Copy"];

    protected override bool Evaluate()
    {
        var name = SetupRules.ValidateRequired(FullName);
        var email = SetupRules.ValidateOwnerEmail(Email);
        var password = SetupRules.ValidatePassword(Password);
        var confirm = SetupRules.ValidatePasswordConfirmation(Password, ConfirmPassword);

        FullNameError = Describe(name);
        EmailError = Describe(email);
        PasswordError = Describe(password);
        ConfirmPasswordError = Describe(confirm);

        return name == ValidationCode.None && email == ValidationCode.None
            && password == ValidationCode.None && confirm == ValidationCode.None;
    }

    public override void RefreshStrings()
    {
        base.RefreshStrings();
        Revalidate();
    }

    [RelayCommand]
    private async Task CopyCodeAsync()
    {
        _clipboard.SetText(UserCode);
        IsCodeCopied = true;
        await Task.Delay(TimeSpan.FromSeconds(2));
        IsCodeCopied = false;
    }

    /// <summary>Drops the password from memory once setup has used it.</summary>
    public void ClearSecrets()
    {
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    partial void OnFullNameChanged(string value) => Revalidate();

    partial void OnEmailChanged(string value) => Revalidate();

    partial void OnPasswordChanged(string value) => Revalidate();

    partial void OnConfirmPasswordChanged(string value) => Revalidate();

    partial void OnIsCodeCopiedChanged(bool value) => OnPropertyChanged(nameof(CopyText));
}
