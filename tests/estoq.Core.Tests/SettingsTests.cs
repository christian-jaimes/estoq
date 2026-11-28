using Estoq.Core.Roles;
using Estoq.Core.Settings;
using Estoq.Core.Setup;
using Xunit;

namespace Estoq.Core.Tests;

public class SettingsSectionsTests
{
    [Fact]
    public void CashierSeesOnlyAccountAndExchangeRates() =>
        Assert.Equal([SettingsSection.Account, SettingsSection.ExchangeRates], SettingsSections.Available(UserRole.Cashier));

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Owner)]
    public void ManagerAndOwnerSeeEverything(UserRole role) =>
        Assert.Equal(4, SettingsSections.Available(role).Count);

    [Fact]
    public void OnlyOwnerAndManagerSeeHistory()
    {
        Assert.True(SettingsSections.CanSeeHistory(UserRole.Owner));
        Assert.True(SettingsSections.CanSeeHistory(UserRole.Manager));
        Assert.False(SettingsSections.CanSeeHistory(UserRole.Cashier));
    }
}

public class SettingsDiffTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReportsOnlyChangedFieldsWithOldAndNewValue()
    {
        var before = new Dictionary<string, string> { ["Name"] = "Shop", ["Phone"] = "1" };
        var after = new Dictionary<string, string> { ["Name"] = "Shop", ["Phone"] = "2" };

        var change = Assert.Single(SettingsDiff.Compare(SettingsSection.Store, before, after, "Ana", Now));

        Assert.Equal("Phone", change.Field);
        Assert.Equal("1", change.OldValue);
        Assert.Equal("2", change.NewValue);
        Assert.Equal("Ana", change.User);
        Assert.Equal(Now, change.At);
    }

    [Fact]
    public void NoChangeGivesNoEntries() =>
        Assert.Empty(SettingsDiff.Compare(SettingsSection.Store, new Dictionary<string, string> { ["A"] = "x" },
            new Dictionary<string, string> { ["A"] = "x" }, "Ana", Now));

    [Fact]
    public void AMissingOldValueCountsAsEmpty()
    {
        var changes = SettingsDiff.Compare(SettingsSection.Store, new Dictionary<string, string>(),
            new Dictionary<string, string> { ["A"] = "x", ["B"] = "" }, "Ana", Now);

        Assert.Equal(["A"], changes.Select(c => c.Field));
    }
}

public class StoreRulesTests
{
    private static StoreProfile Profile(string name = "Shop", string email = "", string baseCurrency = "USD") =>
        new(name, "", "", "", "", null, "", email, baseCurrency, ["VES"], TaxRate.Default, TaxMode.Included, AppLanguage.English);

    [Fact]
    public void ValidProfilePasses() => Assert.Equal(ValidationCode.None, StoreRules.Validate(Profile()));

    [Fact]
    public void NameIsRequired() => Assert.Equal(ValidationCode.Required, StoreRules.Validate(Profile(name: " ")));

    [Fact]
    public void EmailIsOptionalButMustBeWellFormed()
    {
        Assert.Equal(ValidationCode.None, StoreRules.Validate(Profile(email: "")));
        Assert.Equal(ValidationCode.InvalidEmail, StoreRules.Validate(Profile(email: "nope")));
    }

    [Fact]
    public void BaseCurrencyMustBeACode() =>
        Assert.Equal(ValidationCode.InvalidCurrency, StoreRules.Validate(Profile(baseCurrency: "US")));

    [Fact]
    public void FieldMapHasPlainValuesForAudit()
    {
        var fields = Profile().ToFields();
        Assert.Equal("VES", fields["AcceptedCurrencies"]);
        Assert.Equal("Included", fields["TaxMode"]);
        Assert.Equal("0", fields["TaxRate"]);
    }
}

