using Estoq.Core.Sales;
using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>
/// The sample store's tax, in one place until the Settings screen and the back end store the owner's choice from the setup wizard.
/// The till, the seeded sales and the sales rebuilt from the stock movements all read it, so a sale is taxed the same way everywhere.
/// <see cref="TaxMode.Included"/> means the listed prices already contain the tax (the total equals the subtotal);
/// <see cref="TaxMode.Excluded"/> adds the tax on top.
/// </summary>
internal static class SampleStoreTax
{
    /// <summary>The sample store charges 16 % to exercise the tax rows; the setup default is 0 %.</summary>
    public static TaxRate Rate => TaxRate.TryCreate(16m, out var rate) ? rate : TaxRate.Default;

    public static TaxMode Mode => TaxMode.Included;
}
