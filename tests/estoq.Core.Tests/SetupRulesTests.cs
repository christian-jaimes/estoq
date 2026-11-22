using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class SetupRulesTests
{
    [Theory]
    [InlineData(null, ValidationCode.Required)]
    [InlineData("", ValidationCode.Required)]
    [InlineData("   ", ValidationCode.Required)]
    [InlineData("Urban Wear", ValidationCode.None)]
    public void RequiredText(string? value, ValidationCode expected) =>
        Assert.Equal(expected, SetupRules.ValidateRequired(value));

    [Theory]
    [InlineData(null, ValidationCode.Required)]
    [InlineData("", ValidationCode.Required)]
    [InlineData("owner", ValidationCode.InvalidEmail)]
    [InlineData("owner@host", ValidationCode.InvalidEmail)]
    [InlineData("owner@.com", ValidationCode.InvalidEmail)]
    [InlineData("owner@host.", ValidationCode.InvalidEmail)]
    [InlineData("two words@host.com", ValidationCode.InvalidEmail)]
    [InlineData("Owner <owner@host.com>", ValidationCode.InvalidEmail)]
    [InlineData("owner@host.com", ValidationCode.None)]
    [InlineData("  owner@host.com ", ValidationCode.None)]
    public void OwnerEmailIsRequiredAndWellFormed(string? email, ValidationCode expected) =>
        Assert.Equal(expected, SetupRules.ValidateOwnerEmail(email));

    [Theory]
    [InlineData(null, ValidationCode.None)]
    [InlineData("", ValidationCode.None)]
    [InlineData("shop", ValidationCode.InvalidEmail)]
    [InlineData("shop@urbanwear.com", ValidationCode.None)]
    public void StoreEmailIsOptionalButWellFormed(string? email, ValidationCode expected) =>
        Assert.Equal(expected, SetupRules.ValidateOptionalEmail(email));

    [Theory]
    [InlineData(null, ValidationCode.Required)]
    [InlineData("", ValidationCode.Required)]
    [InlineData("short", ValidationCode.PasswordTooShort)]
    [InlineData("1234567", ValidationCode.PasswordTooShort)]
    [InlineData("12345678", ValidationCode.None)]
    public void PasswordLength(string? password, ValidationCode expected) =>
        Assert.Equal(expected, SetupRules.ValidatePassword(password));

    [Theory]
    [InlineData("secret-pass", null, ValidationCode.Required)]
    [InlineData("secret-pass", "", ValidationCode.Required)]
    [InlineData("secret-pass", "secret-Pass", ValidationCode.PasswordMismatch)]
    [InlineData("secret-pass", "secret-pass", ValidationCode.None)]
    public void PasswordConfirmation(string password, string? confirmation, ValidationCode expected) =>
        Assert.Equal(expected, SetupRules.ValidatePasswordConfirmation(password, confirmation));

    [Theory]
    [InlineData("logo.png", 1_000, ValidationCode.None)]
    [InlineData("LOGO.JPG", 1_000, ValidationCode.None)]
    [InlineData("logo.jpeg", 2_097_152, ValidationCode.None)]
    [InlineData("logo.png", 2_097_153, ValidationCode.LogoTooLarge)]
    [InlineData("logo.gif", 1_000, ValidationCode.LogoUnsupportedType)]
    [InlineData("logo", 1_000, ValidationCode.LogoUnsupportedType)]
    public void LogoTypeAndSize(string fileName, long size, ValidationCode expected) =>
        Assert.Equal(expected, SetupRules.ValidateLogo(fileName, size));
}
