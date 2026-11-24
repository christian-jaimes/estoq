using System.Globalization;

namespace Estoq.App.Services;

/// <summary>How amounts and currency chips are written at the till. Numbers follow the app language.</summary>
public static class CurrencyFormat
{
    private static readonly Dictionary<string, string> Symbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "$",
        ["EUR"] = "€",
        ["VES"] = "Bs.",
        ["COP"] = "$",
        ["GBP"] = "£",
        ["MXN"] = "$",
        ["BRL"] = "R$",
    };

    /// <summary>The short symbol, or the code itself when no symbol is known.</summary>
    public static string Symbol(string currency) => Symbols.TryGetValue(currency, out var symbol) ? symbol : currency;

    /// <summary>A chip label such as "$ USD" or "Bs. VES".</summary>
    public static string ChipLabel(string currency)
    {
        var symbol = Symbol(currency);
        return symbol == currency ? currency : $"{symbol} {currency}";
    }

    /// <summary>The symbol followed by the amount with two decimals, for example "$83.52".</summary>
    public static string Amount(decimal amount, string currency, CultureInfo culture)
    {
        var symbol = Symbol(currency);
        var text = Math.Abs(amount).ToString("N2", culture);
        var separator = symbol == currency ? " " : string.Empty;
        return amount < 0 ? $"-{symbol}{separator}{text}" : $"{symbol}{separator}{text}";
    }

    /// <summary>Parses an amount typed by the user using the app culture; false for empty or malformed text.</summary>
    public static bool TryParseAmount(string? text, CultureInfo culture, out decimal amount)
    {
        amount = 0m;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        return decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, culture, out amount)
            && amount >= 0m;
    }
}
