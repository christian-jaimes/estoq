using System.Globalization;
using System.Text;

namespace Estoq.Core.Dashboard;

/// <summary>"Export report": a CSV of the figures on screen. Plain invariant numbers so any spreadsheet reads them.</summary>
public static class DashboardReport
{
    /// <summary>One row per figure: section, label, value. Quoting follows RFC 4180.</summary>
    public static string ToCsv(IEnumerable<(string Section, string Label, string Value)> rows)
    {
        var text = new StringBuilder();
        text.AppendLine("section,label,value");
        foreach (var (section, label, value) in rows)
        {
            text.AppendLine(string.Join(',', Escape(section), Escape(label), Escape(value)));
        }

        return text.ToString();
    }

    public static string Number(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
