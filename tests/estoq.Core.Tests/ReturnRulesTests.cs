using Estoq.Core.Returns;
using Xunit;

namespace Estoq.Core.Tests;

public class ReturnRulesTests
{
    private static ReturnedLine Line(decimal price, int quantity) => new("v1", "Basic T-Shirt", "TS-001", price, quantity);

    [Fact]
    public void ReturnableQuantityNeverGoesNegative()
    {
        Assert.Equal(2, ReturnRules.ReturnableQuantity(3, 1));
        Assert.Equal(0, ReturnRules.ReturnableQuantity(1, 3));
    }

    [Theory]
    [InlineData(0, 3, 1)]
    [InlineData(2, 3, 2)]
    [InlineData(9, 3, 3)]
    [InlineData(2, 0, 0)]
    public void ClampKeepsQuantityWithinWhatCanReturn(int typed, int returnable, int expected) =>
        Assert.Equal(expected, ReturnRules.ClampQuantity(typed, returnable));

    [Fact]
    public void NoWindowMeansNoLimit() =>
        Assert.True(ReturnRules.IsWithinWindow(new DateTime(2020, 1, 1), new DateTime(2026, 11, 27), null));

    [Fact]
    public void LastDayOfTheWindowIsStillReturnable()
    {
        var sale = new DateTime(2026, 11, 1, 18, 30, 0);
        Assert.True(ReturnRules.IsWithinWindow(sale, new DateTime(2026, 12, 1, 9, 0, 0), 30));
        Assert.False(ReturnRules.IsWithinWindow(sale, new DateTime(2026, 12, 2, 9, 0, 0), 30));
    }

    [Fact]
    public void WithoutAReceiptTheRefundIsTheCurrentPrice()
    {
        var amounts = ReturnRules.Calculate([Line(59.90m, 1), Line(10m, 2)], null);

        Assert.Equal(79.90m, amounts.Value);
        Assert.Equal(79.90m, amounts.Refund);
        Assert.False(amounts.IsCapped);
    }

    [Fact]
    public void ARefundIsTheShareOfWhatWasPaid()
    {
        // Subtotal 100, 10 % discount, paid 90: returning 40 of 100 refunds 36.
        var amounts = ReturnRules.Calculate([Line(20m, 2)], new SaleBasis(100m, 90m, 0m));

        Assert.Equal(40m, amounts.Value);
        Assert.Equal(36m, amounts.Refund);
    }

    [Fact]
    public void TaxPaidIsRefundedInProportion()
    {
        // Subtotal 100, 16 % tax added, paid 116: returning 50 refunds 58.
        var amounts = ReturnRules.Calculate([Line(50m, 1)], new SaleBasis(100m, 116m, 0m));

        Assert.Equal(58m, amounts.Refund);
    }

    [Fact]
    public void TheRefundCannotExceedWhatIsLeft()
    {
        var amounts = ReturnRules.Calculate([Line(50m, 1)], new SaleBasis(100m, 100m, 80m));

        Assert.Equal(20m, amounts.Refund);
        Assert.True(amounts.IsCapped);
    }

    [Fact]
    public void ExchangeBalanceIsPositiveWhenTheCustomerPays()
    {
        Assert.Equal(15m, ReturnRules.ExchangeBalance(45m, 60m));
        Assert.Equal(-5m, ReturnRules.ExchangeBalance(45m, 40m));
        Assert.Equal(0m, ReturnRules.ExchangeBalance(40m, 40m));
    }

    [Fact]
    public void ReturnNumbersArePadded() => Assert.Equal("R-000007", ReturnRules.FormatReturnNumber(7));
}
