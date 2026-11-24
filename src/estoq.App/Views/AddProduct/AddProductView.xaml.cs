using Estoq.App.ViewModels.AddProduct;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace Estoq.App.Views.AddProduct;

/// <summary>The body of the Add product pop-up (F2). All behaviour lives in <see cref="AddProductViewModel"/>.</summary>
public sealed partial class AddProductView : UserControl
{
    public AddProductView()
    {
        ViewModel = App.Current.Services.GetRequiredService<AddProductViewModel>();
        InitializeComponent();
    }

    public AddProductViewModel ViewModel { get; }

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
        if (suggestions.Count > 0 && !sender.IsSuggestionListOpen) sender.IsSuggestionListOpen = true;
    }

    private void OnMasterDataGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is AutoSuggestBox box && string.IsNullOrWhiteSpace(box.Text) && !box.IsSuggestionListOpen) box.IsSuggestionListOpen = true;
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
        AddSizeFlyout.Hide();
    }

    private void OnThumbnailClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ProductImageViewModel image })
        {
            ViewModel.SelectImageCommand.Execute(image);
        }
    }

    private void OnRemoveVariantClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: VariantRowViewModel row })
        {
            ViewModel.RemoveVariantCommand.Execute(row);
        }
    }

    // Bindings inside the row templates are one-way; edits come back through these handlers.
    private void OnChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ChoiceItemViewModel item } element)
        {
            var isChecked = element switch
            {
                Microsoft.UI.Xaml.Controls.Primitives.ToggleButton toggle => toggle.IsChecked == true,
                _ => item.IsSelected,
            };
            item.IsSelected = isChecked;
        }
    }

    private void OnSkuTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: VariantRowViewModel row } box && row.Sku != box.Text)
        {
            row.Sku = box.Text;
        }
    }

    private void OnStockValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (sender.DataContext is VariantRowViewModel row && !double.IsNaN(args.NewValue))
        {
            row.OpeningStock = args.NewValue;
        }
    }

    private void OnVariantPriceChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: VariantRowViewModel row } box)
        {
            ViewModel.SetVariantPrice(row, box.Text);
            if (box.Text.Length > 0 && !decimal.TryParse(box.Text, System.Globalization.NumberStyles.Number,
                    ViewModel.PriceCulture, out _)) Shake(box);
        }
    }

    private void OnPreviousImageClick(object sender, RoutedEventArgs e) => ViewModel.StepImage(-1);

    private void OnNextImageClick(object sender, RoutedEventArgs e) => ViewModel.StepImage(1);

    private void Shake(FrameworkElement element)
    {
        var transform = element.RenderTransform as CompositeTransform ?? new CompositeTransform();
        element.RenderTransform = transform;
        transform.TranslateX = 0;
        var storyboard = new Storyboard();
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0 });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(45)), Value = -4 });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90)), Value = 4 });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(135)), Value = -2 });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180)), Value = 0 });
        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, "TranslateX");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
}