public class StaffDirectoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 28, 10, 0, 0, TimeSpan.Zero);
    private static readonly StaffMember Owner = new(Guid.NewGuid(), "Carlos", "c@shop.com", "100200", UserRole.Owner, AccountStatus.Active);
    private static readonly StaffMember Cashier = new(Guid.NewGuid(), "Luis", "", "570146", UserRole.Cashier, AccountStatus.Active);

    private static StaffDirectory Directory(params string[] codes)
    {
        var queue = new Queue<string>(codes.Length == 0 ? ["111111", "222222", "333333"] : codes);
        return new StaffDirectory([Owner, Cashier], queue.Dequeue);
    }

    [Fact]
    public void ManagerAddsACashierWithAGeneratedCodeAndAuditEntry()
    {
        var directory = Directory();

        var error = directory.Add(UserRole.Manager, "Marta", Now, " Sofía ", null, UserRole.Cashier, out var added, out var audit);

        Assert.Equal(StaffError.None, error);
        Assert.Equal("Sofía", added!.Name);
        Assert.Equal("111111", added.Code);
        Assert.Equal(AccountStatus.Active, added.Status);
        Assert.Equal("Marta", Assert.Single(audit).User);
    }

    [Fact]
    public void CashierCannotAdd() =>
        Assert.Equal(StaffError.NotAllowed,
            Directory().Add(UserRole.Cashier, "Luis", Now, "X", null, UserRole.Cashier, out _, out _));

    [Fact]
    public void ThereIsExactlyOneOwner() =>
        Assert.Equal(StaffError.OwnerIsUnique,
            Directory().Add(UserRole.Owner, "Carlos", Now, "X", "x@shop.com", UserRole.Owner, out _, out _));

    [Fact]
    public void NameIsRequiredAndEmailMustBeWellFormed()
    {
        var directory = Directory();
        Assert.Equal(StaffError.NameRequired, directory.Add(UserRole.Owner, "C", Now, " ", null, UserRole.Cashier, out _, out _));
        Assert.Equal(StaffError.EmailInvalid, directory.Add(UserRole.Owner, "C", Now, "X", "bad", UserRole.Cashier, out _, out _));
    }

    [Fact]
    public void GeneratedCodesSkipCodesInUse()
    {
        var directory = Directory("570146", "999999");

        directory.Add(UserRole.Owner, "C", Now, "X", null, UserRole.Cashier, out var added, out _);

        Assert.Equal("999999", added!.Code);
    }

    [Fact]
    public void ManagerCannotDeactivateOrEditTheOwner()
    {
        var directory = Directory();
        Assert.Equal(StaffError.NotAllowed, directory.SetStatus(UserRole.Manager, "M", Now, Owner.Id, AccountStatus.Inactive, out _));
        Assert.Equal(StaffError.NotAllowed, directory.Edit(UserRole.Manager, "M", Now, Owner.Id, "New", "c@shop.com", UserRole.Owner, out _));
        Assert.Equal(AccountStatus.Active, directory.Find(Owner.Id)!.Status);
    }

    [Fact]
    public void ManagerDeactivatesACashierAndItIsAudited()
    {
        var directory = Directory();

        var error = directory.SetStatus(UserRole.Manager, "M", Now, Cashier.Id, AccountStatus.Inactive, out var audit);

        Assert.Equal(StaffError.None, error);
        Assert.Equal(AccountStatus.Inactive, directory.Find(Cashier.Id)!.Status);
        var entry = Assert.Single(audit);
        Assert.Equal("Active", entry.OldValue);
        Assert.Equal("Inactive", entry.NewValue);
    }

    [Fact]
    public void SettingTheSameStatusWritesNothing()
    {
        Directory().SetStatus(UserRole.Owner, "C", Now, Cashier.Id, AccountStatus.Active, out var audit);
        Assert.Empty(audit);
    }

    [Fact]
    public void RoleChangesAreAuditedButNeverToOrFromOwner()
    {
        var directory = Directory();

        Assert.Equal(StaffError.None, directory.Edit(UserRole.Owner, "C", Now, Cashier.Id, "Luis", "", UserRole.Manager, out var audit));
        Assert.Equal("StaffRole|Luis", Assert.Single(audit).Field);
        Assert.Equal(StaffError.OwnerIsUnique, directory.Edit(UserRole.Owner, "C", Now, Cashier.Id, "Luis", "", UserRole.Owner, out _));
        Assert.Equal(StaffError.OwnerIsUnique, directory.Edit(UserRole.Owner, "C", Now, Owner.Id, "Carlos", "c@shop.com", UserRole.Manager, out _));
    }

    [Fact]
    public void OwnerKeepsAnEmail() =>
        Assert.Equal(StaffError.EmailInvalid,
            Directory().Edit(UserRole.Owner, "C", Now, Owner.Id, "Carlos", "", UserRole.Owner, out _));

    [Fact]
    public void RegeneratingACodeAuditsMaskedValuesOnly()
    {
        var directory = Directory("424242");

        directory.RegenerateCode(UserRole.Owner, "C", Now, Cashier.Id, out var audit);

        var entry = Assert.Single(audit);
        Assert.Equal("424242", directory.Find(Cashier.Id)!.Code);
        Assert.DoesNotContain("570146", entry.OldValue);
        Assert.DoesNotContain("424242", entry.NewValue);
        Assert.EndsWith("46", entry.OldValue);
    }

    [Fact]
    public void UnknownMemberIsNotFound() =>
        Assert.Equal(StaffError.NotFound, Directory().SetStatus(UserRole.Owner, "C", Now, Guid.NewGuid(), AccountStatus.Inactive, out _));
}

