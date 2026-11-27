using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.AddProduct;

/// <summary>A picked product image. Only the path is saved; the preview is for the pop-up.</summary>
public sealed partial class ProductImageViewModel : ObservableObject
{
    public ProductImageViewModel(
        string path,
        ImageSource? preview,
        Action<ProductImageViewModel> select,
        Action<ProductImageViewModel> remove)
    {
        Path = path;
        Preview = preview;
        SelectCommand = new RelayCommand(() => select(this));
        RemoveCommand = new RelayCommand(() => remove(this));
    }

    public string Path { get; }

    public ImageSource? Preview { get; }

    public IRelayCommand SelectCommand { get; }

    public IRelayCommand RemoveCommand { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public Visibility SelectionVisibility => IsSelected ? Visibility.Visible : Visibility.Collapsed;

    partial void OnIsSelectedChanged(bool value) => OnPropertyChanged(nameof(SelectionVisibility));
}
