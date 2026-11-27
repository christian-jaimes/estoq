using Estoq.Core.Products;
using Xunit;

namespace Estoq.Core.Tests;

public class InternalBarcodeTests
{
    [Fact]
    public void KnownEan13CheckDigit() =>
        // 4006381333931 is a published EAN-13 example.
        Assert.Equal('1', InternalBarcode.CheckDigit("400638133393"));

    [Fact]
    public void SequenceIsPaddedAndGetsPrefixAndCheckDigit()
    {
        var barcode = InternalBarcode.FromSequence(1);
        Assert.Equal(13, barcode.Length);
        Assert.StartsWith("200000000001", barcode, StringComparison.Ordinal);
        Assert.True(InternalBarcode.IsValid(barcode));
    }

    [Fact]
    public void ManySequencesAreValidAndUnique()
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < 2000; i++)
        {
            var barcode = InternalBarcode.FromSequence(i);
            Assert.True(InternalBarcode.IsValid(barcode));
            Assert.True(seen.Add(barcode));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100_000_000_000)]
    public void SequenceOutOfRangeIsRejected(long sequence) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => InternalBarcode.FromSequence(sequence));

    [Theory]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("2000000000018")]
    public void WrongBarcodesAreInvalid(string? barcode) => Assert.False(InternalBarcode.IsValid(barcode));
}
