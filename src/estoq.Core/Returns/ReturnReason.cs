namespace Estoq.Core.Returns;

/// <summary>Why the customer brought the items back. The reason is optional (docs/features/returns-and-voids.md).</summary>
public enum ReturnReason
{
    ChangeOfMind,
    WrongSize,
    Defective,
    WrongItem,
    Other,
}
