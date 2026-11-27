using System.Globalization;

namespace Estoq.Core.Products;

/// <summary>
/// The internal barcode of a variant: an EAN-13 in the store-use range (prefix 2). The sequence number comes from
/// wherever variants are stored, so the barcode stays unique without a lookup here.
/// </summary>
public static class InternalBarcode
{
    public const char Prefix = '2';
    public const long MaxSequence = 99_999_999_999;

    /// <summary>Sequence 1 gives "2000000000017": prefix, eleven digits, check digit.</summary>
    public static string FromSequence(long sequence)
    {
        if (sequence is < 0 or > MaxSequence)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), sequence, "The sequence must fit in eleven digits.");
        }

        var twelve = Prefix + sequence.ToString("D11", CultureInfo.InvariantCulture);
        return twelve + CheckDigit(twelve);
    }

    /// <summary>The EAN-13 check digit for the first twelve digits.</summary>
    public static char CheckDigit(string twelveDigits)
    {
        if (twelveDigits is not { Length: 12 } || !twelveDigits.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Twelve digits are required.", nameof(twelveDigits));
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = twelveDigits[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        return (char)('0' + ((10 - (sum % 10)) % 10));
    }

    public static bool IsValid(string? barcode) =>
        barcode is { Length: 13 }
        && barcode.All(char.IsAsciiDigit)
        && barcode[12] == CheckDigit(barcode[..12]);
}
