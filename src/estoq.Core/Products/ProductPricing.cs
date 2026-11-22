namespace Estoq.Core.Products;

/// <summary>Estimated margin of a product: what is left of the selling price after cost.</summary>
/// <param name="Amount">Selling price minus cost, in the same currency.</param>
/// <param name="Percent">The amount as a percentage of the selling price; null when there is no selling price.</param>
public sealed record Margin(decimal Amount, decimal? Percent);

public static class ProductPricing
{
    /// <summary>Step 8. Cost 8.00 and selling price 12.00 gives 4.00 and 33.33 %. Rounded to two decimals, away from zero.</summary>
    public static Margin Estimate(decimal cost, decimal sellingPrice)
    {
        var amount = decimal.Round(sellingPrice - cost, 2, MidpointRounding.AwayFromZero);
        decimal? percent = sellingPrice > 0
            ? decimal.Round((sellingPrice - cost) / sellingPrice * 100m, 2, MidpointRounding.AwayFromZero)
            : null;
        return new Margin(amount, percent);
    }
}
