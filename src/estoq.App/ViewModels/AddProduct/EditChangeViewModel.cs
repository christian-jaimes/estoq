namespace Estoq.App.ViewModels.AddProduct;

/// <summary>One line of the "Confirm changes" step in edit mode.</summary>
/// <param name="Subject">What changed: the product name for a product field, or the variant ("White – S") for a variant price.</param>
/// <param name="Field">The name of the field that changed, in the app language.</param>
/// <param name="Before">The old value, or a dash when it was empty.</param>
/// <param name="After">The new value, or a dash when it is now empty.</param>
/// <param name="IsAlternateRow">True for every second row, which gets the table's banding.</param>
public sealed record EditChangeViewModel(string Subject, string Field, string Before, string After, bool IsAlternateRow);
