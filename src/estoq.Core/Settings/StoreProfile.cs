using System.Globalization;
using Estoq.Core.Setup;

namespace Estoq.Core.Settings;

/// <summary>The store details the owner and managers edit in Settings.</summary>
public sealed record StoreProfile(
    string Name,
    string TaxId,
    string Address,
    string City,
    string PostalCode,
    string? CountryCode,
    string Phone,
    string Email,
    string BaseCurrency,
    IReadOnlyList<string> AcceptedCurrencies,
    TaxRate TaxRate,
    TaxMode TaxMode,
    AppLanguage StoreLanguage,
    string? LogoPath = null)
{
    /// <summary>Flat field map used to audit changes; field names double as resource key suffixes.</summary>
    public IReadOnlyDictionary<string, string> ToFields() => new Dictionary<string, string>
    {
        ["Name"] = Name,
        ["TaxId"] = TaxId,
        ["Address"] = Address,
        ["City"] = City,
        ["PostalCode"] = PostalCode,
        ["Country"] = CountryCode ?? string.Empty,
        ["Phone"] = Phone,
        ["Email"] = Email,
        ["BaseCurrency"] = BaseCurrency,
        ["AcceptedCurrencies"] = string.Join(", ", AcceptedCurrencies),
        ["TaxRate"] = TaxRate.Percent.ToString("0.##", CultureInfo.InvariantCulture),
        ["TaxMode"] = TaxMode.ToString(),
        ["StoreLanguage"] = StoreLanguage.ToString(),
        ["Logo"] = LogoPath is null ? string.Empty : Path.GetFileName(LogoPath),
    };
}

public static class StoreRules
{
    public static ValidationCode ValidateName(string? name) => SetupRules.ValidateRequired(name);

    public static ValidationCode ValidateEmail(string? email) => SetupRules.ValidateOptionalEmail(email);

    /// <summary>The first problem found in a store profile, or <see cref="ValidationCode.None"/>.</summary>
    public static ValidationCode Validate(StoreProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ValidationCode[] checks =
        [
            ValidateName(profile.Name),
            ValidateEmail(profile.Email),
            Currencies.IsValidCode(profile.BaseCurrency) ? ValidationCode.None : ValidationCode.InvalidCurrency,
        ];
        return checks.FirstOrDefault(code => code != ValidationCode.None);
    }
}