public class ExchangeRateBookTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 28, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("41.5", 41.5)]
    [InlineData(" 0.000001 ", 0.000001)]
    public void ParsesPositiveRates(string text, double expected)
    {
        Assert.True(ExchangeRateBook.TryParseRate(text, System.Globalization.CultureInfo.InvariantCulture, out var rate));
        Assert.Equal((decimal)expected, rate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("abc")]
    [InlineData("1.0000001")]
    public void RejectsOtherText(string text) =>
        Assert.False(ExchangeRateBook.TryParseRate(text, System.Globalization.CultureInfo.InvariantCulture, out _));

    [Fact]
    public void AppendKeepsEveryEntryAndNeverOverwrites()
    {
        var book = new ExchangeRateBook("USD");
        book.Append("VES", 40m, T0, "Ana", T0);
        book.Append("VES", 41m, T0.AddHours(1), "Ana", T0.AddHours(1));

        Assert.Equal(2, book.Entries.Count);
        Assert.Equal([41m, 40m], book.History("ves").Select(e => e.Rate));
    }

    [Fact]
    public void RateAtUsesTheLatestEffectiveEntryNotAfterThatTime()
    {
        var book = new ExchangeRateBook("USD");
        book.Append("VES", 40m, T0, "Ana", T0);
        book.Append("VES", 42m, T0.AddHours(5), "Ana", T0);

        Assert.Null(book.RateAt("VES", T0.AddMinutes(-1)));
        Assert.Equal(40m, book.RateAt("VES", T0.AddHours(1))!.Rate);
        Assert.Equal(42m, book.RateAt("VES", T0.AddHours(6))!.Rate);
    }

    [Fact]
    public void BackdatedEntryDoesNotReplaceANewerRate()
    {
        var book = new ExchangeRateBook("USD");
        book.Append("VES", 42m, T0.AddHours(5), "Ana", T0);
        book.Append("VES", 39m, T0, "Ana", T0.AddHours(6));

        Assert.Equal(42m, book.RateAt("VES", T0.AddHours(7))!.Rate);
    }

    [Fact]
    public void BaseCurrencyAndBadRatesAreRefused()
    {
        var book = new ExchangeRateBook("USD");
        Assert.False(book.Append("usd", 1m, T0, "Ana", T0));
        Assert.False(book.Append("VES", 0m, T0, "Ana", T0));
        Assert.Empty(book.Entries);
    }

    [Theory]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Owner)]
    public void EveryRoleMayUpdateRates(UserRole role) => Assert.True(ExchangeRateBook.CanUpdate(role));
}

public class LowStockPolicyTests
{
    [Theory]
    [InlineData(-1, 5, StockLevel.NoStock)]
    [InlineData(0, 5, StockLevel.NoStock)]
    [InlineData(1, 5, StockLevel.LowStock)]
    [InlineData(5, 5, StockLevel.LowStock)]
    [InlineData(6, 5, StockLevel.InStock)]
    public void ClassifiesOnHand(int onHand, int threshold, StockLevel expected) =>
        Assert.Equal(expected, LowStockPolicy.Classify(onHand, threshold));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(10_000, true)]
    [InlineData(10_001, false)]
    public void ThresholdRange(int value, bool valid) => Assert.Equal(valid, LowStockPolicy.IsValid(value));

    [Fact]
    public void DefaultIsValid() => Assert.True(LowStockPolicy.IsValid(LowStockPolicy.DefaultThreshold));
}
