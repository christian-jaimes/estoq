using System.Text;

namespace Estoq.App.Services;

/// <summary>Reads and appends the development CSV fixtures in <c>data/</c> (the source copy when run from a checkout).</summary>
internal static class SampleCsv
{
    public static IReadOnlyList<Dictionary<string, string>> Read(string fileName)
    {
        var path = ResolvePath(fileName);
        if (!File.Exists(path))
        {
            return [];
        }

        var rows = File.ReadAllLines(path);
        if (rows.Length < 2)
        {
            return [];
        }

        var headers = ParseRow(rows[0]);
        var result = new List<Dictionary<string, string>>();
        foreach (var row in rows.Skip(1).Where(row => !string.IsNullOrWhiteSpace(row)))
        {
            var values = ParseRow(row);
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                var header = headers[index].Trim();
                if (header.Length == 0 || fields.ContainsKey(header))
                {
                    continue;
                }

                fields.Add(header, index < values.Length ? values[index].Trim() : string.Empty);
            }

            result.Add(fields);
        }

        return result;
    }

    public static string Get(IReadOnlyDictionary<string, string> row, string column) =>
        row.TryGetValue(column, out var value) ? value : string.Empty;

    /// <summary>Appends rows after the last line, whether or not the file ends with a line break.</summary>
    public static void Append(string fileName, IReadOnlyList<string> lines)
    {
        var path = ResolvePath(fileName);
        if (lines.Count == 0 || !File.Exists(path))
        {
            return;
        }

        var text = File.ReadAllText(path);
        var prefix = text.Length == 0 || text.EndsWith('\n') ? string.Empty : Environment.NewLine;
        File.AppendAllText(path, prefix + string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    public static string Escape(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n')
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;

    private static string ResolvePath(string fileName)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var sourcePath = Path.Combine(directory.FullName, "data", fileName);
            if ((Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git"))) && File.Exists(sourcePath))
            {
                return sourcePath;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "data", fileName);
    }

    private static string[] ParseRow(string row)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < row.Length; i++)
        {
            var character = row[i];
            if (character == '"')
            {
                if (quoted && i + 1 < row.Length && row[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        values.Add(value.ToString());
        return [.. values];
    }
}
