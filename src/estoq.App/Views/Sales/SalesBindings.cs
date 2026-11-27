using Estoq.Core.Sales;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Estoq.App.Views.Sales;

/// <summary>Function bindings for the Sales page (x:Bind functions must be static).</summary>
public static class SalesBindings
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Hidden(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Text and dot colour of a status badge: success, warning or danger token. Status is never colour alone; the badge also carries its word.</summary>
    public static Brush StatusForeground(SaleStatus status) => (Brush)Application.Current.Resources[StatusKey(status)];

    /// <summary>The same token at low opacity, as the badge fill.</summary>
    public static Brush StatusBackground(SaleStatus status)
    {
        var color = ((SolidColorBrush)Application.Current.Resources[StatusKey(status)]).Color;
        return new SolidColorBrush(Color.FromArgb(0x2E, color.R, color.G, color.B));
    }

    private static string StatusKey(SaleStatus status) => status switch
    {
        SaleStatus.Completed => "EqSuccessBrush",
        SaleStatus.Returned => "EqWarningBrush",
        _ => "EqDangerBrush",
    };
}
