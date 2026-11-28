using Estoq.Core.Roles;
using Estoq.Core.Setup;

namespace Estoq.Core.Settings;

/// <summary>Field rules of the Account section and the Add or edit staff form.</summary>
public static class AccountRules
{
    public static ValidationCode ValidateName(string? name) => SetupRules.ValidateRequired(name);

    /// <summary>The owner must have an email; for everyone else it is optional.</summary>
    public static ValidationCode ValidateEmail(UserRole role, string? email) =>
        role == UserRole.Owner ? SetupRules.ValidateOwnerEmail(email) : SetupRules.ValidateOptionalEmail(email);
}
