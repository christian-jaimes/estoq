namespace Estoq.Core.Sales;

/// <summary>How a payment was made. The declaration order is the fallback when store usage counts tie.</summary>
public enum PaymentMethod
{
    Cash,
    Card,
    Transfer,
    Zelle,
    Usdt,
    MobilePayment,
    PayPal,
    Other,
}

public static class PaymentMethods
{
    /// <summary>Every method in fallback display order.</summary>
    public static IReadOnlyList<PaymentMethod> All { get; } = Enum.GetValues<PaymentMethod>();

    /// <summary>Only cash can be handed back as change; every other method is recorded as paid externally.</summary>
    public static bool CanGiveChange(PaymentMethod method) => method == PaymentMethod.Cash;
}
