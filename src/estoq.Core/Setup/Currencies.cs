namespace Estoq.Core.Setup;

/// <summary>
/// Currency rules for setup. The base and default currency is chosen by the owner (USD unless changed); other
/// currencies can be accepted at the till, each with its own exchange rate later (decision 13, 32).
/// </summary>
public static class Currencies
{
    /// <summary>The base and default currency until the owner picks another.</summary>
    public const string Default = "USD";

    /// <summary>An ISO 4217 style code: three capital letters.</summary>
    public static bool IsValidCode(string? code) =>
        code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);

    /// <summary>
    /// The accepted currencies to store: valid codes only, upper case, no duplicates, never the base currency
    /// (the base is always accepted), in the order given.
    /// </summary>
    public static IReadOnlyList<string> NormalizeAccepted(string baseCode, IEnumerable<string> accepted)
    {
        ArgumentNullException.ThrowIfNull(accepted);
        var result = new List<string>();
        foreach (var raw in accepted)
        {
            var code = raw?.Trim().ToUpperInvariant();
            if (IsValidCode(code) && code != baseCode && !result.Contains(code!))
            {
                result.Add(code!);
            }
        }

        return result;
    }
}
