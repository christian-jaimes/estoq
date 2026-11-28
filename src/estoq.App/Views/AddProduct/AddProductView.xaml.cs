using Estoq.App.ViewModels.AddProduct;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Estoq.App.Views.AddProduct;

/// <summary>The body of the Add product pop-up (F2). All behaviour lives in <see cref="AddProductViewModel"/>.</summary>
public sealed partial class AddProductView : UserControl
{
    private ScrollViewer? _popupBodyScrollViewer;

    public AddProductView()
    {
        ViewModel = App.Current.Services.GetRequiredService<AddProductViewModel>();
        InitializeComponent();
        CostField.ErrorTextChanged += OnPricingFieldErrorChanged;
        SellingField.ErrorTextChanged += OnPricingFieldErrorChanged;
        ViewModel.PropertyChanged += OnAddProductPropertyChanged;
        UpdatePricingHeader();
    }

    public AddProductViewModel ViewModel { get; }

    private void OnAddProductLoaded(object sender, RoutedEventArgs e)
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

        if (ViewModel.IsEditing)
        {
            CloseSuggestionLists();
        }

        UpdateFormHeight();
    }

    private void CloseSuggestionLists()
    {
        TypeBox.IsSuggestionListOpen = false;
        CategoryBox.IsSuggestionListOpen = false;
        BrandBox.IsSuggestionListOpen = false;
    }

    private void OnPopupBodySizeChanged(object sender, SizeChangedEventArgs e) => UpdateFormHeight();

    private void UpdateFormHeight()
    {
        if (_popupBodyScrollViewer is null) return;
        var viewportHeight = _popupBodyScrollViewer.ViewportHeight;
        if (viewportHeight > 0 && !double.IsInfinity(viewportHeight))
        {
            AddProductRoot.Height = viewportHeight;
        }
    }

    private void OnPricingFieldErrorChanged(object? sender, EventArgs e)
    {
        UpdatePricingHeader();
    }

    private void OnAddProductPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddProductViewModel.PricingDisclaimer)) UpdatePricingHeader();
        if (e.PropertyName == nameof(AddProductViewModel.IsEditing) && ViewModel.IsEditing)
        {
            // Edit mode opens with every field filled in and every suggestion list closed.
            CloseSuggestionLists();

            // The field values arrive through bindings a moment later; close again once they have.
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, CloseSuggestionLists);
        }

        if (e.PropertyName is nameof(AddProductViewModel.MainImage) or nameof(AddProductViewModel.ImagePosition))
        {
            DispatcherQueue.TryEnqueue(CenterSelectedThumbnail);
        }
    }

    private void UpdatePricingHeader()
    {
        var error = !string.IsNullOrEmpty(SellingField.ErrorText) ? SellingField.ErrorText
            : !string.IsNullOrEmpty(CostField.ErrorText) ? CostField.ErrorText
            : ViewModel.PricingDisclaimer;
        PricingErrorText.Text = string.IsNullOrEmpty(error)
            ? string.Empty
            : error.StartsWith("— ", StringComparison.Ordinal) ? error : $"— {error}";
        PricingErrorText.Visibility = string.IsNullOrEmpty(error) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnMasterDataTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        var source = ReferenceEquals(sender, TypeBox) ? ViewModel.ProductTypeNames
            : ReferenceEquals(sender, CategoryBox) ? ViewModel.CategoryNames : ViewModel.BrandNames;
        var results = source.Where(value => value.Contains(sender.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).Take(100).ToList();
        var suggestions = results.Select(value => new MasterDataSuggestion(value, false)).ToList();
        var typed = sender.Text.Trim();
        if (typed.Length > 0 && !source.Any(value => string.Equals(value, typed, StringComparison.CurrentCultureIgnoreCase)))
        {
            if (results.Count == 0) suggestions.Add(new MasterDataSuggestion(typed, true));
        }
        if (ReferenceEquals(sender, TypeBox)) Replace(ViewModel.FilteredProductTypes, suggestions);
        else if (ReferenceEquals(sender, CategoryBox)) Replace(ViewModel.FilteredCategories, suggestions);
        else Replace(ViewModel.FilteredBrands, suggestions);
        if (suggestions.Count > 0 && !sender.IsSuggestionListOpen && sender.FocusState != FocusState.Unfocused) sender.IsSuggestionListOpen = true;
    }

    private void OnMasterDataGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is AutoSuggestBox box && !ViewModel.IsEditing && string.IsNullOrWhiteSpace(box.Text) && !box.IsSuggestionListOpen) box.IsSuggestionListOpen = true;
    }

    private void OnAddProductTapped(object sender, TappedRoutedEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        CloseIfOutside(TypeBox, source);
        CloseIfOutside(CategoryBox, source);
        CloseIfOutside(BrandBox, source);
    }

    private static void CloseIfOutside(AutoSuggestBox box, DependencyObject? source)
    {
        if (box.IsSuggestionListOpen && (source is null || !IsWithin(source, box))) box.IsSuggestionListOpen = false;
    }

    private static bool IsWithin(DependencyObject element, DependencyObject ancestor)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor)) return true;
        }
        return false;
    }

    private void OnMasterDataSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is not MasterDataSuggestion suggestion) return;
        ApplySuggestionValueAfterSelection(sender, suggestion.Value);
    }

    private void OnMasterDataQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var value = (args.ChosenSuggestion as MasterDataSuggestion)?.Value ?? sender.Text.Trim();
        sender.IsSuggestionListOpen = false;
        ApplySuggestionValueAfterSelection(sender, value);
    }

    private void ApplySuggestionValueAfterSelection(AutoSuggestBox sender, string value) =>
        sender.DispatcherQueue.TryEnqueue(() =>
        {
            sender.Text = value;
            SetMasterDataValue(sender, value);
        });

    private void SetMasterDataValue(AutoSuggestBox sender, string value)
    {
        if (ReferenceEquals(sender, TypeBox)) ViewModel.ProductType = value;
        else if (ReferenceEquals(sender, CategoryBox)) ViewModel.Category = value;
        else if (ReferenceEquals(sender, BrandBox)) ViewModel.Brand = value;
    }

    private static void Replace(System.Collections.ObjectModel.ObservableCollection<MasterDataSuggestion> target, IEnumerable<MasterDataSuggestion> values)
    {
        var desired = values.ToList();
        for (var index = target.Count - 1; index >= 0; index--)
        {
            if (!desired.Any(option => option == target[index])) target.RemoveAt(index);
        }
        for (var index = 0; index < desired.Count; index++)
        {
            if (index < target.Count && target[index] == desired[index]) continue;
            var currentIndex = -1;
            for (var scan = index + 1; scan < target.Count; scan++)
            {
                if (target[scan] == desired[index]) { currentIndex = scan; break; }
            }
            if (currentIndex >= 0) target.Move(currentIndex, index);
            else target.Insert(index, desired[index]);
        }
        while (target.Count > desired.Count) target.RemoveAt(target.Count - 1);
    }

    private void OnAddSizeClick(object sender, RoutedEventArgs e) => CommitNewSize();

    private void OnNewSizeKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            CommitNewSize();
        }
    }

    private void CommitNewSize()
    {
        ViewModel.AddCustomSize(NewSizeBox.Text);
        NewSizeBox.Text = string.Empty;
    }

    private void OnAddColourClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AddCustomColour(NewColourBox.Text);
        NewColourBox.Text = string.Empty;
    }

    private void OnRemoveSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ChoiceItemViewModel size }) ViewModel.RemoveSize(size);
    }

    private void OnRemoveColourClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ChoiceItemViewModel colour }) ViewModel.RemoveColour(colour);
    }

    private void CenterSelectedThumbnail()
    {
        if (!ViewModel.HasImages) return;
        var selectedIndex = ViewModel.Images.ToList().FindIndex(image => ReferenceEquals(image.Preview, ViewModel.MainImage));
        if (selectedIndex < 0) return;
        var viewportHeight = ImageThumbnailScrollViewer.ViewportHeight > 0
            ? ImageThumbnailScrollViewer.ViewportHeight
            : ImageThumbnailScrollViewer.ActualHeight;
        const double thumbnailStride = 78;
        const double thumbnailExtent = 70;
        var offset = selectedIndex * thumbnailStride - (viewportHeight - thumbnailExtent) / 2;
        ImageThumbnailScrollViewer.ChangeView(null, Math.Clamp(offset, 0, ImageThumbnailScrollViewer.ScrollableHeight), null, true);
    }

    private void OnThumbnailPointerEntered(object sender, PointerRoutedEventArgs e) => SetThumbnailRemoveOpacity(sender, 1);

    private void OnThumbnailPointerExited(object sender, PointerRoutedEventArgs e) => SetThumbnailRemoveOpacity(sender, 0);

    private static void SetThumbnailRemoveOpacity(object sender, double opacity)
    {
        if (sender is not Grid thumbnail || thumbnail.Children.OfType<Button>().LastOrDefault() is not { } removeButton) return;

        var storyboard = new Storyboard();
        var animation = new DoubleAnimation { To = opacity, Duration = TimeSpan.FromMilliseconds(80) };
        Storyboard.SetTarget(animation, removeButton);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void OnProductImageKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var delta = e.Key switch
        {
            VirtualKey.Right or VirtualKey.Down => 1,
            VirtualKey.Left or VirtualKey.Up => -1,
            _ => 0,
        };
        if (delta == 0 || !ViewModel.HasImages) return;
        ViewModel.StepImage(delta);
        e.Handled = true;
    }

    private void OnBrowseImagesClick(object sender, RoutedEventArgs e) => ViewModel.AddImageCommand.Execute(null);

    private void OnStockTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var raw = textBox.Text;
        var filtered = new string(raw.Where(character => character is >= '0' and <= '9').ToArray());
        if (int.TryParse(filtered, out var units) && units > 1_000_000) filtered = "1000000";
        if (raw == filtered) return;

        var caret = Math.Min(textBox.SelectionStart, filtered.Length);
        textBox.Text = filtered;
        textBox.SelectionStart = caret;
    }

    private void OnProductImageDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy
            : DataPackageOperation.None;
        e.Handled = true;
    }

    private async void OnProductImageDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.Handled = true;
        var files = (await e.DataView.GetStorageItemsAsync())
            .OfType<StorageFile>()
            .Where(IsSupportedProductImage)
            .Select(file => file.Path)
            .ToList();
        await ViewModel.AddDroppedImagesAsync(files);
    }

    private static bool IsSupportedProductImage(StorageFile file) =>
        file.FileType.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
        file.FileType.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
        file.FileType.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        file.FileType.Equals(".webp", StringComparison.OrdinalIgnoreCase);

    // Bindings inside the row templates are one-way; edits come back through these handlers.
    private void OnChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ChoiceItemViewModel item } element)
        {
            var isChecked = element switch
            {
                CheckBox checkBox => checkBox.IsChecked == true,
                Microsoft.UI.Xaml.Controls.Primitives.ToggleButton toggle => toggle.IsChecked == true,
                _ => item.IsSelected,
            };
            item.IsSelected = isChecked;
        }
    }

    private void OnPreviousImageClick(object sender, RoutedEventArgs e) => ViewModel.StepImage(-1);

    private void OnNextImageClick(object sender, RoutedEventArgs e) => ViewModel.StepImage(1);

}
