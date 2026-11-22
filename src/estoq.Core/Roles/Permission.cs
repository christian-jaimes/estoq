namespace Estoq.Core.Roles;

/// <summary>Capabilities in the matrix of docs/product/roles-and-permissions.md.</summary>
public enum Permission
{
    MakeSale,
    DiscountUpToCap,
    DiscountAboveCap,
    SellOutOfStockItem,
    AddProductOrReceiveStock,
    BrowseInventory,
    SeeCostsAndMargins,
    EditProductDetailsAndPrices,
    AddVariantsOrReprintLabels,
    ArchiveOrRestoreProducts,
    AdjustStock,
    ReturnOrExchange,
    VoidSale,
    ViewSalesHistory,
    ExportCsv,
    UpdateExchangeRates,
    SettingsAccount,
    SettingsStore,
    SettingsStaff,
    SettingsDefaultsAndRules,
    SettingsData,
    ChangeOwnerAccountOrRole,
    DashboardFigures,
}
