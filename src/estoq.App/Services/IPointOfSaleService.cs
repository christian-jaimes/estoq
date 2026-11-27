using Estoq.Core.Roles;
using Estoq.Core.Sales;
using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>A sellable variant as the till sees it. The price is in the base currency.</summary>
public sealed record SaleVariant(string VariantId, string Name, string Sku, string Barcode, decimal UnitPrice, int OnHand);

/// <summary>A store-configured percentage discount and whether the owner has activated it.</summary>
public sealed record DiscountPreset(decimal Percent, bool IsActive);

/// <summary>The store settings the till reads: currencies and rates, tax, discount cap (docs/features/point-of-sale.md).</summary>
public sealed record SaleSettings(
    ExchangeRates Rates,
    IReadOnlyList<string> Currencies,
    TaxRate TaxRate,
    TaxMode TaxMode,
    decimal DiscountCapPercent);

/// <summary>Everything a completed sale saves. Rates are snapshotted so the sale never changes when rates do.</summary>
public sealed record SaleRequest(
    IReadOnlyList<CartLine> Lines,
    decimal DiscountPercent,
    SaleTotals Totals,
    IReadOnlyList<PaymentLine> Payments,
    ExchangeRates Rates);

/// <summary>What the till shows after a sale is saved.</summary>
public sealed record SaleReceipt(string SaleNumber, SaleTotals Totals, decimal Change);

/// <summary>
/// Catalog, settings and saving for the point of sale. The sample implementation keeps everything in memory;
/// the back end replaces it with one that saves the sale, items, payments and stock movements in one transaction.
/// </summary>
public interface IPointOfSaleService
{
    /// <summary>The signed-in user's role. Sample value (owner) until sign-in is merged.</summary>
    UserRole CurrentRole { get; }

    SaleSettings GetSettings();

    /// <summary>Configured discount presets; the POS offers only active presets.</summary>
    IReadOnlyList<DiscountPreset> GetDiscountPresets();

    /// <summary>Completed payment counts by method, used to put the store's most common methods first.</summary>
    IReadOnlyDictionary<PaymentMethod, int> GetPaymentMethodUsage();

    /// <summary>The variant whose internal or supplier barcode, or SKU, equals <paramref name="code"/>.</summary>
    SaleVariant? FindByCode(string code);

    /// <summary>Variants whose name or SKU contains <paramref name="text"/>, best matches first.</summary>
    IReadOnlyList<SaleVariant> Search(string text, int maxResults);

    /// <summary>True when <paramref name="userCode"/> belongs to an active manager or owner (discount approval).</summary>
    bool ApproveAsManager(string userCode);

    SaleReceipt Complete(SaleRequest request);

    /// <summary>Moves a variant's on-hand count by <paramref name="delta"/> after stock movements are written elsewhere (Returns).</summary>
    void ApplyStockChange(string variantId, int delta);
}
