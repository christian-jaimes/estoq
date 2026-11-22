using Estoq.Core.Setup;

namespace Estoq.Core.SignIn;

/// <summary>What the sign-in input currently holds. It decides which fields the screen shows.</summary>
public enum IdentifierKind
{
    /// <summary>Nothing typed yet: the screen waits for a code.</summary>
    Empty,

    /// <summary>Digits only: a user code.</summary>
    Code,

    /// <summary>Anything with a non-digit: an email, so the password field appears.</summary>
    Email,
}

/// <summary>The result of a sign-in attempt. Failed never says whether the code or email exists.</summary>
public enum SignInOutcome
{
    Success,
    Failed,
    LockedOut,
}

/// <summary>Input rules of the sign-in screen (docs/features/sign-in.md).</summary>
public static class SignInRules
{
    public static IdentifierKind Classify(string? input)
    {
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return IdentifierKind.Empty;
        }

        return text.All(char.IsAsciiDigit) ? IdentifierKind.Code : IdentifierKind.Email;
    }

    /// <summary>Digit-only input is capped at the code length; anything else is kept as typed.</summary>
    public static string Normalize(string? input)
    {
        var text = input?.Trim() ?? string.Empty;
        return Classify(text) == IdentifierKind.Code && text.Length > UserCode.Length ? text[..UserCode.Length] : text;
    }

    /// <summary>True when the input is a complete code, which signs in without a password.</summary>
    public static bool IsCompleteCode(string? input) => Classify(input) == IdentifierKind.Code && UserCode.IsValid(input?.Trim());

    /// <summary>True when the input can be submitted: a full code, or an email with a password.</summary>
    public static bool CanSubmit(string? identifier, string? password) =>
        Classify(identifier) switch
        {
            IdentifierKind.Code => IsCompleteCode(identifier),
            IdentifierKind.Email => !string.IsNullOrEmpty(password),
            _ => false,
        };
}
