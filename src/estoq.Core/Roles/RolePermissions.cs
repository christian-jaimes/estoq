namespace Estoq.Core.Roles;

/// <summary>The role and permission matrix. Application services enforce it; hiding a control is never enough.</summary>
public static class RolePermissions
{
    // Columns: Cashier, Manager, Owner.
    private static readonly Dictionary<Permission, (Access Cashier, Access Manager, Access Owner)> Matrix = new()
    {
        [Permission.MakeSale] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.DiscountUpToCap] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.DiscountAboveCap] = (Access.Approve, Access.Yes, Access.Yes),
        [Permission.SellOutOfStockItem] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.AddProductOrReceiveStock] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.BrowseInventory] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.SeeCostsAndMargins] = (Access.No, Access.Yes, Access.Yes),
        [Permission.EditProductDetailsAndPrices] = (Access.No, Access.Yes, Access.Yes),
        [Permission.AddVariantsOrReprintLabels] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.ArchiveOrRestoreProducts] = (Access.No, Access.Yes, Access.Yes),
        [Permission.AdjustStock] = (Access.No, Access.Yes, Access.Yes),
        [Permission.ReturnOrExchange] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.VoidSale] = (Access.Approve, Access.Yes, Access.Yes),
        [Permission.ViewSalesHistory] = (Access.Own, Access.Yes, Access.Yes),
        [Permission.ExportCsv] = (Access.No, Access.Yes, Access.Yes),
        [Permission.UpdateExchangeRates] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.SettingsAccount] = (Access.Yes, Access.Yes, Access.Yes),
        [Permission.SettingsStore] = (Access.No, Access.Yes, Access.Yes),
        [Permission.SettingsStaff] = (Access.No, Access.Yes, Access.Yes),
        [Permission.SettingsDefaultsAndRules] = (Access.No, Access.View, Access.Yes),
        [Permission.SettingsData] = (Access.No, Access.No, Access.Yes),
        [Permission.ChangeOwnerAccountOrRole] = (Access.No, Access.No, Access.Yes),
        [Permission.DashboardFigures] = (Access.Own, Access.Yes, Access.Yes),
    };

    /// <summary>How far <paramref name="role"/> may use <paramref name="permission"/>.</summary>
    public static Access GetAccess(UserRole role, Permission permission)
    {
        if (!Matrix.TryGetValue(permission, out var row))
        {
            throw new ArgumentOutOfRangeException(nameof(permission), permission, "Permission is not in the matrix.");
        }

        return role switch
        {
            UserRole.Cashier => row.Cashier,
            UserRole.Manager => row.Manager,
            UserRole.Owner => row.Owner,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role."),
        };
    }

    /// <summary>True when the role may act without anyone else's approval (full access).</summary>
    public static bool IsAllowed(UserRole role, Permission permission) => GetAccess(role, permission) == Access.Yes;

    /// <summary>True when the role needs a manager or owner to approve the action on the same screen.</summary>
    public static bool NeedsApproval(UserRole role, Permission permission) => GetAccess(role, permission) == Access.Approve;

    /// <summary>
    /// True when an account may sign in and act. Inactive accounts never can.
    /// </summary>
    public static bool CanSignIn(AccountStatus status) => status == AccountStatus.Active;

    /// <summary>
    /// Whether <paramref name="actor"/> may change, deactivate or re-role <paramref name="target"/>.
    /// Only the owner may touch the owner's account; managers may manage cashiers and other managers.
    /// </summary>
    public static bool CanManageAccount(UserRole actor, UserRole target)
    {
        if (target == UserRole.Owner)
        {
            return IsAllowed(actor, Permission.ChangeOwnerAccountOrRole);
        }

        return IsAllowed(actor, Permission.SettingsStaff);
    }
}
