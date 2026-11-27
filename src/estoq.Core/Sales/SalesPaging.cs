namespace Estoq.Core.Sales;

/// <summary>Page arithmetic for the sales table and its pager ("Showing 1-7 of 126 sales").</summary>
public static class SalesPaging
{
    /// <summary>Marks the gap in <see cref="PageNumbers"/> where the pager shows an ellipsis.</summary>
    public const int Gap = 0;

    public static int PageCount(int total, int pageSize) =>
        pageSize <= 0 ? 1 : Math.Max(1, (total + pageSize - 1) / pageSize);

    /// <summary>A page number inside 1..<paramref name="pageCount"/>.</summary>
    public static int Clamp(int page, int pageCount) => Math.Min(Math.Max(page, 1), Math.Max(pageCount, 1));

    /// <summary>The first and last row (1-based, inclusive) on a page; (0, 0) when there are no rows.</summary>
    public static (int First, int Last) RowRange(int page, int pageSize, int total)
    {
        if (total <= 0)
        {
            return (0, 0);
        }

        var first = ((page - 1) * pageSize) + 1;
        return (first, Math.Min(page * pageSize, total));
    }

    /// <summary>
    /// The numbers the pager shows: first, last, and the pages next to the current one, with <see cref="Gap"/> where pages are skipped.
    /// For 18 pages and page 2: 1 2 3 Gap 18.
    /// </summary>
    public static IReadOnlyList<int> PageNumbers(int pageCount, int current)
    {
        var shown = new SortedSet<int> { 1, pageCount, current - 1, current, current + 1 };
        shown.RemoveWhere(page => page < 1 || page > pageCount);

        // With the first page current, show one more page so the pager keeps its width.
        if (current == 1 && pageCount >= 3)
        {
            shown.Add(3);
        }

        var result = new List<int>();
        var previous = 0;
        foreach (var page in shown)
        {
            if (previous != 0 && page - previous > 1)
            {
                // A single skipped page is shown as the page itself; an ellipsis would hide nothing.
                result.Add(page - previous == 2 ? previous + 1 : Gap);
            }

            result.Add(page);
            previous = page;
        }

        return result;
    }
}
