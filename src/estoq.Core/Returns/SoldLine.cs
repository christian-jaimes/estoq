namespace Estoq.Core.Returns;

/// <summary>
/// One line of a settled sale as the return flow sees it. The price is the unit price in the base currency
/// at the time of the sale; <see cref="QuantityReturned"/> counts units already taken back.
/// </summary>
public sealed record SoldLine(string VariantId, string Name, string Sku, string Detail, decimal UnitPrice, int QuantitySold, int QuantityReturned)
{
    /// <summary>Units that can still come back.</summary>
    public int Returnable => ReturnRules.ReturnableQuantity(QuantitySold, QuantityReturned);
}

/// <summary>Units of one variant being taken back, at the unit price they were sold for.</summary>
public sealed record ReturnedLine(string VariantId, string Name, string Sku, decimal UnitPrice, int Quantity);
