namespace Estoq.Core.SignIn;

/// <summary>
/// Automatic log out after inactivity. The timeout is a setting: the owner will change it in Settings
/// (docs/features/settings.md, Defaults and rules); until then the default applies.
/// </summary>
public static class InactivityPolicy
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    public static readonly TimeSpan MinTimeout = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(60);

    public static bool IsValidTimeout(TimeSpan timeout) => timeout >= MinTimeout && timeout <= MaxTimeout;

    /// <summary>The timeout limited to the allowed range.</summary>
    public static TimeSpan Clamp(TimeSpan timeout) =>
        timeout < MinTimeout ? MinTimeout : timeout > MaxTimeout ? MaxTimeout : timeout;

    /// <summary>True once the time since the last activity reaches the timeout.</summary>
    public static bool HasExpired(DateTimeOffset lastActivity, DateTimeOffset now, TimeSpan timeout) =>
        now - lastActivity >= timeout;
}
