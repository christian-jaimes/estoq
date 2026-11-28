using Estoq.Core.Roles;

namespace Estoq.Core.Dashboard;

/// <summary>What a role sees on the dashboard (the "By role" table of docs/features/dashboard.md). The matrix in <see cref="RolePermissions"/> is the source.</summary>
/// <param name="OwnFiguresOnly">The sales figures count only the signed-in person's own sales (cashiers).</param>
/// <param name="ShowCharts">Sales over time, payment methods, top products and categories.</param>
/// <param name="CanExport">Export report.</param>
public sealed record DashboardAccess(bool OwnFiguresOnly, bool ShowCharts, bool CanExport)
{
    public static DashboardAccess For(UserRole role)
    {
        var figures = RolePermissions.GetAccess(role, Permission.DashboardFigures);
        return new DashboardAccess(
            OwnFiguresOnly: figures == Access.Own,
            ShowCharts: figures == Access.Yes,
            CanExport: RolePermissions.IsAllowed(role, Permission.ExportCsv));
    }
}
