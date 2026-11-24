using Estoq.Core.Roles;

namespace Estoq.Core.Sales;

/// <summary>What a requested discount needs before it can be applied.</summary>
public enum DiscountDecision
{
    Allowed,

    /// <summary>Above the cap: a manager or owner must approve on the same screen.</summary>
    NeedsApproval,

    /// <summary>Not a percentage between 0 and 100 with at most two decimals, or not permitted for the role.</summary>
    Invalid,
}

/// <summary>Discounts are percentages only. Everyone may discount up to the cap; above it the role matrix decides.</summary>
public static class DiscountPolicy
{
    /// <summary>The cap until the owner changes it in settings (decision 30).</summary>
    public const decimal DefaultCapPercent = 10m;

    public const decimal MaxPercent = 100m;

    public static bool IsValidPercent(decimal percent) =>
        percent is >= 0m and <= MaxPercent && decimal.Round(percent, 2) == percent;

    public static DiscountDecision Evaluate(UserRole role, decimal percent, decimal capPercent = DefaultCapPercent)
    {
        if (!IsValidPercent(percent))
        {
            return DiscountDecision.Invalid;
        }

        if (percent <= capPercent)
        {
            return RolePermissions.IsAllowed(role, Permission.DiscountUpToCap) ? DiscountDecision.Allowed : DiscountDecision.Invalid;
        }

        return RolePermissions.GetAccess(role, Permission.DiscountAboveCap) switch
        {
            Access.Yes => DiscountDecision.Allowed,
            Access.Approve => DiscountDecision.NeedsApproval,
            _ => DiscountDecision.Invalid,
        };
    }
}
