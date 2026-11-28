using Estoq.Core.Dashboard;

namespace Estoq.App.Services;

/// <summary>The transaction and stock data behind the dashboard. Every figure is computed from it in Estoq.Core, never here.</summary>
public interface IDashboardService
{
    /// <summary>Every sale line and return line on record, any date. Read again on each call, so a sale rung up a moment ago shows.</summary>
    IReadOnlyList<DashboardLine> LoadLines();

    /// <summary>Units on hand per article, summed across locations.</summary>
    IReadOnlyList<StockOnHand> LoadStock();
}
