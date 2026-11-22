namespace Estoq.Core.Setup;

/// <summary>
/// Why a value is rejected. Codes, not messages, so the UI can show them in the user's language.
/// </summary>
public enum ValidationCode
{
    /// <summary>The value is acceptable.</summary>
    None,
    Required,
    InvalidEmail,
    PasswordTooShort,
    PasswordMismatch,
    InvalidTaxRate,
    InvalidCurrency,
    LogoUnsupportedType,
    LogoTooLarge,
}
