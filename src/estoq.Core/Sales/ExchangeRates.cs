namespace Estoq.Core.Sales;

/// <summary>
/// Rates for one sale: how many units of each currency equal one unit of the base currency (decision 13).
/// Prices are stored in the base currency and converted at these rates; the rates are snapshotted on the sale.
/// </summary>
public sealed class ExchangeRates
{
    private readonly Dictionary<string, decimal> _perBase;

    public ExchangeRates(string baseCurrency, IReadOnlyDictionary<string, decimal> perBase)
    {
        ArgumentNullException.ThrowIfNull(perBase);
        BaseCurrency = baseCurrency;
        _perBase = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, rate) in perBase)
        {
            if (rate <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(perBase), rate, "A rate must be greater than zero.");
            }

            if (!string.Equals(code, baseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                _perBase[code] = rate;
            }
        }
    }

    public string BaseCurrency { get; }

    /// <summary>True for the base currency and for every currency that has a rate.</summary>
    public bool Supports(string currency) => IsBase(currency) || _perBase.ContainsKey(currency);

    /// <summary>Units of <paramref name="currency"/> per one base unit; 1 for the base currency.</summary>
    public decimal RateOf(string currency)
    {
        if (IsBase(currency))
        {
            return 1m;
        }

        return _perBase.TryGetValue(currency, out var rate)
            ? rate
            : throw new KeyNotFoundException($"No exchange rate for {currency}.");
    }

    /// <summary>A base amount shown in <paramref name="currency"/>.</summary>
    public decimal FromBase(decimal baseAmount, string currency) => Money.Round(baseAmount * RateOf(currency));

    /// <summary>An amount in <paramref name="currency"/> as base currency.</summary>
    public decimal ToBase(decimal amount, string currency) => Money.Round(amount / RateOf(currency));

    /// <summary>
    /// The smallest amount in <paramref name="currency"/> that, converted back, covers <paramref name="baseAmount"/>.
    /// Rounded up so paying the suggested amount never leaves the sale short.
    /// </summary>
    public decimal AmountToCover(decimal baseAmount, string currency)
    {
        var amount = Money.RoundUp(baseAmount * RateOf(currency));
        while (ToBase(amount, currency) < baseAmount)
        {
            amount += 0.01m;
        }

        return amount;
    }

    private bool IsBase(string currency) => string.Equals(currency, BaseCurrency, StringComparison.OrdinalIgnoreCase);
}
