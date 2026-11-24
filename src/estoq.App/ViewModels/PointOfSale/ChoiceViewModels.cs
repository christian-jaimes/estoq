using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.Core.Sales;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.PointOfSale;

/// <summary>A currency chip in the pop-up header. Exactly one is selected.</summary>
public sealed partial class CurrencyChipViewModel : ObservableObject
{
    public CurrencyChipViewModel(string code, string label, bool isSelected, Action<string> select)
    {
        Code = code;
        Label = label;
        IsSelected = isSelected;
        SelectCommand = new RelayCommand(() => select(Code));
    }

    public string Code { get; }

    public string Label { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public ICommand SelectCommand { get; }
}

/// <summary>A tile in the payment method grid. Exactly one is selected.</summary>
public sealed partial class PaymentMethodViewModel : ObservableObject
{
    public PaymentMethodViewModel(PaymentMethod method, string label, bool isSelected)
    {
        Method = method;
        Label = label;
        IsSelected = isSelected;
        (Glyph, BrandLetter, BrandBrush) = Describe(method);
        HasGlyph = Glyph.Length > 0;
        HasBrand = BrandLetter.Length > 0;
    }

    private static (string Glyph, string Letter, Brush? Brush) Describe(PaymentMethod method) =>
        method switch
        {
            PaymentMethod.Cash => ("\uEF63", string.Empty, (Brush?)null),
            PaymentMethod.Card => ("\uE8A1", string.Empty, (Brush?)null),
            PaymentMethod.Transfer => ("\uE8D4", string.Empty, (Brush?)null),
            PaymentMethod.MobilePayment => ("\uE324", string.Empty, (Brush?)null),
            PaymentMethod.Zelle => (string.Empty, "Z", Brand(0x6D, 0x1E, 0xD4)),
            PaymentMethod.Usdt => (string.Empty, "\u20AE", Brand(0x26, 0xA1, 0x7B)),
            PaymentMethod.PayPal => (string.Empty, "P", Brand(0x17, 0x9B, 0xD7)),
            _ => ("\uE5D3", string.Empty, (Brush?)null),
        };

    public PaymentMethod Method { get; }

    public string Label { get; }

    /// <summary>Material glyph for methods without a brand mark.</summary>
    public string Glyph { get; }

    public bool HasGlyph { get; }

    /// <summary>One letter standing in for the brand logo of Zelle, USDT and PayPal.</summary>
    public string BrandLetter { get; }

    public bool HasBrand { get; }

    /// <summary>Fixed brand colour (the only colours outside the design tokens, as in the mockup); null for plain icons.</summary>
    public Brush? BrandBrush { get; }

    private static SolidColorBrush Brand(byte r, byte g, byte b) => new(ColorHelper.FromArgb(0xFF, r, g, b));

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

}

/// <summary>A single active preset, sized so a one-row discount grid fills its flyout evenly.</summary>
public sealed class DiscountPresetViewModel
{
    public DiscountPresetViewModel(decimal percent, double buttonWidth, CultureInfo culture)
    {
        Label = $"{percent.ToString("0.##", culture)}%";
        Tag = percent.ToString("0.##", CultureInfo.InvariantCulture);
        ButtonWidth = buttonWidth;
    }

    public string Label { get; }

    public string Tag { get; }

    public double ButtonWidth { get; }
}

/// <summary>A payment already added to the sale (split payments), shown below the total.</summary>
public sealed class PaymentLineViewModel
{
    public PaymentLineViewModel(string method, string amount, bool isAlternateRow, Action remove)
    {
        Method = method;
        Amount = amount;
        IsAlternateRow = isAlternateRow;
        RemoveCommand = new RelayCommand(remove);
    }

    public string Method { get; }

    public string Amount { get; }

    public bool IsAlternateRow { get; }

    public ICommand RemoveCommand { get; }
}
