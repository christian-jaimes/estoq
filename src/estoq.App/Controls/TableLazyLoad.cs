using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace Estoq.App.Controls;

/// <summary>Load on scroll for the standard list table (no pager): adds the next batch of rows while the end of the table is near.</summary>
public static class TableLazyLoad
{
    private const double NearEnd = 240;

    /// <summary>Loads the next batch when the end is near or the rows do not fill the view yet, then checks again once the new rows are laid out.</summary>
    public static void LoadMoreIfNeeded(ScrollViewer scroll, Func<bool> hasMore, Action loadMore)
    {
        if (!hasMore() || scroll.ScrollableHeight - scroll.VerticalOffset > NearEnd)
        {
            return;
        }

        loadMore();

        // A tall window may need more than one batch.
        scroll.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => LoadMoreIfNeeded(scroll, hasMore, loadMore));
    }
}
