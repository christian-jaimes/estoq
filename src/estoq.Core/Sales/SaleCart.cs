namespace Estoq.Core.Sales;

/// <summary>One variant in the cart. The price is the unit price in the base currency.</summary>
public sealed record CartLine(string VariantId, string Name, string Sku, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => Money.Round(UnitPrice * Quantity);
}

/// <summary>
/// The lines of the sale being rung up. Scanning a variant that is already in the cart raises its quantity.
/// Pure state, no database; the pop-up view model owns one.
/// </summary>
public sealed class SaleCart
{
    public const int MaxQuantity = 9999;

    private readonly List<CartLine> _lines = [];

    public IReadOnlyList<CartLine> Lines => _lines;

    public int ItemCount => _lines.Sum(line => line.Quantity);

    public bool IsEmpty => _lines.Count == 0;

    public decimal Subtotal => Money.Round(_lines.Sum(line => line.LineTotal));

    /// <summary>Adds one unit, or raises the quantity when the variant is already in the cart. Returns the line.</summary>
    public CartLine Add(string variantId, string name, string sku, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variantId);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        var index = IndexOf(variantId);
        if (index >= 0)
        {
            return Replace(index, _lines[index] with { Quantity = Math.Min(MaxQuantity, _lines[index].Quantity + 1) });
        }

        var line = new CartLine(variantId, name, sku, Money.Round(unitPrice), 1);
        _lines.Add(line);
        return line;
    }

    /// <summary>Sets a quantity from 1 to <see cref="MaxQuantity"/>; zero or less removes the line. Unknown variants are ignored.</summary>
    public void SetQuantity(string variantId, int quantity)
    {
        var index = IndexOf(variantId);
        if (index < 0)
        {
            return;
        }

        if (quantity <= 0)
        {
            _lines.RemoveAt(index);
            return;
        }

        Replace(index, _lines[index] with { Quantity = Math.Min(MaxQuantity, quantity) });
    }

    public void Remove(string variantId) => SetQuantity(variantId, 0);

    public void Clear() => _lines.Clear();

    private int IndexOf(string variantId) => _lines.FindIndex(line => line.VariantId == variantId);

    private CartLine Replace(int index, CartLine line)
    {
        _lines[index] = line;
        return line;
    }
}
