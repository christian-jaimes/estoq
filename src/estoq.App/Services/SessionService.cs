namespace Estoq.App.Services;

/// <summary>The signed-in user. Sample implementation until sign-in and the back end exist (docs/features/sign-in.md).</summary>
public interface ISessionService
{
    /// <summary>Full name of the signed-in user, or null when nobody is signed in.</summary>
    string? UserName { get; }

    /// <summary>Signs the user in. Setup does this for the owner, who is signed in automatically.</summary>
    void SignIn(string userName);
}

public sealed class SessionService : ISessionService
{
    public string? UserName { get; private set; }

    public void SignIn(string userName) => UserName = userName.Trim();
}
