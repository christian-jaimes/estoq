namespace Estoq.Core.Sales;

/// <summary>The shortcuts of the date picker's "Quick select" list.</summary>
public enum QuickRange
{
    Today,
    ThisWeek,
    ThisMonth,
    ThisYear,
}

/// <summary>A range of days, both ends included. <see cref="Create"/> puts the ends in order, so From is never after To.</summary>
public readonly record struct DateRange(DateOnly From, DateOnly To)
{
    public static DateRange Create(DateOnly first, DateOnly second) =>
        first <= second ? new DateRange(first, second) : new DateRange(second, first);

    public bool Contains(DateOnly day) => day >= From && day <= To;

    /// <summary>The days a shortcut covers around <paramref name="today"/>. Weeks start on Monday.</summary>
    public static DateRange Of(QuickRange quick, DateOnly today)
    {
        switch (quick)
        {
            case QuickRange.ThisWeek:
                var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                return new DateRange(monday, monday.AddDays(6));
            case QuickRange.ThisMonth:
                var first = new DateOnly(today.Year, today.Month, 1);
                return new DateRange(first, first.AddMonths(1).AddDays(-1));
            case QuickRange.ThisYear:
                return new DateRange(new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31));
            default:
                return new DateRange(today, today);
        }
    }

    /// <summary>The shortcut whose range equals <paramref name="range"/> today, or null for any other range (the list highlights it).</summary>
    public static QuickRange? Match(DateRange? range, DateOnly today)
    {
        if (range is null)
        {
            return null;
        }

        foreach (var quick in Enum.GetValues<QuickRange>())
        {
            if (Of(quick, today) == range)
            {
                return quick;
            }
        }

        return null;
    }
}
