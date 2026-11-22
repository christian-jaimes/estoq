using System.Globalization;
using System.Security.Cryptography;

namespace Estoq.Core.Setup;

/// <summary>The six-digit code a person signs in with. Uniqueness across users is checked where users are stored.</summary>
public static class UserCode
{
    public const int Length = 6;

    private const int Limit = 1_000_000;

    /// <summary>A random code, leading zeros kept (for example 004821).</summary>
    public static string Generate() => Generate(RandomNumberGenerator.GetInt32);

    /// <summary>Generates with a supplied source: it receives the exclusive upper bound and returns 0 up to that bound.</summary>
    public static string Generate(Func<int, int> nextBelow)
    {
        ArgumentNullException.ThrowIfNull(nextBelow);
        var value = nextBelow(Limit);
        if (value is < 0 or >= Limit)
        {
            throw new InvalidOperationException("The random source returned a value outside the range.");
        }

        return value.ToString("D6", CultureInfo.InvariantCulture);
    }

    public static bool IsValid(string? code) =>
        code is { Length: Length } && code.All(char.IsAsciiDigit);
}
