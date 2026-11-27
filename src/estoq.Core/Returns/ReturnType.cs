namespace Estoq.Core.Returns;

/// <summary>What the customer wants for the items they bring back.</summary>
public enum ReturnType
{
    /// <summary>The items go back and the money goes back to the customer.</summary>
    Refund,

    /// <summary>The items go back and the customer takes other items; the difference is settled.</summary>
    Exchange,
}
