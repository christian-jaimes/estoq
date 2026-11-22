using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class CurrenciesTests
{
    [Fact]
    public void DefaultIsTheUsDollar() => Assert.Equal("USD", Currencies.Default);

    [Theory]
    [InlineData("USD", true)]
    [InlineData("VES", true)]
    [InlineData("usd", false)]
    [InlineData("US", false)]
    [InlineData("USDX", false)]
    [InlineData("U5D", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void CodeFormat(string? code, bool expected) =>
        Assert.Equal(expected, Currencies.IsValidCode(code));

    [Fact]
    public void AcceptedKeepsOrderAndDropsBaseDuplicatesAndInvalid()
    {
        var result = Currencies.NormalizeAccepted("USD", ["eur", "COP", "USD", "EUR", "x", " ves ", ""]);
        Assert.Equal(["EUR", "COP", "VES"], result);
    }

    [Fact]
    public void AcceptedFollowsTheChosenBase()
    {
        var result = Currencies.NormalizeAccepted("EUR", ["USD", "EUR", "COP"]);
        Assert.Equal(["USD", "COP"], result);
    }

    [Fact]
    public void EmptyAcceptedIsFine() => Assert.Empty(Currencies.NormalizeAccepted("USD", []));

    [Fact]
    public void TaxIsExcludedByDefault() => Assert.Equal(TaxMode.Excluded, default(TaxMode));
}
