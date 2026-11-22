namespace Estoq.Core.Setup;

/// <summary>Whether entered prices already include sales tax. Default is excluded (decision 8).</summary>
public enum TaxMode
{
    /// <summary>Prices are before tax; tax is added at the till.</summary>
    Excluded,

    /// <summary>Prices already contain tax.</summary>
    Included,
}
