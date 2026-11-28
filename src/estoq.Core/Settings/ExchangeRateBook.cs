using System.Globalization;
using Estoq.Core.Roles;

namespace Estoq.Core.Settings;

/// <summary>One recorded rate: units of the currency per one base unit, effective from a time, set by someone.</summary>
public sealed record RateEntry(string Currency, decimal Rate, DateTimeOffset EffectiveAt, string SetBy, DateTimeOffset RecordedAt);

/// <summary>
/// Exchange rate history. Append-only: a new rate is added with an effective time and never overwrites an old one
/// (docs/features/settings.md). Open to every role.
/// </summary>
public sealed class ExchangeRateBook
{
    public const decimal MaxRate = 1_000_000_000m;

    private readonly List<RateEntry> _entries = [];

    public ExchangeRateBook(string baseCurrency) => BaseCurrency = baseCurrency;

    public string BaseCurrency { get; }

    /// <summary>Every entry in the order it was recorded.</summary>
    public IReadOnlyList<RateEntry> Entries => _entries;

    /// <summary>Text such as 41.5 or 41,5 as a positive rate with at most six decimals.</summary>
    public static bool TryParseRate(string? text, IFormatProvider provider, out decimal rate)
    {
        rate = 0m;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        return decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, provider, out rate)
            && rate > 0m && rate <= MaxRate && decimal.Round(rate, 6) == rate;
    }

    /// <summary>Records a new rate. The base currency has no rate and cannot get one.</summary>
    public bool Append(string currency, decimal rate, DateTimeOffset effectiveAt, string setBy, DateTimeOffset recordedAt)
    {
        if (string.Equals(currency, BaseCurrency, StringComparison.OrdinalIgnoreCase) || rate <= 0m || rate > MaxRate)
        {
            return false;
        }

        _entries.Add(new RateEntry(currency.ToUpperInvariant(), rate, effectiveAt, setBy, recordedAt));
        return true;
    }

    /// <summary>The rate in force at <paramref name="at"/>: the latest effective entry not after that time (ties: recorded last).</summary>
    public RateEntry? RateAt(string currency, DateTimeOffset at) =>
        _entries
            .Select((entry, index) => (entry, index))
            .Where(item => string.Equals(item.entry.Currency, currency, StringComparison.OrdinalIgnoreCase) && item.entry.EffectiveAt <= at)
            .OrderBy(item => item.entry.EffectiveAt).ThenBy(item => item.index)
            .Select(item => item.entry)
            .LastOrDefault();

    /// <summary>All entries of a currency, newest effective time first.</summary>
    public IReadOnlyList<RateEntry> History(string currency) =>
        _entries
            .Select((entry, index) => (entry, index))
            .Where(item => string.Equals(item.entry.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.entry.EffectiveAt).ThenByDescending(item => item.index)
            .Select(item => item.entry)
            .ToList();

    /// <summary>Everyone may update rates; border-shop rates change often.</summary>
    public static bool CanUpdate(UserRole role) => RolePermissions.IsAllowed(role, Permission.UpdateExchangeRates);
}
