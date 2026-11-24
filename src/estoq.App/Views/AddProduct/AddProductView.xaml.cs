using Estoq.App.ViewModels.AddProduct;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    /// <summary>For x:Bind: shows an element while a flag is false.</summary>
    public static bool Not(bool value) => !value;

    /// <summary>A typed value that is not in the list is accepted as a new product type, category or brand.</summary>
    private void OnComboTextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
    {
        var text = args.Text.Trim();
        sender.Text = text;
        if (ReferenceEquals(sender, TypeBox))
        {
            ViewModel.ProductType = text;
        }
        else if (ReferenceEquals(sender, CategoryBox))
        {
            ViewModel.Category = text;
        }
        else if (ReferenceEquals(sender, BrandBox))
        {
            ViewModel.Brand = text;
        }

        args.Handled = true;
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
}
