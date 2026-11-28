using System.Globalization;
using System.Text;

namespace Estoq.Core.Sales;

/// <summary>
/// Builds the CSV for the Export button: exactly the rows of the filtered table, with fixed (culture-independent) numbers and
/// ISO dates so the file reads the same in any spreadsheet. Headers and words come from the caller so they follow the app language.
/// </summary>
public static class SalesCsv
{
    /// <param name="headers">Receipt, date and time, staff, payment method, status, items, total, tax (the table order).</param>
    public static string Build(
        IEnumerable<SaleRecord> sales,
        IReadOnlyList<string> headers,
        Func<PaymentMethod, string> methodText,
        Func<SaleStatus, string> statusText)
    {
        var builder = new StringBuilder();
        builder.Append(string.Join(',', headers.Select(Cell))).Append("\r\n");

        foreach (var sale in sales)
        {
            var cells = new[]
            {
                Cell(sale.ReceiptNumber),
                Cell(sale.SoldAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
                Cell(sale.Staff),
                Cell(methodText(sale.PrimaryMethod)),
                Cell(statusText(sale.Status)),
                sale.ItemCount.ToString(CultureInfo.InvariantCulture),
                sale.Totals.Total.ToString("0.00", CultureInfo.InvariantCulture),
                sale.Totals.Tax.ToString("0.00", CultureInfo.InvariantCulture),
            };
            builder.Append(string.Join(',', cells)).Append("\r\n");
        }

        return builder.ToString();
    }

    /// <summary>Quotes a text cell and defuses spreadsheet formulas (a value starting with = + - or @).</summary>
    public static string Cell(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@')
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
