using Estoq.Core.Roles;
using Xunit;

namespace Estoq.Core.Tests;

public class RolePermissionsTests
{
    [Fact]
    public void EveryPermissionHasARowForEveryRole()
    {
        foreach (var permission in Enum.GetValues<Permission>())
        {
            foreach (var role in Enum.GetValues<UserRole>())
            {
                _ = RolePermissions.GetAccess(role, permission);
            }
        }
    }

    [Theory]
    [InlineData(UserRole.Cashier, Permission.MakeSale, Access.Yes)]
    [InlineData(UserRole.Cashier, Permission.DiscountAboveCap, Access.Approve)]
    [InlineData(UserRole.Manager, Permission.DiscountAboveCap, Access.Yes)]
    [InlineData(UserRole.Cashier, Permission.SeeCostsAndMargins, Access.No)]
    [InlineData(UserRole.Manager, Permission.SeeCostsAndMargins, Access.Yes)]
    [InlineData(UserRole.Cashier, Permission.AdjustStock, Access.No)]
    [InlineData(UserRole.Cashier, Permission.VoidSale, Access.Approve)]
    [InlineData(UserRole.Cashier, Permission.ViewSalesHistory, Access.Own)]
    [InlineData(UserRole.Manager, Permission.ViewSalesHistory, Access.Yes)]
    [InlineData(UserRole.Cashier, Permission.ExportCsv, Access.No)]
    [InlineData(UserRole.Cashier, Permission.UpdateExchangeRates, Access.Yes)]
    [InlineData(UserRole.Cashier, Permission.SettingsAccount, Access.Yes)]
    [InlineData(UserRole.Cashier, Permission.SettingsStore, Access.No)]
    [InlineData(UserRole.Manager, Permission.SettingsStaff, Access.Yes)]
    [InlineData(UserRole.Manager, Permission.SettingsDefaultsAndRules, Access.View)]
    [InlineData(UserRole.Owner, Permission.SettingsDefaultsAndRules, Access.Yes)]
    [InlineData(UserRole.Manager, Permission.SettingsData, Access.No)]
    [InlineData(UserRole.Owner, Permission.SettingsData, Access.Yes)]
    [InlineData(UserRole.Manager, Permission.ChangeOwnerAccountOrRole, Access.No)]
    [InlineData(UserRole.Cashier, Permission.DashboardFigures, Access.Own)]
    [InlineData(UserRole.Owner, Permission.DashboardFigures, Access.Yes)]
    public void MatrixMatchesTheProductDocument(UserRole role, Permission permission, Access expected) =>
        Assert.Equal(expected, RolePermissions.GetAccess(role, permission));

    [Fact]
    public void OwnerIsAllowedEverything()
    {
        foreach (var permission in Enum.GetValues<Permission>())
        {
            Assert.True(RolePermissions.IsAllowed(UserRole.Owner, permission), permission.ToString());
        }
    }

    [Fact]
    public void ApprovalIsOnlyNeededWhereTheMatrixSaysApprove()
    {
        Assert.True(RolePermissions.NeedsApproval(UserRole.Cashier, Permission.VoidSale));
        Assert.False(RolePermissions.NeedsApproval(UserRole.Manager, Permission.VoidSale));
        Assert.False(RolePermissions.IsAllowed(UserRole.Cashier, Permission.VoidSale));
    }

    [Fact]
    public void InactiveAccountsCannotSignIn()
    {
        Assert.True(RolePermissions.CanSignIn(AccountStatus.Active));
        Assert.False(RolePermissions.CanSignIn(AccountStatus.Inactive));
    }

    [Theory]
    [InlineData(UserRole.Owner, UserRole.Owner, true)]
    [InlineData(UserRole.Owner, UserRole.Manager, true)]
    [InlineData(UserRole.Manager, UserRole.Cashier, true)]
    [InlineData(UserRole.Manager, UserRole.Owner, false)]
    [InlineData(UserRole.Cashier, UserRole.Cashier, false)]
    [InlineData(UserRole.Cashier, UserRole.Owner, false)]
    public void OnlyTheOwnerCanTouchTheOwnerAccount(UserRole actor, UserRole target, bool expected) =>
        Assert.Equal(expected, RolePermissions.CanManageAccount(actor, target));

    [Fact]
    public void UnknownPermissionIsRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RolePermissions.GetAccess(UserRole.Owner, (Permission)999));
}
