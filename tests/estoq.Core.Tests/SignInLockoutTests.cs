using Estoq.Core.SignIn;
using Xunit;

namespace Estoq.Core.Tests;

public class SignInLockoutTests
{
    private DateTimeOffset _now = new(2026, 11, 22, 9, 0, 0, TimeSpan.Zero);

    private SignInLockout Create() => new(() => _now);

    private static void Fail(SignInLockout lockout, int times)
    {
        for (var i = 0; i < times; i++)
        {
            lockout.RegisterFailure();
        }
    }

    [Fact]
    public void FourFailuresDoNotLock()
    {
        var lockout = Create();
        Fail(lockout, 4);
        Assert.False(lockout.IsLocked);
    }

    [Fact]
    public void FiveFailuresLockForOneMinute()
    {
        var lockout = Create();
        Fail(lockout, 5);
        Assert.True(lockout.IsLocked);
        Assert.Equal(TimeSpan.FromMinutes(1), lockout.Remaining);
    }

    [Fact]
    public void LockEndsAfterThePause()
    {
        var lockout = Create();
        Fail(lockout, 5);
        _now += TimeSpan.FromSeconds(59);
        Assert.True(lockout.IsLocked);
        _now += TimeSpan.FromSeconds(1);
        Assert.False(lockout.IsLocked);
    }

    [Fact]
    public void PauseDoublesEveryFiveFailuresUpToTheCap()
    {
        Assert.Equal(TimeSpan.FromMinutes(1), SignInLockout.PauseFor(5));
        Assert.Equal(TimeSpan.FromMinutes(2), SignInLockout.PauseFor(10));
        Assert.Equal(TimeSpan.FromMinutes(4), SignInLockout.PauseFor(15));
        Assert.Equal(TimeSpan.FromMinutes(8), SignInLockout.PauseFor(20));
        Assert.Equal(SignInLockout.MaxPause, SignInLockout.PauseFor(25));
        Assert.Equal(SignInLockout.MaxPause, SignInLockout.PauseFor(500));
    }

    [Fact]
    public void FailuresKeepCountingAcrossLocks()
    {
        var lockout = Create();
        Fail(lockout, 5);
        _now += TimeSpan.FromMinutes(1);
        Fail(lockout, 5);
        Assert.Equal(TimeSpan.FromMinutes(2), lockout.Remaining);
    }

    [Fact]
    public void SuccessResetsEverything()
    {
        var lockout = Create();
        Fail(lockout, 4);
        lockout.RegisterSuccess();
        Fail(lockout, 4);
        Assert.False(lockout.IsLocked);
        Fail(lockout, 1);
        Assert.Equal(TimeSpan.FromMinutes(1), lockout.Remaining);
    }
}
