namespace Estoq.Core.Roles;

/// <summary>How far a role may use a capability. Order is least to most.</summary>
public enum Access
{
    /// <summary>Not allowed.</summary>
    No,

    /// <summary>Own records only.</summary>
    Own,

    /// <summary>A manager or owner signs in on the same screen to approve; the approver is recorded.</summary>
    Approve,

    /// <summary>Read-only.</summary>
    View,

    /// <summary>Allowed, whole shop where it applies.</summary>
    Yes,
}
