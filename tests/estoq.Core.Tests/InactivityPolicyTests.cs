using Estoq.Core.SignIn;
using Xunit;

namespace Estoq.Core.Tests;

public class InactivityPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DefaultIsThreeMinutes() =>
        Assert.Equal(TimeSpan.FromMinutes(3), InactivityPolicy.DefaultTimeout);

    [Fact]
    public void DefaultIsWithinTheAllowedRange() =>
        Assert.True(InactivityPolicy.IsValidTimeout(InactivityPolicy.DefaultTimeout));

    [Theory]
    [InlineData(0, false)]
    [InlineData(30, false)]
    [InlineData(60, true)]
    [InlineData(600, true)]
    [InlineData(3600, true)]
    [InlineData(3601, false)]
    public void TimeoutMustBeBetweenOneAndSixtyMinutes(int seconds, bool expected) =>
        Assert.Equal(expected, InactivityPolicy.IsValidTimeout(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void ClampKeepsTheRange()
    {
        Assert.Equal(InactivityPolicy.MinTimeout, InactivityPolicy.Clamp(TimeSpan.Zero));
        Assert.Equal(InactivityPolicy.MaxTimeout, InactivityPolicy.Clamp(TimeSpan.FromHours(5)));
        Assert.Equal(TimeSpan.FromMinutes(10), InactivityPolicy.Clamp(TimeSpan.FromMinutes(10)));
    }

    [Theory]
    [InlineData(179, false)]
    [InlineData(180, true)]
    [InlineData(900, true)]
    public void ExpiresWhenTheTimeoutIsReached(int idleSeconds, bool expected) =>
        Assert.Equal(expected, InactivityPolicy.HasExpired(Start, Start.AddSeconds(idleSeconds), TimeSpan.FromMinutes(3)));
}
