namespace Estoq.Core.Dashboard;

/// <summary>The date ranges the dashboard offers (docs/features/dashboard.md). The default is <see cref="ThisMonth"/>.</summary>
public enum DashboardRange
{
    Today,
    Last7Days,
    Last30Days,
    ThisMonth,
    LastMonth,
}

/// <summary>An inclusive range of calendar days.</summary>
public readonly record struct DatePeriod(DateOnly Start, DateOnly End)
{
    /// <summary>Number of days, counting both ends.</summary>
    public int Days => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateTime moment) => Contains(DateOnly.FromDateTime(moment));

    public bool Contains(DateOnly day) => day >= Start && day <= End;

    /// <summary>The period of the same length that ends the day before this one starts; what the "vs previous period" change compares to.</summary>
    public DatePeriod Previous() => new(Start.AddDays(-Days), Start.AddDays(-1));

    public static DatePeriod Resolve(DashboardRange range, DateOnly today) => range switch
    {
        DashboardRange.Today => new DatePeriod(today, today),
        DashboardRange.Last7Days => new DatePeriod(today.AddDays(-6), today),
        DashboardRange.Last30Days => new DatePeriod(today.AddDays(-29), today),
        DashboardRange.ThisMonth => new DatePeriod(new DateOnly(today.Year, today.Month, 1), today),
        DashboardRange.LastMonth => LastMonth(today),
        _ => throw new ArgumentOutOfRangeException(nameof(range), range, "Unknown range."),
    };

    private static DatePeriod LastMonth(DateOnly today)
    {
        var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        return new DatePeriod(first, first.AddMonths(1).AddDays(-1));
    }
}
