using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.SignIn;
using Microsoft.UI.Dispatching;

namespace Estoq.App.ViewModels;

/// <summary>
/// Sign-in screen (docs/features/sign-in.md). A six-digit code signs in directly; typing a letter switches to email
/// and password. Failures are generic. After five wrong attempts the screen locks and counts down.
/// </summary>
public sealed partial class SignInViewModel : ObservableObject
{
    private readonly IAccountService _accounts;
    private readonly ISessionService _session;
    private readonly ILocalizer _localizer;
    private DispatcherQueueTimer? _lockTimer;
    private bool _isNormalizing;

    public SignInViewModel(IAccountService accounts, ISessionService session, ILocalizer localizer)
    {
        _accounts = accounts;
        _session = session;
        _localizer = localizer;

        if (_session.LastSignOutReason == SignOutReason.Inactivity)
        {
            ShowNotice(_localizer["SignIn_InactivityNotice"]);
        }

        _session.ClearSignOutReason();
        ResumeLockIfAny();
    }

    /// <summary>Raised after a successful sign-in; the page then opens the shell.</summary>
    public event EventHandler? SignedIn;

    /// <summary>Requests field feedback before the failed credentials are cleared.</summary>
    public event Action<bool>? FailureAnimationRequested;

    [ObservableProperty]
    public partial string Identifier { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPasswordRevealed { get; set; }

    /// <summary>Calm message under the fields: a failure, a lock countdown or the inactivity notice.</summary>
    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMessageError { get; set; }

    [ObservableProperty]
    public partial bool IsLocked { get; set; }

    public bool IsEmailMode => SignInRules.Classify(Identifier) == IdentifierKind.Email;

    public bool IsCodeMode => !IsEmailMode;

    public string Subtitle => _localizer[IsEmailMode ? "SignIn_SubtitlePassword" : "SignIn_SubtitleCode"];

    public bool HasMessage => !string.IsNullOrEmpty(Message);

    /// <summary>Eye icon: shows the password, or crosses it out while it is visible.</summary>
    public string RevealGlyph => IsPasswordRevealed ? "" : "";

    public string RevealToolTip => _localizer[IsPasswordRevealed ? "SignIn_HidePassword" : "SignIn_ShowPassword"];

    public bool IsNotLocked => !IsLocked;

    partial void OnIdentifierChanged(string value)
    {
        if (_isNormalizing)
        {
            return;
        }

        // Digits are capped at six; anything else is kept as typed.
        var normalized = SignInRules.Normalize(value);
        if (normalized != value)
        {
            _isNormalizing = true;
            Identifier = normalized;
            _isNormalizing = false;
        }

        OnPropertyChanged(nameof(IsEmailMode));
        OnPropertyChanged(nameof(IsCodeMode));
        OnPropertyChanged(nameof(Subtitle));
        if (!IsLocked)
        {
            ClearMessage();
        }

        if (!IsEmailMode)
        {
            Password = string.Empty;
        }

        // A complete code signs in on its own.
        if (SignInRules.IsCompleteCode(Identifier))
        {
            Submit();
        }
    }

    partial void OnMessageChanged(string value) => OnPropertyChanged(nameof(HasMessage));

    partial void OnIsLockedChanged(bool value) => OnPropertyChanged(nameof(IsNotLocked));

    partial void OnIsPasswordRevealedChanged(bool value)
    {
        OnPropertyChanged(nameof(RevealGlyph));
        OnPropertyChanged(nameof(RevealToolTip));
    }

    [RelayCommand]
    private void TogglePasswordReveal() => IsPasswordRevealed = !IsPasswordRevealed;

    [RelayCommand]
    private void Submit()
    {
        if (IsLocked)
        {
            return;
        }

        if (!SignInRules.CanSubmit(Identifier, Password))
        {
            ShowError(_localizer["SignIn_Incomplete"]);
            return;
        }

        var result = _accounts.SignIn(Identifier, IsEmailMode ? Password : null);
        switch (result.Outcome)
        {
            case SignInOutcome.Success when result.User is { } user:
                ClearMessage();
                Password = string.Empty;
                _session.SignIn(user);
                SignedIn?.Invoke(this, EventArgs.Empty);
                break;
            case SignInOutcome.LockedOut:
                FailureAnimationRequested?.Invoke(IsEmailMode);
                ResetInputAfterFailure();
                StartLock();
                break;
            default:
                FailureAnimationRequested?.Invoke(IsEmailMode);
                ResetInputAfterFailure();
                ShowError(_localizer["SignIn_Failed"]);
                break;
        }
    }

    /// <summary>A wrong password is cleared; a wrong code is cleared too, so it can be typed again straight away.</summary>
    private void ResetInputAfterFailure()
    {
        Password = string.Empty;
        if (IsCodeMode)
        {
            Identifier = string.Empty;
        }
    }

    /// <summary>Stops the countdown timer when the page goes away.</summary>
    public void Stop()
    {
        _lockTimer?.Stop();
        _lockTimer = null;
    }

    private void ResumeLockIfAny()
    {
        if (_accounts.LockRemaining > TimeSpan.Zero)
        {
            StartLock();
        }
    }

    private void StartLock()
    {
        IsLocked = true;
        UpdateLockMessage();
        _lockTimer ??= CreateTimer();
        _lockTimer?.Start();
    }

    private DispatcherQueueTimer? CreateTimer()
    {
        var queue = DispatcherQueue.GetForCurrentThread();
        if (queue is null)
        {
            return null;
        }

        var timer = queue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.IsRepeating = true;
        timer.Tick += (_, _) => UpdateLockMessage();
        return timer;
    }

    private void UpdateLockMessage()
    {
        var remaining = _accounts.LockRemaining;
        if (remaining <= TimeSpan.Zero)
        {
            _lockTimer?.Stop();
            IsLocked = false;
            ClearMessage();
            return;
        }

        // Round up so the countdown never shows 0:00 while still locked.
        var shown = TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds));
        ShowError(_localizer.Format("SignIn_Locked", shown.ToString(@"m\:ss", _localizer.Culture)));
    }

    private void ShowError(string text)
    {
        IsMessageError = true;
        Message = text;
    }

    private void ShowNotice(string text)
    {
        IsMessageError = false;
        Message = text;
    }

    private void ClearMessage() => Message = string.Empty;
}
