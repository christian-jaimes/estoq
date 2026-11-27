using Estoq.App.Services;
using Estoq.App.ViewModels.Returns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ReturnsViewModel.IsExchange))
            {
                UpdateExchangeRow();
            }
        };
        UpdateExchangeRow();
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

    /// <summary>Keeps the right column at least as tall as the pop-up body, so its cards fill the workable area.</summary>
    private void OnRightScrollSizeChanged(object sender, SizeChangedEventArgs e) => RightPanel.MinHeight = RightScroll.ActualHeight;

    /// <summary>Return mode gives the receipt items the whole left column; Exchange splits it with the new-items table.</summary>
    private void UpdateExchangeRow()
    {
        ExchangeRow.Height = new GridLength(ViewModel.IsExchange ? 1 : 0, GridUnitType.Star);
        ExchangeRow.MinHeight = ViewModel.IsExchange ? 200 : 0;
    }

    /// <summary>Keeps the left column at least as tall as the pop-up body, so its cards fill the workable area.</summary>
    private void OnLeftScrollSizeChanged(object sender, SizeChangedEventArgs e) => LeftPanel.MinHeight = LeftScroll.ActualHeight;

    /// <summary>Clicking the chosen mode again keeps it chosen: the toggle is put back in line with the view model.</summary>
    private void OnModeClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
        {
            toggle.DispatcherQueue.TryEnqueue(() =>
                toggle.IsChecked = ReferenceEquals(toggle.Command, ViewModel.SelectExchangeCommand) ? ViewModel.IsExchange : ViewModel.IsRefund);
        }
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

    private void OnExchangeSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        ViewModel.UpdateExchangeSuggestions(sender.Text);
        sender.IsSuggestionListOpen = ViewModel.ExchangeSuggestions.Count > 0;
    }

    private void OnExchangeSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        sender.IsSuggestionListOpen = false;
        var added = args.ChosenSuggestion is SaleVariant variant
            ? ViewModel.AddExchangeItem(variant)
            : ViewModel.AddExchangeByQuery(args.QueryText ?? sender.Text);

        // Ready for the next scan; a failed match keeps the text so it can be corrected.
        if (added)
        {
            sender.DispatcherQueue.TryEnqueue(() =>
            {
                sender.Text = string.Empty;
                sender.Focus(FocusState.Programmatic);
            });
        }
    }

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
