using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>Everything the owner entered in the setup wizard, except the password.</summary>
public sealed record SetupRequest(
    string OwnerName,
    string OwnerEmail,
    string UserCode,
    string StoreName,
    string TaxId,
    string Address,
    string City,
    string PostalCode,
    string? CountryCode,
    string Phone,
    string StoreEmail,
    string? LogoPath,
    string BaseCurrency,
    IReadOnlyList<string> AcceptedCurrencies,
    TaxRate TaxRate,
    TaxMode TaxMode,
    AppLanguage AppLanguage,
    AppLanguage StoreLanguage);

/// <summary>
/// First-run setup. The sample implementation only validates; the back end replaces it with one that creates
/// the database, the owner account and the store, and stores the recovery key material (docs/features/setup-wizard.md).
/// </summary>
public interface ISetupService
{
    /// <summary>True once an owner exists. Decides between the wizard and sign-in on launch.</summary>
    bool IsSetupComplete { get; }

    /// <summary>A new six-digit user code for the owner.</summary>
    string GenerateUserCode();

    /// <summary>A new recovery key, shown once on the last step.</summary>
    string GenerateRecoveryKey();

    /// <summary>Completes setup. Returns <see cref="ValidationCode.None"/> when accepted, otherwise the first problem found.</summary>
    Task<ValidationCode> CompleteAsync(SetupRequest request, string ownerPassword);
}
