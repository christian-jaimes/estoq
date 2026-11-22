using System.Globalization;

namespace Estoq.Core.Setup;

/// <summary>
/// The default sales tax rate as a percentage. Default 0 %; prices exclude tax (decisions 8 and 23).
/// Between 0 and 100 inclusive, at most two decimals.
/// </summary>
public readonly record struct TaxRate
{
    public const decimal MaxPercent = 100m;

    private TaxRate(decimal percent) => Percent = percent;

    /// <summary>The rate that applies until the owner sets one in the wizard.</summary>
    public static TaxRate Default => new(0m);

    /// <summary>The rate as a percentage, for example 16 for 16 %.</summary>
    public decimal Percent { get; }

    /// <summary>The rate as a multiplier, for example 0.16.</summary>
    public decimal Fraction => Percent / 100m;

    public static bool TryCreate(decimal percent, out TaxRate rate)
    {
        var valid = percent is >= 0m and <= MaxPercent && decimal.Round(percent, 2) == percent;
        rate = valid ? new TaxRate(percent) : default;
        return valid;
    }

    /// <summary>Parses text such as 16, 16,5 or 7.25 % using the given culture. Empty text is invalid.</summary>
    public static bool TryParse(string? text, IFormatProvider provider, out TaxRate rate)
    {
        rate = default;
        var trimmed = text?.Trim().TrimEnd('%').Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        return decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, provider, out var percent)
            && TryCreate(percent, out rate);
    }

    /// <summary>Validates tax rate text for the wizard.</summary>
    public static ValidationCode Validate(string? text, IFormatProvider provider) =>
        TryParse(text, provider, out _) ? ValidationCode.None : ValidationCode.InvalidTaxRate;
}
