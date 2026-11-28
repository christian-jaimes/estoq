using Estoq.Core.Roles;

namespace Estoq.Core.Settings;

/// <summary>The sections of the Settings pop-up (docs/features/settings.md).</summary>
public enum SettingsSection
{
    Account,
    Store,
    Staff,
    ExchangeRates,
}

/// <summary>Which sections each role may open. Mirrors the Settings rows of the permission matrix.</summary>
public static class SettingsSections
{
    public static bool CanOpen(UserRole role, SettingsSection section) => section switch
    {
        SettingsSection.Account => RolePermissions.IsAllowed(role, Permission.SettingsAccount),
        SettingsSection.Store => RolePermissions.IsAllowed(role, Permission.SettingsStore),
        SettingsSection.Staff => RolePermissions.IsAllowed(role, Permission.SettingsStaff),
        SettingsSection.ExchangeRates => RolePermissions.IsAllowed(role, Permission.UpdateExchangeRates),
        _ => false,
    };

    /// <summary>The sections the role sees, in display order.</summary>
    public static IReadOnlyList<SettingsSection> Available(UserRole role) =>
        Enum.GetValues<SettingsSection>().Where(section => CanOpen(role, section)).ToList();

    /// <summary>Owners and managers read the change history of Store and Staff; exchange rates keep their own.</summary>
    public static bool CanSeeHistory(UserRole role) => role is UserRole.Owner or UserRole.Manager;
}
