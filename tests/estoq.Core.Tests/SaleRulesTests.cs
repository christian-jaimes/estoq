using Estoq.Core.Roles;
using Estoq.Core.Sales;
using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class SaleCartTests
{
    [Fact]
    public void ScanningAgainRaisesTheQuantity()
    {
        var cart = new SaleCart();
        cart.Add("v1", "Basic T-Shirt", "TS-001", 12m);
        var line = cart.Add("v1", "Basic T-Shirt", "TS-001", 12m);

        Assert.Equal(2, line.Quantity);
        Assert.Single(cart.Lines);
        Assert.Equal(24m, cart.Subtotal);
        Assert.Equal(2, cart.ItemCount);
    }

    [Fact]
    public void SubtotalSumsTheLines()
    {
        var cart = new SaleCart();
        cart.Add("v1", "Basic T-Shirt", "TS-001", 12m);
        cart.SetQuantity("v1", 2);
        cart.Add("v2", "Slim Fit Jeans", "JN-014", 28m);
        cart.Add("v3", "Hoodie", "HD-008", 20m);

        Assert.Equal(72m, cart.Subtotal);
        Assert.Equal(4, cart.ItemCount);
    }

    [Fact]
    public void ZeroQuantityRemovesTheLine()
    {
        var cart = new SaleCart();
        cart.Add("v1", "Hoodie", "HD-008", 20m);
        cart.SetQuantity("v1", 0);

        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void QuantityIsCapped()
    {
        var cart = new SaleCart();
        cart.Add("v1", "Hoodie", "HD-008", 20m);
        cart.SetQuantity("v1", 1_000_000);

        Assert.Equal(SaleCart.MaxQuantity, cart.Lines[0].Quantity);
    }

    [Fact]
    public void UnknownVariantIsIgnored()
    {
        var cart = new SaleCart();
        cart.SetQuantity("missing", 3);

        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void ClearEmptiesTheCart()
    {
        var cart = new SaleCart();
        cart.Add("v1", "Hoodie", "HD-008", 20m);
        cart.Clear();

        Assert.True(cart.IsEmpty);
        Assert.Equal(0m, cart.Subtotal);
    }
}

public class SaleTotalsTests
{
    [Fact]
    public void TaxIsAddedWhenPricesExcludeIt()
    {
        var totals = SaleTotals.Calculate(72m, 0m, Rate(16m), TaxMode.Excluded);

        Assert.Equal(new SaleTotals(72m, 0m, 11.52m, 83.52m), totals);
    }

    [Fact]
    public void DiscountComesBeforeTax()
    {
        var totals = SaleTotals.Calculate(100m, 10m, Rate(16m), TaxMode.Excluded);

        Assert.Equal(10m, totals.Discount);
        Assert.Equal(14.40m, totals.Tax);
        Assert.Equal(104.40m, totals.Total);
    }

    [Fact]
    public void IncludedTaxDoesNotChangeTheTotal()
    {
        var totals = SaleTotals.Calculate(116m, 0m, Rate(16m), TaxMode.Included);

        Assert.Equal(116m, totals.Total);
        Assert.Equal(16m, totals.Tax);
    }

    [Fact]
    public void ZeroRateAddsNothing()
    {
        var totals = SaleTotals.Calculate(50m, 0m, TaxRate.Default, TaxMode.Excluded);

        Assert.Equal(0m, totals.Tax);
        Assert.Equal(50m, totals.Total);
    }

    [Fact]
    public void AmountsRoundHalfAwayFromZero()
    {
        var totals = SaleTotals.Calculate(0.05m, 50m, TaxRate.Default, TaxMode.Excluded);

        Assert.Equal(0.03m, totals.Discount);
    }

    private static TaxRate Rate(decimal percent)
    {
        Assert.True(TaxRate.TryCreate(percent, out var rate));
        return rate;
    }
}

public class DiscountPolicyTests
{
    [Theory]
    [InlineData(UserRole.Cashier, 10, DiscountDecision.Allowed)]
    [InlineData(UserRole.Cashier, 10.01, DiscountDecision.NeedsApproval)]
    [InlineData(UserRole.Manager, 50, DiscountDecision.Allowed)]
    [InlineData(UserRole.Owner, 100, DiscountDecision.Allowed)]
    [InlineData(UserRole.Cashier, 0, DiscountDecision.Allowed)]
    public void CapAndApproval(UserRole role, double percent, DiscountDecision expected) =>
        Assert.Equal(expected, DiscountPolicy.Evaluate(role, (decimal)percent));

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(5.555)]
    public void InvalidPercentages(double percent) =>
        Assert.Equal(DiscountDecision.Invalid, DiscountPolicy.Evaluate(UserRole.Owner, (decimal)percent));

    [Fact]
    public void CapIsConfigurable()
    {
        Assert.Equal(DiscountDecision.Allowed, DiscountPolicy.Evaluate(UserRole.Cashier, 20m, 25m));
        Assert.Equal(DiscountDecision.NeedsApproval, DiscountPolicy.Evaluate(UserRole.Cashier, 20m, 15m));
    }
}

public class ExchangeRatesTests
{
    private static ExchangeRates Rates() => new("USD", new Dictionary<string, decimal>
    {
        ["EUR"] = 0.92m,
        ["VES"] = 36.50m,
        ["COP"] = 4100m,
    });

    [Fact]
    public void BaseCurrencyIsAlwaysSupportedAtOne()
    {
        Assert.True(Rates().Supports("usd"));
        Assert.Equal(1m, Rates().RateOf("USD"));
        Assert.Equal(83.52m, Rates().FromBase(83.52m, "USD"));
    }

    [Fact]
    public void ConvertsFromBaseAndBack()
    {
        var rates = Rates();

        Assert.Equal(3048.48m, rates.FromBase(83.52m, "VES"));
        Assert.Equal(83.52m, rates.ToBase(3048.48m, "VES"));
    }

    [Fact]
    public void UnknownCurrencyThrows() =>
        Assert.Throws<KeyNotFoundException>(() => Rates().RateOf("JPY"));

    [Fact]
    public void RateMustBePositive() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExchangeRates("USD", new Dictionary<string, decimal> { ["EUR"] = 0m }));

    [Theory]
    [InlineData(83.52, "EUR")]
    [InlineData(10.01, "VES")]
    [InlineData(0.01, "COP")]
    [InlineData(99.99, "EUR")]
    public void AmountToCoverNeverLeavesTheSaleShort(double baseAmount, string currency)
    {
        var rates = Rates();
        var cover = rates.AmountToCover((decimal)baseAmount, currency);

        Assert.True(rates.ToBase(cover, currency) >= (decimal)baseAmount);
    }
}

