using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Roles;
using Estoq.Core.Settings;
using Estoq.Core.Setup;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.Settings;

/// <summary>
/// The Account section (mockup 9): photo, role, name, email, sign-in code, password, language, theme and Log out.
/// Edits stay local until <see cref="Apply"/>; language and theme take effect then.
/// </summary>
public sealed partial class AccountSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ISessionService _session;
    private readonly ILocalizer _localizer;
    private readonly IThemeService _theme;
    private readonly ILogoPickerService _picker;
    private readonly IClipboardService _clipboard;
    private AccountProfile _saved = new(string.Empty, string.Empty, string.Empty, UserRole.Cashier, null);
    private int _savedLanguage;
    private int _savedTheme;
    private bool _loading;

    public AccountSettingsViewModel(ISettingsService settings, ISessionService session, ILocalizer localizer, IThemeService theme,
        ILogoPickerService picker, IClipboardService clipboard)
    {
        _settings = settings;
        _session = session;
        _localizer = localizer;
        _theme = theme;
        _picker = picker;
        _clipboard = clipboard;
    }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NameError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmailError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RoleText { get; set; } = string.Empty;

    /// <summary>True for the owner: the email is then required.</summary>
    [ObservableProperty]
    public partial bool IsEmailRequired { get; set; }

    public bool IsEmailOptional => !IsEmailRequired;

    [ObservableProperty]
    public partial string Initials { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? PhotoPath { get; set; }

    [ObservableProperty]
    public partial ImageSource? PhotoPreview { get; set; }

    [ObservableProperty]
    public partial string PhotoError { get; set; } = string.Empty;

    public bool HasPhoto => PhotoPath is not null;

    public bool HasNoPhoto => PhotoPath is null;

    // Sign-in code: masked until the person reveals it.
    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsCodeVisible { get; set; }

    public string CodeDisplay => IsCodeVisible ? Code : new string('•', Code.Length);

    /// <summary>0 English, 1 Spanish.</summary>
    [ObservableProperty]
    public partial int LanguageIndex { get; set; }

    /// <summary>0 System, 1 Light, 2 Dark (same order as <see cref="ThemePreference"/>).</summary>
    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    // Change password: a small inline form that applies at once and is audited.
    [ObservableProperty]
    public partial bool IsChangingPassword { get; set; }

    [ObservableProperty]
    public partial string NewPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PasswordError { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PasswordNotice { get; set; } = string.Empty;

    public bool IsDirty =>
        Name.Trim() != _saved.Name || Email.Trim() != _saved.Email || PhotoPath != _saved.PhotoPath
        || LanguageIndex != _savedLanguage || ThemeIndex != _savedTheme;

    /// <summary>Loads the signed-in person's details and drops unsaved edits.</summary>
    public void Reset()
    {
        _loading = true;
        _saved = _settings.GetAccount();
        _savedLanguage = _localizer.Language == AppLanguage.Spanish ? 1 : 0;
        _savedTheme = (int)_theme.Preference;
        Name = _saved.Name;
        Email = _saved.Email;
        Code = _saved.Code;
        PhotoPath = _saved.PhotoPath;
        PhotoPreview = null;
        PhotoError = string.Empty;
        LanguageIndex = _savedLanguage;
        ThemeIndex = _savedTheme;
        IsCodeVisible = false;
        IsEmailRequired = _saved.Role == UserRole.Owner;
        NameError = EmailError = PasswordError = PasswordNotice = string.Empty;
        IsChangingPassword = false;
        NewPassword = ConfirmPassword = string.Empty;
        _loading = false;
        RefreshStrings();
        OnPropertyChanged(nameof(IsDirty));
        _ = LoadPreviewAsync();
    }

    public void RefreshStrings()
    {
        RoleText = _localizer[$"Role_{_saved.Role}"];
        Initials = MakeInitials(Name);
    }

    /// <summary>Checks the fields; shows errors and returns false when something is wrong.</summary>
    public bool Validate()
    {
        var name = AccountRules.ValidateName(Name);
        var email = AccountRules.ValidateEmail(_saved.Role, Email);
        NameError = Describe(name);
        EmailError = Describe(email);
        return name == ValidationCode.None && email == ValidationCode.None;
    }

    /// <summary>Saves the profile; applies the theme and returns the language to switch to (done after the pop-up closes).</summary>
    public AppLanguage? Apply()
    {
        var result = _settings.SaveAccount(_saved with { Name = Name, Email = Email, PhotoPath = PhotoPath });
        if (result != ValidationCode.None)
        {
            return null;
        }

        if (ThemeIndex != _savedTheme)
        {
            _theme.Apply((ThemePreference)ThemeIndex);
        }

        return LanguageIndex != _savedLanguage ? (LanguageIndex == 1 ? AppLanguage.Spanish : AppLanguage.English) : null;
    }

    [RelayCommand]
    private async Task ChangePhotoAsync()
    {
        var picked = await _picker.PickAsync();
        if (picked is null)
        {
            return;
        }

        var problem = SetupRules.ValidateLogo(picked.FileName, picked.SizeInBytes);
        if (problem != ValidationCode.None)
        {
            PhotoError = _localizer[$"Error_{problem}"];
            return;
        }

        PhotoError = string.Empty;
        PhotoPreview = await _picker.LoadPreviewAsync(picked.Path);
        PhotoPath = picked.Path;
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        PhotoPath = null;
        PhotoPreview = null;
        PhotoError = string.Empty;
    }

    [RelayCommand]
    private void ToggleCode() => IsCodeVisible = !IsCodeVisible;

    [RelayCommand]
    private void CopyCode() => _clipboard.SetText(Code, isSensitive: true);

    [RelayCommand]
    private void TogglePassword()
    {
        IsChangingPassword = !IsChangingPassword;
        NewPassword = ConfirmPassword = string.Empty;
        PasswordError = PasswordNotice = string.Empty;
    }

    [RelayCommand]
    private void SavePassword()
    {
        var result = _settings.ChangePassword(NewPassword, ConfirmPassword);
        if (result != ValidationCode.None)
        {
            PasswordError = result == ValidationCode.PasswordTooShort
                ? _localizer.Format("Error_PasswordTooShort", SetupRules.MinPasswordLength)
                : Describe(result);
            return;
        }

        IsChangingPassword = false;
        NewPassword = ConfirmPassword = string.Empty;
        PasswordError = string.Empty;
        PasswordNotice = _localizer["Settings_PasswordChanged"];
    }

    [RelayCommand]
    private void LogOut() => _session.SignOut(SignOutReason.UserRequested);

    partial void OnNameChanged(string value)
    {
        Initials = MakeInitials(value);
        if (!_loading)
        {
            NameError = string.Empty;
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    partial void OnEmailChanged(string value)
    {
        if (!_loading)
        {
            EmailError = string.Empty;
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    partial void OnPhotoPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPhoto));
        OnPropertyChanged(nameof(HasNoPhoto));
        OnPropertyChanged(nameof(IsDirty));
    }

    partial void OnLanguageIndexChanged(int value) => OnPropertyChanged(nameof(IsDirty));

    partial void OnThemeIndexChanged(int value) => OnPropertyChanged(nameof(IsDirty));

    partial void OnIsCodeVisibleChanged(bool value) => OnPropertyChanged(nameof(CodeDisplay));

    partial void OnCodeChanged(string value) => OnPropertyChanged(nameof(CodeDisplay));

    partial void OnIsEmailRequiredChanged(bool value) => OnPropertyChanged(nameof(IsEmailOptional));

    private async Task LoadPreviewAsync()
    {
        if (PhotoPath is { } path)
        {
            PhotoPreview = await _picker.LoadPreviewAsync(path);
        }
    }

    private string Describe(ValidationCode code) => code == ValidationCode.None ? string.Empty : _localizer[$"Error_{code}"];

    internal static string MakeInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => string.Empty,
            1 => parts[0][..1].ToUpperInvariant(),
            _ => string.Concat(parts[0][..1], parts[^1][..1]).ToUpperInvariant(),
        };
    }
}
