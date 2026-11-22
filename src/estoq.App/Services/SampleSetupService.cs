using System.Security.Cryptography;
using System.Text;
using Estoq.Core.Setup;

namespace Estoq.App.Services;

/// <summary>
/// Front-end implementation: validates with the Core rules and stores nothing, so the wizard shows again on the next launch.
/// The recovery key format is a placeholder until the encryption spike decides the real one (docs/technical/encryption.md).
/// </summary>
public sealed class SampleSetupService : ISetupService
{
    private readonly IAccountService _accounts;

    public SampleSetupService(IAccountService accounts) => _accounts = accounts;

    // No 0, 1, I, O: easy to read from paper.
    private const string KeyAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int KeyGroups = 6;
    private const int KeyGroupLength = 4;

    public bool IsSetupComplete { get; private set; }

    public string GenerateUserCode() => UserCode.Generate();

    public string GenerateRecoveryKey()
    {
        var key = new StringBuilder();
        for (var group = 0; group < KeyGroups; group++)
        {
            if (group > 0)
            {
                key.Append('-');
            }

            for (var i = 0; i < KeyGroupLength; i++)
            {
                key.Append(KeyAlphabet[RandomNumberGenerator.GetInt32(KeyAlphabet.Length)]);
            }
        }

        return key.ToString();
    }

    public Task<ValidationCode> CompleteAsync(SetupRequest request, string ownerPassword)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidationCode[] checks =
        [
            SetupRules.ValidateRequired(request.OwnerName),
            SetupRules.ValidateOwnerEmail(request.OwnerEmail),
            SetupRules.ValidatePassword(ownerPassword),
            SetupRules.ValidateRequired(request.StoreName),
            Currencies.IsValidCode(request.BaseCurrency) ? ValidationCode.None : ValidationCode.InvalidCurrency,
            SetupRules.ValidateOptionalEmail(request.StoreEmail),
            UserCode.IsValid(request.UserCode) ? ValidationCode.None : ValidationCode.Required,
        ];

        var error = checks.FirstOrDefault(code => code != ValidationCode.None);
        if (error == ValidationCode.None)
        {
            _accounts.RegisterOwner(request.OwnerName, request.UserCode, request.OwnerEmail, ownerPassword);
            IsSetupComplete = true;
        }

        return Task.FromResult(error);
    }
}
