using System.Globalization;
using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>A currency the owner can pick: ISO code and English name (names come from Windows region data).</summary>
public sealed record CurrencyItem(string Code, string Name)
{
    public string Display => $"{Code} - {Name}";
}

/// <summary>Every currency known to the system, built once, sorted by code. USD is always present.</summary>
public static class CurrencyCatalog
{
    private static readonly Lazy<IReadOnlyList<CurrencyItem>> Items = new(Build);

    public static IReadOnlyList<CurrencyItem> All => Items.Value;

    public static CurrencyItem? Find(string? code) =>
        All.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<CurrencyItem> Build()
    {
        var items = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .Where(region => Currencies.IsValidCode(region.ISOCurrencySymbol))
            .GroupBy(region => region.ISOCurrencySymbol)
            .Select(group => new CurrencyItem(group.Key, group.First().CurrencyEnglishName))
            .ToList();

        if (items.All(item => item.Code != Currencies.Default))
        {
            items.Add(new CurrencyItem(Currencies.Default, "US Dollar"));
        }

        return items.OrderBy(item => item.Code, StringComparer.Ordinal).ToList();
    }
}
