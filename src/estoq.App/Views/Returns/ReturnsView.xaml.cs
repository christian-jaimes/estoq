using Estoq.App.ViewModels.Returns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Estoq.App.Views.Returns;

/// <summary>The body of the Returns pop-up (F4). All behaviour lives in <see cref="ReturnsViewModel"/>.</summary>
public sealed partial class ReturnsView : UserControl
{
    private ScrollViewer? _popupBody;

    public ReturnsView()
    {
        ViewModel = App.Current.Services.GetRequiredService<ReturnsViewModel>();
        InitializeComponent();
    }

    public ReturnsViewModel ViewModel { get; }

    /// <summary>
    /// The pop-up body is a non-scrolling ScrollViewer around a StackPanel, which would let the form grow without limit.
    /// Pinning the form to the body's viewport keeps the item list and the right column scrolling inside their own cards.
    /// </summary>
    private void OnReturnsLoaded(object sender, RoutedEventArgs e)
    {
        if (_popupBody is null)
        {
            for (DependencyObject? current = this; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is ScrollViewer scrollViewer)
                {
                    _popupBody = scrollViewer;
                    _popupBody.SizeChanged += (_, _) => UpdateHeight();
                    break;
                }
            }
        }

        UpdateHeight();
    }

    private void UpdateHeight()
    {
        if (_popupBody is { ViewportHeight: > 0 and var height } && !double.IsInfinity(height))
        {
            ReturnsRoot.Height = height;
        }
    }

    private void OnReceiptKeyDown(object sender, KeyRoutedEventArgs e) => RunOnEnter(e, ViewModel.FindSaleCommand);

    private void OnReturnScanKeyDown(object sender, KeyRoutedEventArgs e) => RunOnEnter(e, ViewModel.ScanReturnItemCommand);

    private void OnExchangeScanKeyDown(object sender, KeyRoutedEventArgs e) => RunOnEnter(e, ViewModel.ScanExchangeItemCommand);

    private void OnQuantityCommit(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: ReturnLineViewModel line } box)
        {
            line.CommitQuantity(box.Text);
        }
    }

    private void OnQuantityKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && sender is TextBox { DataContext: ReturnLineViewModel line } box)
        {
            line.CommitQuantity(box.Text);
            e.Handled = true;
        }
    }

    private static void RunOnEnter(KeyRoutedEventArgs e, System.Windows.Input.ICommand command)
    {
        if (e.Key == VirtualKey.Enter)
        {
            command.Execute(null);
            e.Handled = true;
        }
    }
}
