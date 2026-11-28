using Estoq.Core.Roles;
using Estoq.Core.Settings;
using Estoq.Core.Setup;
using Estoq.Core.SignIn;

namespace Estoq.App.Services;

/// <summary>
/// Front-end implementation: sample store, staff and rates held in memory, with the Core rules applied and every change
/// audited. Nothing is stored, so the sample data comes back on the next launch. The staff list here is not yet the
/// list sign-in checks; the back end makes them one table.
/// </summary>
public sealed class SampleSettingsService : ISettingsService
{
    private static readonly DateTimeOffset SeedTime = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);

    private readonly ISessionService _session;
    private readonly ISecuritySettings _security;
    private readonly List<SettingChange> _audit = [];
    private readonly ExchangeRateBook _rates = new(Currencies.Default);
    private readonly StaffDirectory _staff;
    private AccountProfile? _account;
    private StoreProfile _store;

    public SampleSettingsService(ISessionService session, ISecuritySettings security)
    {
        _session = session;
        _security = security;
        _store = new StoreProfile(
            "Estoq Shop", "J-12345678-9", "Av. Principal 120", "Cúcuta", "540001", "CO", "+57 300 000 0000", "shop@estoq.example",
            Currencies.Default, ["EUR", "VES", "COP"], SampleTaxRate(), TaxMode.Included, AppLanguage.English);

        _staff = new StaffDirectory(
        [
            new StaffMember(Guid.NewGuid(), "Carlos Mendoza", "carlos@estoq.example", "100200", UserRole.Owner, AccountStatus.Active),
            new StaffMember(Guid.NewGuid(), "Marta Rojas", "marta@estoq.example", "482913", UserRole.Manager, AccountStatus.Active),
            new StaffMember(Guid.NewGuid(), "Luis Pérez", string.Empty, "570146", UserRole.Cashier, AccountStatus.Active),
            new StaffMember(Guid.NewGuid(), "Sofía Díaz", string.Empty, "315208", UserRole.Cashier, AccountStatus.Inactive),
        ]);

        // Same rates the sample till uses.
        _rates.Append("EUR", 0.92m, SeedTime, "Carlos Mendoza", SeedTime);
        _rates.Append("VES", 36.50m, SeedTime, "Carlos Mendoza", SeedTime);
        _rates.Append("COP", 4100m, SeedTime, "Carlos Mendoza", SeedTime);
        _rates.Append("VES", 35.80m, SeedTime.AddDays(-3), "Marta Rojas", SeedTime.AddDays(-3));
    }

    public event EventHandler? Changed;

    public TimeSpan InactivityTimeout => _security.InactivityTimeout;

    public IReadOnlyList<StaffMember> Staff => _staff.Members;

    private string ActorName => _session.UserName ?? string.Empty;

    private UserRole ActorRole => _session.Role;

    public AccountProfile GetAccount()
    {
        var current = _session.CurrentUser;
        var member = _staff.Members.FirstOrDefault(m => string.Equals(m.Name, current?.Name, StringComparison.OrdinalIgnoreCase));
        _account ??= new AccountProfile(
            current?.Name ?? string.Empty,
            member?.Email ?? string.Empty,
            member?.Code ?? UserCode.Generate(),
            current?.Role ?? UserRole.Cashier,
            null);
        return _account with { Role = ActorRole };
    }

    public ValidationCode SaveAccount(AccountProfile profile)
    {
        var current = GetAccount();
        var problem = AccountRules.ValidateName(profile.Name);
        if (problem == ValidationCode.None)
        {
            problem = AccountRules.ValidateEmail(current.Role, profile.Email);
        }

        if (problem != ValidationCode.None)
        {
            return problem;
        }

        var before = Fields(current);
        _account = current with { Name = profile.Name.Trim(), Email = profile.Email.Trim(), PhotoPath = profile.PhotoPath };
        Record(SettingsDiff.Compare(SettingsSection.Account, before, Fields(_account), ActorName, DateTimeOffset.Now));
        Changed?.Invoke(this, EventArgs.Empty);
        return ValidationCode.None;
    }

    public ValidationCode ChangePassword(string password, string confirmation)
    {
        var problem = SetupRules.ValidatePassword(password);
        if (problem == ValidationCode.None)
        {
            problem = SetupRules.ValidatePasswordConfirmation(password, confirmation);
        }

        if (problem != ValidationCode.None)
        {
            return problem;
        }

        // The sample keeps no passwords; the entry shows that a change happened, never the value.
        _audit.Add(new SettingChange(SettingsSection.Account, "Password", null, "••••••••", ActorName, DateTimeOffset.Now));
        Changed?.Invoke(this, EventArgs.Empty);
        return ValidationCode.None;
    }

    public StoreProfile GetStore() => _store;

    public ValidationCode SaveStore(StoreProfile profile)
    {
        if (!RolePermissions.IsAllowed(ActorRole, Permission.SettingsStore))
        {
            return ValidationCode.Required;
        }

        var problem = StoreRules.Validate(profile);
        if (problem != ValidationCode.None)
        {
            return problem;
        }

        var normalized = profile with
        {
            Name = profile.Name.Trim(),
            AcceptedCurrencies = Currencies.NormalizeAccepted(profile.BaseCurrency, profile.AcceptedCurrencies),
        };
        Record(SettingsDiff.Compare(SettingsSection.Store, _store.ToFields(), normalized.ToFields(), ActorName, DateTimeOffset.Now));
        _store = normalized;
        Changed?.Invoke(this, EventArgs.Empty);
        return ValidationCode.None;
    }

    public bool SaveInactivityTimeout(TimeSpan timeout)
    {
        if (ActorRole != UserRole.Owner || !InactivityPolicy.IsValidTimeout(timeout))
        {
            return false;
        }

        var before = _security.InactivityTimeout;
        if (before != timeout)
        {
            _security.InactivityTimeout = timeout;
            _audit.Add(new SettingChange(SettingsSection.Store, "InactivityTimeout",
                before.TotalMinutes.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                timeout.TotalMinutes.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture), ActorName, DateTimeOffset.Now));
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return true;
    }

    public StaffError AddStaff(string name, string? email, UserRole role, out StaffMember? added)
    {
        var error = _staff.Add(ActorRole, ActorName, DateTimeOffset.Now, name, email, role, out added, out var audit);
        Applied(error, audit);
        return error;
    }

    public StaffError EditStaff(Guid id, string name, string? email, UserRole role)
    {
        var error = _staff.Edit(ActorRole, ActorName, DateTimeOffset.Now, id, name, email, role, out var audit);
        Applied(error, audit);
        return error;
    }

    public StaffError SetStaffStatus(Guid id, AccountStatus status)
    {
        var error = _staff.SetStatus(ActorRole, ActorName, DateTimeOffset.Now, id, status, out var audit);
        Applied(error, audit);
        return error;
    }

    public StaffError RegenerateStaffCode(Guid id)
    {
        var error = _staff.RegenerateCode(ActorRole, ActorName, DateTimeOffset.Now, id, out var audit);
        Applied(error, audit);
        return error;
    }

    public IReadOnlyList<CurrentRate> CurrentRates()
    {
        var now = DateTimeOffset.Now;
        var rows = new List<CurrentRate> { new(_store.BaseCurrency, true, null) };
        rows.AddRange(_store.AcceptedCurrencies.Select(code => new CurrentRate(code, false, _rates.RateAt(code, now))));
        return rows;
    }

    public IReadOnlyList<RateEntry> RateHistory(string currency) => _rates.History(currency);

    public bool UpdateRate(string currency, decimal rate, DateTimeOffset effectiveAt)
    {
        if (!ExchangeRateBook.CanUpdate(ActorRole) || !_store.AcceptedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var appended = _rates.Append(currency, rate, effectiveAt, ActorName, DateTimeOffset.Now);
        if (appended)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return appended;
    }

    public IReadOnlyList<SettingChange> History(SettingsSection section) =>
        SettingsSections.CanSeeHistory(ActorRole)
            ? _audit.Where(change => change.Section == section).OrderByDescending(change => change.At).ToList()
            : [];

    private static TaxRate SampleTaxRate() => TaxRate.TryCreate(16m, out var rate) ? rate : TaxRate.Default;

    private static Dictionary<string, string> Fields(AccountProfile profile) => new()
    {
        ["Name"] = profile.Name,
        ["Email"] = profile.Email,
        ["Photo"] = profile.PhotoPath ?? string.Empty,
    };

    private void Applied(StaffError error, IReadOnlyList<SettingChange> audit)
    {
        if (error != StaffError.None)
        {
            return;
        }

        Record(audit);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Record(IEnumerable<SettingChange> changes) => _audit.AddRange(changes);
}
