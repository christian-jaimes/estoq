using System.Text;

namespace Estoq.Core.Inventory;

/// <summary>Writes comma-separated text the way spreadsheets read it (docs/features/inventory.md: Export).</summary>
public static class CsvText
{
    /// <summary>One header line then one line per row, each ending in a line break. Fields are quoted only when needed.</summary>
    public static string Build(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        var builder = new StringBuilder();
        AppendLine(builder, headers);
        foreach (var row in rows)
        {
            AppendLine(builder, row);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Quotes a field containing a comma, quote or line break. Text that starts with =, + or @ is prefixed with an
    /// apostrophe so a spreadsheet shows it as text instead of running it as a formula.
    /// </summary>
    public static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Length > 0 && text[0] is '=' or '+' or '@')
        {
            text = "'" + text;
        }

        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : text;
    }

    private static void AppendLine(StringBuilder builder, IReadOnlyList<string> fields)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(Escape(fields[index]));
        }

        builder.Append("\r\n");
    }
}
