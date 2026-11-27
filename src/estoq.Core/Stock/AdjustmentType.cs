namespace Estoq.Core.Stock;

/// <summary>The fixed adjustment types of docs/features/stock-adjustments.md. One adjustment has one type.</summary>
public enum AdjustmentType
{
    /// <summary>The system count differs from the shelf.</summary>
    Recount,

    /// <summary>Correction after an out-of-stock sale.</summary>
    SoldWithoutStock,

    /// <summary>Damaged, unsellable.</summary>
    Damaged,

    /// <summary>Lost or stolen.</summary>
    Lost,
}
