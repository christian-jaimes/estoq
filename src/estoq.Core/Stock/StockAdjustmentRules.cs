namespace Estoq.Core.Stock;

/// <summary>One item of an adjustment: stock now and the counted quantity (the new stock).</summary>
public readonly record struct AdjustmentLine(int Current, int Counted)
{
    /// <summary>Counted minus current: positive is an increase, negative a decrease.</summary>
    public int Difference => Counted - Current;
}

/// <summary>Increases, decreases (both as positive units) and the net change of an adjustment.</summary>
public readonly record struct AdjustmentSummary(int Increases, int Decreases)
{
    public int Net => Increases - Decreases;
}

/// <summary>Why an adjustment cannot be applied yet. <see cref="None"/> means it can.</summary>
public enum AdjustmentValidation
{
    None,
    NoItems,
    NothingChanged,
}

/// <summary>Business rules of the Stock adjustments pop-up (F3). Pure code: no database, no UI.</summary>
public static class StockAdjustmentRules
{
    /// <summary>The largest stock quantity the pop-up accepts.</summary>
    public const int MaxQuantity = 1_000_000;

    /// <summary>The movement type recorded in the stock movements table for an adjustment type.</summary>
    public static string MovementType(AdjustmentType type) => type switch
    {
        AdjustmentType.Recount => "recount",
        AdjustmentType.SoldWithoutStock => "correction",
        AdjustmentType.Damaged => "defect",
        AdjustmentType.Lost => "lost",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>The reasons offered for a type. Each type has one fixed reason (the localisation key suffix).</summary>
    public static IReadOnlyList<string> Reasons(AdjustmentType type) => type switch
    {
        AdjustmentType.Recount => ["StockCountDifference"],
        AdjustmentType.SoldWithoutStock => ["OutOfStockSaleCorrection"],
        AdjustmentType.Damaged => ["DamagedUnsellable"],
        AdjustmentType.Lost => ["LostOrStolen"],
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>Damaged and lost items can only leave stock, never enter it.</summary>
    public static bool AllowsIncrease(AdjustmentType type) => type is AdjustmentType.Recount or AdjustmentType.SoldWithoutStock;

    /// <summary>The counted quantity kept inside what the type allows: never below zero, never above stock for decreases-only types.</summary>
    public static int ClampCounted(AdjustmentType? type, int current, int counted)
    {
        var upper = type is { } t && !AllowsIncrease(t) ? Math.Max(0, current) : MaxQuantity;
        return Math.Clamp(counted, 0, upper);
    }

    /// <summary>The counted quantity after changing the difference by <paramref name="step"/> units.</summary>
    public static int Step(AdjustmentType? type, int current, int counted, int step) => ClampCounted(type, current, counted + step);

    public static AdjustmentSummary Summarize(IEnumerable<AdjustmentLine> lines)
    {
        var increases = 0;
        var decreases = 0;
        foreach (var line in lines)
        {
            if (line.Difference > 0)
            {
                increases += line.Difference;
            }
            else
            {
                decreases -= line.Difference;
            }
        }

        return new AdjustmentSummary(increases, decreases);
    }

    /// <summary>An adjustment needs at least one item with a difference. Lines with no difference write nothing.</summary>
    public static AdjustmentValidation Validate(IReadOnlyCollection<AdjustmentLine> lines)
    {
        if (lines.Count == 0)
        {
            return AdjustmentValidation.NoItems;
        }

        return lines.Any(line => line.Difference != 0) ? AdjustmentValidation.None : AdjustmentValidation.NothingChanged;
    }
}
