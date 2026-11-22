using System.Globalization;
using System.Text;

namespace Estoq.Core.Products;

/// <summary>
/// The generated product name and text normalisation shared by the duplicate check and the code generators
/// (docs/features/add-product.md, steps 3 and 4).
/// </summary>
public static class ProductNaming
{
    /// <summary>Trims the text and collapses runs of white space to one space; null becomes empty.</summary>
    public static string Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// The name built from master data: "Hugo Boss T-shirt, Red graphic". Size and colour are never part of it.
    /// Empty when the product type or the model label is missing; a missing brand is simply left out.
    /// </summary>
    public static string GenerateName(string? brand, string? productType, string? modelLabel)
    {
        var type = Clean(productType);
        var model = Clean(modelLabel);
        if (type.Length == 0 || model.Length == 0)
        {
            return string.Empty;
        }

        var head = Clean($"{Clean(brand)} {type}");
        return $"{head}, {model}";
    }

    /// <summary>A comparison key: clean, accents removed, upper case. "  Red  Graphic " equals "red graphic".</summary>
    public static string Key(string? text)
    {
        var decomposed = Clean(text).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
