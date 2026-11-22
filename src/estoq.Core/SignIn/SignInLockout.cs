namespace Estoq.Core.SignIn;

/// <summary>
/// Slows down guessing. Five wrong attempts in a row lock sign-in for one minute; each further five double the pause,
/// up to fifteen minutes. A success resets it. Applies to the whole sign-in screen, never to one account, so it
/// reveals nothing about which codes exist. The curve is a working default (docs/open-questions.md, R6).
/// </summary>
public sealed class SignInLockout
{
    public const int AttemptsPerBatch = 5;

    public static readonly TimeSpan FirstPause = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaxPause = TimeSpan.FromMinutes(15);

    private readonly Func<DateTimeOffset> _now;
    private int _failures;
    private DateTimeOffset _lockedUntil = DateTimeOffset.MinValue;

    public SignInLockout(Func<DateTimeOffset>? now = null) => _now = now ?? (() => DateTimeOffset.UtcNow);

    /// <summary>Time left in the current lock; zero when sign-in is open.</summary>
    public TimeSpan Remaining
    {
        get
        {
            var left = _lockedUntil - _now();
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }

    public bool IsLocked => Remaining > TimeSpan.Zero;

    /// <summary>Counts a wrong attempt and starts a lock when a batch of five is complete.</summary>
    public void RegisterFailure()
    {
        _failures++;
        if (_failures % AttemptsPerBatch == 0)
        {
            _lockedUntil = _now() + PauseFor(_failures);
        }
    }

    public void RegisterSuccess()
    {
        _failures = 0;
        _lockedUntil = DateTimeOffset.MinValue;
    }

    /// <summary>The lock that follows the given total of consecutive failures (a multiple of five).</summary>
    public static TimeSpan PauseFor(int failures)
    {
        var batch = Math.Max(1, failures / AttemptsPerBatch);
        var seconds = FirstPause.TotalSeconds * Math.Pow(2, Math.Min(batch - 1, 10));
        return TimeSpan.FromSeconds(Math.Min(seconds, MaxPause.TotalSeconds));
    }
}
