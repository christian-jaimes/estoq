using Estoq.Core.SignIn;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Estoq.App.Services;

/// <summary>Security settings the owner will change in Settings. In memory until the back end stores them.</summary>
public interface ISecuritySettings
{
    /// <summary>Idle time after which the user is logged out. Always within the allowed range.</summary>
    TimeSpan InactivityTimeout { get; set; }
}

public sealed class SecuritySettings : ISecuritySettings
{
    private TimeSpan _timeout = InactivityPolicy.DefaultTimeout;

    public TimeSpan InactivityTimeout
    {
        get => _timeout;
        set => _timeout = InactivityPolicy.Clamp(value);
    }
}

/// <summary>Logs the user out once there has been no pointer or keyboard input for the inactivity timeout.</summary>
public interface IInactivityMonitor
{
    /// <summary>Starts watching input on <paramref name="root"/>. Called once by the main window.</summary>
    void Attach(UIElement root, DispatcherQueue dispatcher);
}

public sealed class InactivityMonitor : IInactivityMonitor
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private readonly ISessionService _session;
    private readonly ISecuritySettings _settings;
    private DateTimeOffset _lastActivity = DateTimeOffset.UtcNow;
    private DateTimeOffset _lastPointer;
    private DispatcherQueueTimer? _timer; // referenced so it is not collected

    public InactivityMonitor(ISessionService session, ISecuritySettings settings)
    {
        _session = session;
        _settings = settings;
        _session.SignedIn += (_, _) => Touch();
    }

    public void Attach(UIElement root, DispatcherQueue dispatcher)
    {
        // handledEventsToo: controls mark most input handled, and that is still activity.
        root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) => Touch()), true);
        root.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler((_, _) => Touch()), true);
        root.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, _) => Touch()), true);
        root.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnPointerMoved), true);

        var timer = _timer = dispatcher.CreateTimer();
        timer.Interval = CheckInterval;
        timer.IsRepeating = true;
        timer.Tick += (_, _) => Check();
        timer.Start();
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        // Moves fire many times a second; once a second is enough.
        var now = DateTimeOffset.UtcNow;
        if (now - _lastPointer >= CheckInterval)
        {
            _lastPointer = now;
            Touch();
        }
    }

    private void Touch() => _lastActivity = DateTimeOffset.UtcNow;

    private void Check()
    {
        if (_session.IsSignedIn && InactivityPolicy.HasExpired(_lastActivity, DateTimeOffset.UtcNow, _settings.InactivityTimeout))
        {
            _session.SignOut(SignOutReason.Inactivity);
        }
    }
}
