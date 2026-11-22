using Microsoft.UI.Xaml.Media;

namespace Estoq.App.ViewModels.AddProduct;

/// <summary>A picked product image. Only the path is saved; the preview is for the pop-up.</summary>
public sealed record ProductImageViewModel(string Path, ImageSource? Preview);
