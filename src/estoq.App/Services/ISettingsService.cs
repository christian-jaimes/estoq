using Estoq.Core.Roles;
using Estoq.Core.Settings;
using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>The signed-in person's own details, edited in the Account section.</summary>
public sealed record AccountProfile(string Name, string Email, string Code, UserRole Role, string? PhotoPath);

/// <summary>A currency with its rate in force now, for the Exchange rates table. The base currency has no entry.</summary>
public sealed record CurrentRate(string Currency, bool IsBase, RateEntry? Entry);

/// <summary>
/// What the Settings pop-up reads and writes (docs/features/settings.md). Every change is audited with old value, new
/// value, who and when. The sample implementation keeps everything in memory; the back end replaces it with the
/// encrypted database and enforces the permission matrix again (hiding a control is never enough).
/// </summary>
public interface ISettingsService
{
    /// <summary>Raised after any saved change, so open lists can refresh.</summary>
    event EventHandler? Changed;

    AccountProfile GetAccount();

    ValidationCode SaveAccount(AccountProfile profile);

    /// <summary>Changes the signed-in person's password. The change is audited without the password itself.</summary>
    ValidationCode ChangePassword(string password, string confirmation);

    StoreProfile GetStore();

    /// <summary>Saves the store details. Only managers and owners; returns the first validation problem, or None.</summary>
    ValidationCode SaveStore(StoreProfile profile);

    /// <summary>Idle time before automatic log out. Only the owner may change it.</summary>
    TimeSpan InactivityTimeout { get; }

    bool SaveInactivityTimeout(TimeSpan timeout);

    /// <summary>Stock at or below this count is "Low stock" (see <see cref="LowStockPolicy"/>). Only the owner may change it.</summary>
    int LowStockThreshold { get; }

    bool SaveLowStockThreshold(int threshold);

    IReadOnlyList<StaffMember> Staff { get; }

    StaffError AddStaff(string name, string? email, UserRole role, out StaffMember? added);

    StaffError EditStaff(Guid id, string name, string? email, UserRole role);

    StaffError SetStaffStatus(Guid id, AccountStatus status);

    StaffError RegenerateStaffCode(Guid id);

    /// <summary>The base currency first, then every accepted currency, each with the rate in force now.</summary>
    IReadOnlyList<CurrentRate> CurrentRates();

    /// <summary>Every rate ever recorded for the currency, newest first. Rates are never overwritten.</summary>
    IReadOnlyList<RateEntry> RateHistory(string currency);

    /// <summary>Appends a rate effective from <paramref name="effectiveAt"/>. Open to every role.</summary>
    bool UpdateRate(string currency, decimal rate, DateTimeOffset effectiveAt);

    /// <summary>Audited changes of a section, newest first. Empty for roles that may not see history.</summary>
    IReadOnlyList<SettingChange> History(SettingsSection section);
}
