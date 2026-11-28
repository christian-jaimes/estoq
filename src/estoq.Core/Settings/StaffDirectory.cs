using Estoq.Core.Roles;
using Estoq.Core.Setup;

namespace Estoq.Core.Settings;

/// <summary>Why a staff change was refused. <see cref="None"/> means it was applied.</summary>
public enum StaffError
{
    None,

    /// <summary>The acting role may not manage that account (a manager touching the owner).</summary>
    NotAllowed,

    /// <summary>There is exactly one owner: a second one cannot be added, and the owner cannot be re-roled.</summary>
    OwnerIsUnique,

    /// <summary>The name is empty.</summary>
    NameRequired,

    /// <summary>The email is malformed (or missing for the owner).</summary>
    EmailInvalid,

    /// <summary>The generated code is already used by someone else.</summary>
    CodeInUse,

    /// <summary>No such staff member.</summary>
    NotFound,
}

/// <summary>
/// The staff accounts and the rules for changing them (docs/product/roles-and-permissions.md): exactly one owner,
/// a manager cannot change or deactivate the owner, codes are unique. Every applied change is returned as audit entries.
/// Pure logic: the application service stores the directory and the audit log.
/// </summary>
public sealed class StaffDirectory
{
    private readonly List<StaffMember> _members;
    private readonly Func<string> _newCode;

    public StaffDirectory(IEnumerable<StaffMember> members, Func<string>? newCode = null)
    {
        ArgumentNullException.ThrowIfNull(members);
        _members = members.ToList();
        _newCode = newCode ?? Setup.UserCode.Generate;
    }

    public IReadOnlyList<StaffMember> Members => _members;

    public StaffMember? Find(Guid id) => _members.FirstOrDefault(member => member.Id == id);

    /// <summary>Adds a staff member with a fresh unique code. Only managers and owners may; the role must not be Owner.</summary>
    public StaffError Add(UserRole actor, string user, DateTimeOffset at, string name, string? email, UserRole role,
        out StaffMember? added, out IReadOnlyList<SettingChange> audit)
    {
        added = null;
        audit = [];
        if (!RolePermissions.IsAllowed(actor, Permission.SettingsStaff))
        {
            return StaffError.NotAllowed;
        }

        if (role == UserRole.Owner)
        {
            return StaffError.OwnerIsUnique;
        }

        var invalid = Validate(role, name, email);
        if (invalid != StaffError.None)
        {
            return invalid;
        }

        added = new StaffMember(Guid.NewGuid(), name.Trim(), email?.Trim() ?? string.Empty, UniqueCode(), role, AccountStatus.Active);
        _members.Add(added);
        audit = [Entry(user, at, "Added", null, Describe(added))];
        return StaffError.None;
    }

    /// <summary>Changes name, email and role. The owner's role cannot change; only the owner may edit the owner.</summary>
    public StaffError Edit(UserRole actor, string user, DateTimeOffset at, Guid id, string name, string? email, UserRole role,
        out IReadOnlyList<SettingChange> audit)
    {
        audit = [];
        var current = Find(id);
        if (current is null)
        {
            return StaffError.NotFound;
        }

        if (!RolePermissions.CanManageAccount(actor, current.Role))
        {
            return StaffError.NotAllowed;
        }

        if (role != current.Role && (current.Role == UserRole.Owner || role == UserRole.Owner))
        {
            return StaffError.OwnerIsUnique;
        }

        var invalid = Validate(current.Role, name, email);
        if (invalid != StaffError.None)
        {
            return invalid;
        }

        var updated = current with { Name = name.Trim(), Email = email?.Trim() ?? string.Empty, Role = role };
        var changes = new List<SettingChange>();
        if (updated.Name != current.Name)
        {
            changes.Add(Entry(user, at, "StaffName", current.Name, updated.Name, current.Name));
        }

        if (updated.Email != current.Email)
        {
            changes.Add(Entry(user, at, "StaffEmail", current.Email, updated.Email, current.Name));
        }

        if (updated.Role != current.Role)
        {
            changes.Add(Entry(user, at, "StaffRole", current.Role.ToString(), updated.Role.ToString(), current.Name));
        }

        Replace(current, updated);
        audit = changes;
        return StaffError.None;
    }

    /// <summary>Activates or deactivates an account. Nobody can deactivate the owner through a manager.</summary>
    public StaffError SetStatus(UserRole actor, string user, DateTimeOffset at, Guid id, AccountStatus status,
        out IReadOnlyList<SettingChange> audit)
    {
        audit = [];
        var current = Find(id);
        if (current is null)
        {
            return StaffError.NotFound;
        }

        if (!RolePermissions.CanManageAccount(actor, current.Role))
        {
            return StaffError.NotAllowed;
        }

        if (current.Status != status)
        {
            Replace(current, current with { Status = status });
            audit = [Entry(user, at, "StaffStatus", current.Status.ToString(), status.ToString(), current.Name)];
        }

        return StaffError.None;
    }

    /// <summary>Replaces the sign-in code. The old and new codes are audited masked, never in full.</summary>
    public StaffError RegenerateCode(UserRole actor, string user, DateTimeOffset at, Guid id,
        out IReadOnlyList<SettingChange> audit)
    {
        audit = [];
        var current = Find(id);
        if (current is null)
        {
            return StaffError.NotFound;
        }

        if (!RolePermissions.CanManageAccount(actor, current.Role))
        {
            return StaffError.NotAllowed;
        }

        var updated = current with { Code = UniqueCode() };
        Replace(current, updated);
        audit = [Entry(user, at, "StaffCode", Mask(current.Code), Mask(updated.Code), current.Name)];
        return StaffError.None;
    }

    /// <summary>The code with all but the last two digits hidden, for the audit log.</summary>
    public static string Mask(string code) =>
        code.Length <= 2 ? code : new string('•', code.Length - 2) + code[^2..];

    private static StaffError Validate(UserRole role, string? name, string? email)
    {
        if (AccountRules.ValidateName(name) != ValidationCode.None)
        {
            return StaffError.NameRequired;
        }

        return AccountRules.ValidateEmail(role, email) == ValidationCode.None ? StaffError.None : StaffError.EmailInvalid;
    }

    private string UniqueCode()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var code = _newCode();
            if (_members.All(member => member.Code != code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not find an unused sign-in code.");
    }

    private void Replace(StaffMember old, StaffMember updated) => _members[_members.IndexOf(old)] = updated;

    // The person the change is about goes into the field text so one history list reads well.
    private static SettingChange Entry(string user, DateTimeOffset at, string field, string? oldValue, string? newValue, string? who = null) =>
        new(SettingsSection.Staff, who is null ? field : $"{field}|{who}", oldValue, newValue, user, at);

    private static string Describe(StaffMember member) => $"{member.Name} ({member.Role})";
}
