using Estoq.App.Services;
using Estoq.App.ViewModels.StockAdjustments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.Views.StockAdjustments;

/// <summary>The body of the Stock adjustments pop-up (F3). All behaviour lives in <see cref="StockAdjustmentViewModel"/>.</summary>
public sealed partial class StockAdjustmentView : UserControl
{
    private ScrollViewer? _popupBodyScrollViewer;

    public StockAdjustmentView()
    {
        ViewModel = App.Current.Services.GetRequiredService<StockAdjustmentViewModel>();
        InitializeComponent();

        // The shell shows and hides this view with the pop-up; a scanner types into the search box, so it gets focus on open.
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) =>
        {
            if (Visibility == Visibility.Visible)
            {
                FocusSearch();
            }
        });
        ViewModel.PropertyChanged += (_, e) =>
        {
            // A fresh adjustment (after "New adjustment") starts with the search box ready again.
            if (e.PropertyName == nameof(StockAdjustmentViewModel.IsCompleted) && !ViewModel.IsCompleted && Visibility == Visibility.Visible)
            {
                FocusSearch();
            }
        };
    }

    public StockAdjustmentViewModel ViewModel { get; }

    private void FocusSearch() => DispatcherQueue.TryEnqueue(() =>
    {
        if (ViewModel.HasAccess)
        {
            SearchBox.Focus(FocusState.Programmatic);
        }
    });

    private void OnAdjustLoaded(object sender, RoutedEventArgs e)
    {
        if (_popupBodyScrollViewer is null)
        {
            for (DependencyObject? current = this; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is ScrollViewer scrollViewer)
                {
                    _popupBodyScrollViewer = scrollViewer;
                    _popupBodyScrollViewer.SizeChanged += OnPopupBodySizeChanged;
                    break;
                }
            }
        }

        UpdateHeight();
    }

    private void OnPopupBodySizeChanged(object sender, SizeChangedEventArgs e) => UpdateHeight();

    // The form fills the pop-up body exactly: the items table scrolls, the cards and footer never move.
    private void UpdateHeight()
    {
        if (_popupBodyScrollViewer is null)
        {
            return;
        }

        var viewportHeight = _popupBodyScrollViewer.ViewportHeight;
        if (viewportHeight > 0 && !double.IsInfinity(viewportHeight))
        {
            AdjustRoot.Height = viewportHeight;
        }
    }

    private void OnAdjustTapped(object sender, TappedRoutedEventArgs e)
    {
        // Clicking elsewhere in the pop-up closes an open suggestion list.
        if (SearchBox.IsSuggestionListOpen && (e.OriginalSource is not DependencyObject source || !IsWithin(source, SearchBox)))
        {
            SearchBox.IsSuggestionListOpen = false;
        }
    }

    private static bool IsWithin(DependencyObject element, DependencyObject ancestor)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        ViewModel.UpdateSuggestions(sender.Text);
        sender.IsSuggestionListOpen = ViewModel.Suggestions.Count > 0;
    }

    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        sender.IsSuggestionListOpen = false;
        var added = args.ChosenSuggestion is StockItem item
            ? ViewModel.AddItem(item)
            : ViewModel.AddByQuery(args.QueryText ?? sender.Text);

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

    private void OnTypeClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Tag: AdjustmentTypeChoiceViewModel choice } toggle)
        {
            ViewModel.SelectType(choice);

            // Clicking the chosen chip again keeps it chosen: one type is always required once picked.
            toggle.IsChecked = choice.IsSelected;
        }
    }

    private void OnCountedBeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args) =>
        args.Cancel = !args.NewText.All(char.IsAsciiDigit);
}
