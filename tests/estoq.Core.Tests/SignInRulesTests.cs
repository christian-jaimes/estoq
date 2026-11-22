using Estoq.Core.SignIn;
using Xunit;

namespace Estoq.Core.Tests;

public class SignInRulesTests
{
    [Theory]
    [InlineData(null, IdentifierKind.Empty)]
    [InlineData("", IdentifierKind.Empty)]
    [InlineData("   ", IdentifierKind.Empty)]
    [InlineData("4", IdentifierKind.Code)]
    [InlineData("004821", IdentifierKind.Code)]
    [InlineData("0048213", IdentifierKind.Code)]
    [InlineData("a", IdentifierKind.Email)]
    [InlineData("12a", IdentifierKind.Email)]
    [InlineData("ana@shop.com", IdentifierKind.Email)]
    public void ClassifiesInput(string? input, IdentifierKind expected) =>
        Assert.Equal(expected, SignInRules.Classify(input));

    [Fact]
    public void CodeInputIsCappedAtSixDigits() =>
        Assert.Equal("123456", SignInRules.Normalize("12345678"));

    [Fact]
    public void EmailInputIsKept() =>
        Assert.Equal("ana@shop.com", SignInRules.Normalize(" ana@shop.com "));

    [Theory]
    [InlineData("004821", true)]
    [InlineData("48212", false)]
    [InlineData("48212a", false)]
    [InlineData("", false)]
    public void OnlySixDigitsAreACompleteCode(string input, bool expected) =>
        Assert.Equal(expected, SignInRules.IsCompleteCode(input));

    [Theory]
    [InlineData("004821", null, true)]
    [InlineData("0048", null, false)]
    [InlineData("ana@shop.com", "secret", true)]
    [InlineData("ana@shop.com", "", false)]
    [InlineData("ana@shop.com", null, false)]
    [InlineData("", "secret", false)]
    public void SubmitNeedsACodeOrEmailWithPassword(string identifier, string? password, bool expected) =>
        Assert.Equal(expected, SignInRules.CanSubmit(identifier, password));
}
