using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.AddProduct;

/// <summary>A size chip or a colour swatch that can be ticked. Only ticked ones become variants.</summary>
public sealed partial class ChoiceItemViewModel : ObservableObject
{
    public ChoiceItemViewModel(string value, string label, string? hex = null, bool isQuickPick = true)
    {
        Value = value;
        Label = label;
        Hex = hex;
        IsQuickPick = isQuickPick;
        SwatchBrush = Swatch.FromHex(hex);
    }

    /// <summary>The stable value used in SKUs and the database (the size, or the English colour name).</summary>
    public string Value { get; }

    /// <summary>The text shown, in the app language.</summary>
    public string Label { get; }

    /// <summary>Swatch colour like "#B22C2C"; null for sizes.</summary>
    public string? Hex { get; }

    public Brush? SwatchBrush { get; }

    /// <summary>Quick picks show as swatches at once; the rest sit behind "More colours" until ticked.</summary>
    public bool IsQuickPick { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
