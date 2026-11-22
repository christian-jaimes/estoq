using System.Globalization;
using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class TaxRateTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CO");

    [Fact]
    public void DefaultIsZero()
    {
        Assert.Equal(0m, TaxRate.Default.Percent);
        Assert.Equal(0m, TaxRate.Default.Fraction);
        Assert.Equal(default, TaxRate.Default);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(16, true)]
    [InlineData(7.25, true)]
    [InlineData(100, true)]
    [InlineData(-0.01, false)]
    [InlineData(100.01, false)]
    [InlineData(7.255, false)]
    public void RangeAndPrecision(double value, bool valid) =>
        Assert.Equal(valid, TaxRate.TryCreate((decimal)value, out _));

    [Theory]
    [InlineData("0", 0)]
    [InlineData("16", 16)]
    [InlineData(" 7.25 % ", 7.25)]
    [InlineData("19%", 19)]
    public void ParsesEnglishText(string text, double expected)
    {
        Assert.True(TaxRate.TryParse(text, English, out var rate));
        Assert.Equal((decimal)expected, rate.Percent);
    }

    [Fact]
    public void ParsesSpanishDecimalComma()
    {
        Assert.True(TaxRate.TryParse("16,5", Spanish, out var rate));
        Assert.Equal(16.5m, rate.Percent);
        Assert.Equal(0.165m, rate.Fraction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("101")]
    [InlineData("1e2")]
    public void RejectsBadText(string? text)
    {
        Assert.False(TaxRate.TryParse(text, English, out _));
        Assert.Equal(ValidationCode.InvalidTaxRate, TaxRate.Validate(text, English));
    }

    [Fact]
    public void ValidTextHasNoError() =>
        Assert.Equal(ValidationCode.None, TaxRate.Validate("0", English));
}
