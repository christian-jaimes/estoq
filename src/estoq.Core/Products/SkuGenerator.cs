using System.Text;

namespace Estoq.Core.Products;

/// <summary>Step 7: the automatic SKU per variant. The owner can override it; it must stay unique.</summary>
public static class SkuGenerator
{
    private const int PartLength = 3;
    private const string Vowels = "AEIOU";

    /// <summary>
    /// "T-shirt" + "White" + "S" gives "TS-WHT-S". Parts without a value are left out. When the SKU is already taken,
    /// "-2", "-3" and so on is added until it is free (comparison ignores case).
    /// </summary>
    public static string Generate(string? productType, string? colour, string? size, IReadOnlySet<string> taken)
    {
        ArgumentNullException.ThrowIfNull(taken);

        var parts = new List<string>(3);
        AddIfAny(parts, Abbreviate(productType));
        AddIfAny(parts, Abbreviate(colour));
        AddIfAny(parts, Compact(size));
        var baseSku = parts.Count == 0 ? "SKU" : string.Join('-', parts);

        var takenKeys = new HashSet<string>(taken.Select(s => s.Trim().ToUpperInvariant()), StringComparer.Ordinal);
        var candidate = baseSku;
        for (var n = 2; takenKeys.Contains(candidate); n++)
        {
            candidate = $"{baseSku}-{n}";
        }

        return candidate;
    }

    /// <summary>
    /// A short upper-case code for a name. Several words give their initials ("T-shirt" gives "TS");
    /// one word gives its first letter and then its consonants ("White" gives "WHT"). At most three characters.
    /// </summary>
    public static string Abbreviate(string? text)
    {
        var words = ProductNaming.Key(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(SplitAlphanumeric)
            .ToList();
        if (words.Count == 0)
        {
            return string.Empty;
        }

        if (words.Count > 1)
        {
            return new string(words.Take(PartLength).Select(w => w[0]).ToArray());
        }

        var word = words[0];
        var builder = new StringBuilder().Append(word[0]);
        foreach (var c in word.Skip(1))
        {
            if (builder.Length == PartLength)
            {
                break;
            }

            if (!Vowels.Contains(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string Compact(string? text) =>
        new(ProductNaming.Key(text).Where(char.IsAsciiLetterOrDigit).ToArray());

    private static void AddIfAny(List<string> parts, string part)
    {
        if (part.Length > 0)
        {
            parts.Add(part);
        }
    }

    private static IEnumerable<string> SplitAlphanumeric(string word)
    {
        var current = new StringBuilder();
        foreach (var c in word)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }
}
