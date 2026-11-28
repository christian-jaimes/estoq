using Estoq.Core.Roles;

namespace Estoq.Core.Settings;

/// <summary>A staff account as listed in Settings. The code is the six-digit sign-in code.</summary>
public sealed record StaffMember(
    Guid Id,
    string Name,
    string Email,
    string Code,
    UserRole Role,
    AccountStatus Status);
