using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Estoq.App.ViewModels.Returns;

/// <summary>One new item the customer takes in an exchange. Built again after every change, so it holds plain values.</summary>
public sealed class ExchangeLineViewModel
{
    public ExchangeLineViewModel(string variantId, string name, string sku, int quantity, string totalText, Action<string, int> setQuantity)
    {
        VariantId = variantId;
        Name = name;
        Sku = sku;
        Quantity = quantity;
        QuantityText = quantity.ToString(CultureInfo.InvariantCulture);
        TotalText = totalText;
        IncreaseCommand = new RelayCommand(() => setQuantity(VariantId, Quantity + 1));
        DecreaseCommand = new RelayCommand(() => setQuantity(VariantId, Quantity - 1));
        RemoveCommand = new RelayCommand(() => setQuantity(VariantId, 0));
    }

    public string VariantId { get; }

    public string Name { get; }

    public string Sku { get; }

    public int Quantity { get; }

    public string QuantityText { get; }

    public string TotalText { get; }

    public ICommand IncreaseCommand { get; }

    public ICommand DecreaseCommand { get; }

    public ICommand RemoveCommand { get; }
}
