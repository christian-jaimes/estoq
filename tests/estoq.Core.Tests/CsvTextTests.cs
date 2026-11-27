using Estoq.Core.Inventory;
using Xunit;

namespace Estoq.Core.Tests;

public class CsvTextTests
{
    [Fact]
    public void WritesHeaderThenRows()
    {
        var text = CsvText.Build(["Name", "Stock"], [["Basic T-Shirt", "120"], ["Cap", "37"]]);
        Assert.Equal("Name,Stock\r\nBasic T-Shirt,120\r\nCap,37\r\n", text);
    }

    [Fact]
    public void QuotesCommasQuotesAndLineBreaks()
    {
        Assert.Equal("\"Hugo Boss, Dress\"", CsvText.Escape("Hugo Boss, Dress"));
        Assert.Equal("\"say \"\"hi\"\"\"", CsvText.Escape("say \"hi\""));
        Assert.Equal("\"a\nb\"", CsvText.Escape("a\nb"));
    }

    [Theory]
    [InlineData("=SUM(A1)", "'=SUM(A1)")]
    [InlineData("+1", "'+1")]
    [InlineData("@x", "'@x")]
    public void TextThatLooksLikeAFormulaStaysText(string value, string expected) =>
        Assert.Equal(expected, CsvText.Escape(value));

    [Fact]
    public void NullIsEmpty() => Assert.Equal(string.Empty, CsvText.Escape(null));

    [Fact]
    public void PlainAndNegativeTextIsUntouched()
    {
        Assert.Equal("Olive", CsvText.Escape("Olive"));
        Assert.Equal("-6", CsvText.Escape("-6"));
    }
}
