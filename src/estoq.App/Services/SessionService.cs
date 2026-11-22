using Estoq.Core.Roles;

namespace Estoq.App.Services;

/// <summary>The signed-in person: full name and role.</summary>
public sealed record SessionUser(string Name, UserRole Role);

public enum SignOutReason
{
    /// <summary>The user chose Log out or pressed F12.</summary>
    UserRequested,

    /// <summary>The inactivity timeout ran out.</summary>
    Inactivity,
}

/// <summary>The signed-in user. Sample implementation until the back end exists (docs/features/sign-in.md).</summary>
public interface ISessionService
{
    /// <summary>The signed-in user, or null when nobody is signed in.</summary>
    SessionUser? CurrentUser { get; }

    bool IsSignedIn { get; }

    /// <summary>Full name of the signed-in user, or null when nobody is signed in.</summary>
    string? UserName { get; }

    /// <summary>Role of the signed-in user; Cashier (least access) when nobody is signed in.</summary>
    UserRole Role { get; }

    /// <summary>Why the last session ended; shown once on the sign-in screen, then cleared with <see cref="ClearSignOutReason"/>.</summary>
    SignOutReason? LastSignOutReason { get; }

    event EventHandler? SignedIn;

    event EventHandler? SignedOut;

    /// <summary>Starts a session. Setup does this for the owner, who is signed in automatically.</summary>
    void SignIn(SessionUser user);

    /// <summary>Ends the session. Does nothing when nobody is signed in.</summary>
    void SignOut(SignOutReason reason);

    void ClearSignOutReason();
}

public sealed class SessionService : ISessionService
{
    public SessionUser? CurrentUser { get; private set; }

    public bool IsSignedIn => CurrentUser is not null;

    public string? UserName => CurrentUser?.Name;

    public UserRole Role => CurrentUser?.Role ?? UserRole.Cashier;

    public SignOutReason? LastSignOutReason { get; private set; }

    public event EventHandler? SignedIn;

    public event EventHandler? SignedOut;

    public void SignIn(SessionUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        CurrentUser = user with { Name = user.Name.Trim() };
        LastSignOutReason = null;
        SignedIn?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut(SignOutReason reason)
    {
        if (CurrentUser is null)
        {
            return;
        }

        CurrentUser = null;
        LastSignOutReason = reason;
        SignedOut?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSignOutReason() => LastSignOutReason = null;
}
