namespace Estoq.Core.Sales;

/// <summary>Rounding for amounts shown and stored at the till: two decimals, halves away from zero.</summary>
public static class Money
{
    public const int Decimals = 2;

    public static decimal Round(decimal amount) => decimal.Round(amount, Decimals, MidpointRounding.AwayFromZero);

    /// <summary>Rounds up to two decimals, so an amount offered to cover a balance is never short.</summary>
    public static decimal RoundUp(decimal amount) => Math.Ceiling(amount * 100m) / 100m;
}
