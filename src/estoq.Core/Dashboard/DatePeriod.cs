using Estoq.Core.Sales;

namespace Estoq.Core.Dashboard;

/// <summary>An inclusive range of calendar days.</summary>
public readonly record struct DatePeriod(DateOnly Start, DateOnly End)
{
    /// <summary>Number of days, counting both ends.</summary>
    public int Days => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateTime moment) => Contains(DateOnly.FromDateTime(moment));

    public bool Contains(DateOnly day) => day >= Start && day <= End;

    /// <summary>The period of the same length that ends the day before this one starts; what the "vs previous period" change compares to.</summary>
    public DatePeriod Previous() => new(Start.AddDays(-Days), Start.AddDays(-1));

    /// <summary>
    /// The days to report for a picked range. The end stops at today, because days to come have no sales and would only flatten the chart
    /// (this month, this week and this year all end in the future). A range that starts after today is that single day.
    /// </summary>
    public static DatePeriod From(DateRange range, DateOnly today) =>
        new(range.From, range.To < today ? range.To : range.From > today ? range.From : today);
}
