using Estoq.Core.Roles;

namespace Estoq.Core.Sales;

/// <summary>The period tabs of the Sales page.</summary>
public enum SalesPeriod
{
    All,
    Today,
    ThisWeek,
    ThisMonth,
    Custom,
}

/// <summary>What the filter bar and period tabs ask for. Null or empty means "no restriction".</summary>
public sealed record SalesFilter(
    SalesPeriod Period = SalesPeriod.All,
    DateOnly? CustomFrom = null,
    DateOnly? CustomTo = null,
    string? Text = null,
    PaymentMethod? Method = null,
    string? Staff = null,
    SaleStatus? Status = null);

/// <summary>The four KPI cards. Voided and returned sales are not revenue, so they are left out of every figure.</summary>
public readonly record struct SalesSummary(decimal TotalSales, int Transactions, int ItemsSold, decimal AverageSale)
{
    public static SalesSummary Of(IEnumerable<SaleRecord> sales)
    {
        var counted = sales.Where(sale => sale.Status == SaleStatus.Completed).ToList();
        var total = counted.Sum(sale => sale.Totals.Total);
        var average = counted.Count == 0 ? 0m : Money.Round(total / counted.Count);
        return new SalesSummary(total, counted.Count, counted.Sum(sale => sale.ItemCount), average);
    }
}

/// <summary>Period ranges, filtering and role visibility for the sales list.</summary>
public static class SalesHistory
{
    /// <summary>The first and last day (inclusive) a period covers; null for an open end. Weeks start on Monday.</summary>
    public static (DateOnly? From, DateOnly? To) RangeOf(SalesPeriod period, DateOnly today, DateOnly? customFrom, DateOnly? customTo)
    {
        switch (period)
        {
            case SalesPeriod.Today:
                return (today, today);
            case SalesPeriod.ThisWeek:
                var offset = ((int)today.DayOfWeek + 6) % 7;
                var monday = today.AddDays(-offset);
                return (monday, monday.AddDays(6));
            case SalesPeriod.ThisMonth:
                var first = new DateOnly(today.Year, today.Month, 1);
                return (first, first.AddMonths(1).AddDays(-1));
            case SalesPeriod.Custom:
                if (customFrom is { } from && customTo is { } to && from > to)
                {
                    return (to, from);
                }

                return (customFrom, customTo);
            default:
                return (null, null);
        }
    }

    /// <summary>
    /// The sales a user may see, filtered, newest first. Cashiers see only their own sales; managers and owners see the shop
    /// (docs/features/sales.md). A role with no access to the sales history sees nothing.
    /// </summary>
    public static IReadOnlyList<SaleRecord> Apply(
        IEnumerable<SaleRecord> sales,
        SalesFilter filter,
        UserRole role,
        string currentStaff,
        DateOnly today)
    {
        var access = RolePermissions.GetAccess(role, Permission.ViewSalesHistory);
        if (access == Access.No)
        {
            return [];
        }

        var (from, to) = RangeOf(filter.Period, today, filter.CustomFrom, filter.CustomTo);
        var text = filter.Text?.Trim();

        return sales
            .Where(sale => access != Access.Own || string.Equals(sale.Staff, currentStaff, StringComparison.OrdinalIgnoreCase))
            .Where(sale => from is null || DateOnly.FromDateTime(sale.SoldAt) >= from)
            .Where(sale => to is null || DateOnly.FromDateTime(sale.SoldAt) <= to)
            .Where(sale => filter.Method is null || sale.Payments.Any(payment => payment.Method == filter.Method))
            .Where(sale => string.IsNullOrEmpty(filter.Staff) || string.Equals(sale.Staff, filter.Staff, StringComparison.OrdinalIgnoreCase))
            .Where(sale => filter.Status is null || sale.Status == filter.Status)
            .Where(sale => string.IsNullOrEmpty(text) || Matches(sale, text))
            .OrderByDescending(sale => sale.SoldAt)
            .ToList();
    }

    /// <summary>Search in sales: receipt number, staff name, or a product name or SKU.</summary>
    public static bool Matches(SaleRecord sale, string text) =>
        sale.ReceiptNumber.Contains(text, StringComparison.OrdinalIgnoreCase)
        || sale.Staff.Contains(text, StringComparison.OrdinalIgnoreCase)
        || sale.Lines.Any(line =>
            line.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
            || line.Sku.Contains(text, StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether items can still be returned from the sale. Return window checks come with the settings.</summary>
    public static bool CanReturn(SaleRecord sale) => sale.Status == SaleStatus.Completed;
}
