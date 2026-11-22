using System.Net.Mail;

namespace Estoq.Core.Setup;

/// <summary>Field rules for the setup wizard. Each returns <see cref="ValidationCode.None"/> when the value is fine.</summary>
public static class SetupRules
{
    public const int MinPasswordLength = 8;

    public const long MaxLogoBytes = 2 * 1024 * 1024;

    private static readonly string[] LogoExtensions = [".png", ".jpg", ".jpeg"];

    public static ValidationCode ValidateRequired(string? value) =>
        string.IsNullOrWhiteSpace(value) ? ValidationCode.Required : ValidationCode.None;

    /// <summary>The owner must have an email (it also prepares for a future online version).</summary>
    public static ValidationCode ValidateOwnerEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? ValidationCode.Required : ValidateEmailFormat(email);

    /// <summary>Optional emails (the store's) may be empty but must be well formed when given.</summary>
    public static ValidationCode ValidateOptionalEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? ValidationCode.None : ValidateEmailFormat(email);

    public static ValidationCode ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return ValidationCode.Required;
        }

        return password.Length < MinPasswordLength ? ValidationCode.PasswordTooShort : ValidationCode.None;
    }

    public static ValidationCode ValidatePasswordConfirmation(string? password, string? confirmation)
    {
        if (string.IsNullOrEmpty(confirmation))
        {
            return ValidationCode.Required;
        }

        return string.Equals(password, confirmation, StringComparison.Ordinal)
            ? ValidationCode.None
            : ValidationCode.PasswordMismatch;
    }

    /// <summary>A logo must be PNG or JPG and at most 2 MB.</summary>
    public static ValidationCode ValidateLogo(string fileName, long sizeInBytes)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        var extension = Path.GetExtension(fileName);
        if (!LogoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationCode.LogoUnsupportedType;
        }

        return sizeInBytes > MaxLogoBytes ? ValidationCode.LogoTooLarge : ValidationCode.None;
    }

    private static ValidationCode ValidateEmailFormat(string email)
    {
        var value = email.Trim();
        if (value.Any(char.IsWhiteSpace) || !MailAddress.TryCreate(value, out var address) || address.Address != value)
        {
            return ValidationCode.InvalidEmail;
        }

        // A bare local@host parses, but a shop email needs a dotted domain.
        var host = address.Host;
        var dotted = host.Contains('.', StringComparison.Ordinal) && !host.StartsWith('.') && !host.EndsWith('.');
        return dotted ? ValidationCode.None : ValidationCode.InvalidEmail;
    }
}
