using System.Security.Cryptography;
using System.Text;
using Estoq.Core.Roles;
using Estoq.Core.SignIn;

namespace Estoq.App.Services;

/// <summary>Result of a sign-in attempt. <see cref="User"/> is set only on success; <see cref="RetryAfter"/> only when locked out.</summary>
public sealed record SignInResult(SignInOutcome Outcome, SessionUser? User = null, TimeSpan RetryAfter = default);

/// <summary>
/// Checks credentials. The sample implementation keeps accounts in memory; the back end replaces it with the encrypted
/// database and permission checks (docs/features/sign-in.md).
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Signs in with a six-digit code alone, or with an email and password. Wrong, unknown and inactive accounts all give
    /// the same <see cref="SignInOutcome.Failed"/>.
    /// </summary>
    SignInResult SignIn(string identifier, string? password);

    /// <summary>Time left before sign-in accepts attempts again; zero when it is open.</summary>
    TimeSpan LockRemaining { get; }

    /// <summary>Adds the owner created by the setup wizard.</summary>
    void RegisterOwner(string name, string code, string email, string password);
}

public sealed class SampleAccountService : IAccountService
{
    private readonly List<Account> _accounts =
    [
        new("Carlos Mendoza", "100200", "carlos@estoq.example", null, UserRole.Owner, AccountStatus.Active),
        new("Marta Rojas", "482913", null, null, UserRole.Manager, AccountStatus.Active),
        new("Luis Pérez", "570146", null, null, UserRole.Cashier, AccountStatus.Active),
        new("Sofía Díaz", "315208", null, null, UserRole.Cashier, AccountStatus.Inactive),
    ];

    private readonly SignInLockout _lockout = new();

    public TimeSpan LockRemaining => _lockout.Remaining;

    public void RegisterOwner(string name, string code, string email, string password) =>
        _accounts.Insert(0, new Account(name.Trim(), code, email.Trim(), password, UserRole.Owner, AccountStatus.Active));

    public SignInResult SignIn(string identifier, string? password)
    {
        if (_lockout.IsLocked)
        {
            return new SignInResult(SignInOutcome.LockedOut, RetryAfter: _lockout.Remaining);
        }

        var account = Find(identifier?.Trim() ?? string.Empty, password);
        if (account is null)
        {
            _lockout.RegisterFailure();
            return _lockout.IsLocked
                ? new SignInResult(SignInOutcome.LockedOut, RetryAfter: _lockout.Remaining)
                : new SignInResult(SignInOutcome.Failed);
        }

        _lockout.RegisterSuccess();
        return new SignInResult(SignInOutcome.Success, new SessionUser(account.Name, account.Role));
    }

    private Account? Find(string identifier, string? password)
    {
        Account? account = null;
        switch (SignInRules.Classify(identifier))
        {
            case IdentifierKind.Code:
                account = _accounts.FirstOrDefault(a => a.Code == identifier);
                break;
            case IdentifierKind.Email:
                account = _accounts.FirstOrDefault(a =>
                    a.Email is { Length: > 0 } && a.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase)
                    && PasswordMatches(a.Password, password));
                break;
        }

        return account is { Status: AccountStatus.Active } ? account : null;
    }

    private static bool PasswordMatches(string? expected, string? given)
    {
        if (expected is null || given is null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(given));
    }

    private sealed record Account(string Name, string Code, string? Email, string? Password, UserRole Role, AccountStatus Status);
}