public class PaymentSettlementTests
{
    private static readonly ExchangeRates Rates = new("USD", new Dictionary<string, decimal>
    {
        ["EUR"] = 0.92m,
        ["VES"] = 36.50m,
    });

    [Fact]
    public void CashOverpaymentGivesChange()
    {
        var result = PaymentSettlement.Calculate(83.52m, [new PaymentLine(PaymentMethod.Cash, "USD", 100m)], Rates);

        Assert.Equal(16.48m, result.Change);
        Assert.Equal(0m, result.Remaining);
        Assert.True(result.CanComplete);
    }

    [Fact]
    public void ShortPaymentShowsWhatIsStillOwed()
    {
        var result = PaymentSettlement.Calculate(83.52m, [new PaymentLine(PaymentMethod.Cash, "USD", 50m)], Rates);

        Assert.Equal(33.52m, result.Remaining);
        Assert.False(result.IsCovered);
        Assert.False(result.CanComplete);
    }

    [Fact]
    public void SplitPaymentsAcrossMethodsAndCurrencies()
    {
        var result = PaymentSettlement.Calculate(
            100m,
            [
                new PaymentLine(PaymentMethod.Card, "USD", 60m),
                new PaymentLine(PaymentMethod.Cash, "VES", 1460m),
            ],
            Rates);

        Assert.Equal(100m, result.Paid);
        Assert.Equal(0m, result.Remaining);
        Assert.Equal(0m, result.Change);
        Assert.True(result.CanComplete);
    }

    [Fact]
    public void ChangeCannotExceedTheCashTaken()
    {
        var result = PaymentSettlement.Calculate(50m, [new PaymentLine(PaymentMethod.Card, "USD", 60m)], Rates);

        Assert.True(result.IsCovered);
        Assert.Equal(10m, result.Change);
        Assert.False(result.CanGiveChange);
        Assert.False(result.CanComplete);
    }

    [Fact]
    public void ChangeCanComeFromCashWhenOtherMethodsPaidToo()
    {
        var result = PaymentSettlement.Calculate(
            50m,
            [new PaymentLine(PaymentMethod.Card, "USD", 40m), new PaymentLine(PaymentMethod.Cash, "USD", 20m)],
            Rates);

        Assert.Equal(10m, result.Change);
        Assert.True(result.CanComplete);
    }

    [Fact]
    public void ZeroAndNegativeAmountsAreIgnored()
    {
        var result = PaymentSettlement.Calculate(
            10m,
            [new PaymentLine(PaymentMethod.Cash, "USD", 0m), new PaymentLine(PaymentMethod.Cash, "USD", -5m)],
            Rates);

        Assert.Equal(0m, result.Paid);
        Assert.Equal(10m, result.Remaining);
    }
}

public class StockCheckTests
{
    [Theory]
    [InlineData(5, 2, StockLevel.Enough)]
    [InlineData(2, 2, StockLevel.Enough)]
    [InlineData(1, 2, StockLevel.Partial)]
    [InlineData(0, 1, StockLevel.OutOfStock)]
    [InlineData(-3, 1, StockLevel.OutOfStock)]
    public void Levels(int onHand, int quantity, StockLevel expected) =>
        Assert.Equal(expected, StockCheck.Evaluate(onHand, quantity));

    [Theory]
    [InlineData(5, 2, 0)]
    [InlineData(1, 3, 2)]
    [InlineData(0, 2, 2)]
    [InlineData(-4, 2, 2)]
    public void UnitsSoldWithoutStock(int onHand, int quantity, int expected) =>
        Assert.Equal(expected, StockCheck.UnitsWithoutStock(onHand, quantity));
}

public class PaymentMethodsTests
{
    [Fact]
    public void OrderMatchesTheTill() =>
        Assert.Equal(
            [
                PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.Transfer, PaymentMethod.Zelle,
                PaymentMethod.Usdt, PaymentMethod.MobilePayment, PaymentMethod.PayPal, PaymentMethod.Other,
            ],
            PaymentMethods.All);

    [Fact]
    public void OnlyCashGivesChange() =>
        Assert.Equal([PaymentMethod.Cash], PaymentMethods.All.Where(PaymentMethods.CanGiveChange));
}
