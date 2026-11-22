using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class UserCodeTests
{
    [Theory]
    [InlineData(0, "000000")]
    [InlineData(4821, "004821")]
    [InlineData(999_999, "999999")]
    public void KeepsLeadingZeros(int value, string expected) =>
        Assert.Equal(expected, UserCode.Generate(_ => value));

    [Fact]
    public void RandomCodesAreAlwaysSixDigits()
    {
        for (var i = 0; i < 500; i++)
        {
            Assert.True(UserCode.IsValid(UserCode.Generate()));
        }
    }

    [Fact]
    public void RandomSourceIsAskedForTheFullRange()
    {
        var bound = 0;
        _ = UserCode.Generate(limit =>
        {
            bound = limit;
            return 1;
        });
        Assert.Equal(1_000_000, bound);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_000)]
    public void RejectsOutOfRangeSource(int value) =>
        Assert.Throws<InvalidOperationException>(() => UserCode.Generate(_ => value));

    [Theory]
    [InlineData("123456", true)]
    [InlineData("000000", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12345a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ValidatesFormat(string? code, bool expected) =>
        Assert.Equal(expected, UserCode.IsValid(code));
}
