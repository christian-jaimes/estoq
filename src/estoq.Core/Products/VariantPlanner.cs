namespace Estoq.Core.Products;

/// <summary>One size and colour combination. Either part is null for a product without sizes or without colours.</summary>
public sealed record VariantSlot(string? Size, string? Colour);

/// <summary>Step 6: only ticked combinations become variants.</summary>
public static class VariantPlanner
{
    /// <summary>
    /// Every colour with every size, colours first (White S, White M, Black S, ...). Nothing ticked on one side
    /// leaves that part out; nothing ticked on either side gives the single plain variant every product needs.
    /// </summary>
    public static IReadOnlyList<VariantSlot> Plan(IEnumerable<string> sizes, IEnumerable<string> colours)
    {
        ArgumentNullException.ThrowIfNull(sizes);
        ArgumentNullException.ThrowIfNull(colours);

        var sizeList = Distinct(sizes);
        var colourList = Distinct(colours);
        if (sizeList.Count == 0 && colourList.Count == 0)
        {
            return [new VariantSlot(null, null)];
        }

        IReadOnlyList<string?> sizeOptions = sizeList.Count == 0 ? [null] : sizeList;
        IReadOnlyList<string?> colourOptions = colourList.Count == 0 ? [null] : colourList;
        var slots = new List<VariantSlot>(sizeOptions.Count * colourOptions.Count);
        foreach (var colour in colourOptions)
        {
            foreach (var size in sizeOptions)
            {
                slots.Add(new VariantSlot(size, colour));
            }
        }

        return slots;
    }

    private static List<string> Distinct(IEnumerable<string> values)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var value in values)
        {
            var clean = ProductNaming.Clean(value);
            if (clean.Length > 0 && seen.Add(ProductNaming.Key(clean)))
            {
                result.Add(clean);
            }
        }

        return result;
    }
}
