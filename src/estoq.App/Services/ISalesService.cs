using Estoq.Core.Sales;

namespace Estoq.App.Services;

/// <summary>
/// The saved sales the Sales page lists (docs/features/sales.md). The sample implementation builds a fixed history in memory;
/// the back end replaces it with one that reads sales, items and payments from the database. The Dashboard reads the same
/// service so its figures never disagree with this page.
/// </summary>
public interface ISalesService
{
    /// <summary>The currency every amount in <see cref="SaleRecord"/> is expressed in.</summary>
    string BaseCurrency { get; }

    /// <summary>Every sale in the shop. Role visibility is applied by <see cref="SalesHistory.Apply"/>; the back end also enforces it.</summary>
    IReadOnlyList<SaleRecord> GetSales();

    /// <summary>The staff who have made sales, for the staff filter.</summary>
    IReadOnlyList<string> GetStaffNames();

    /// <summary>Audit entry for an export: who exported how many rows and when. The sample implementation writes nothing.</summary>
    void RecordExport(int rowCount);
}
