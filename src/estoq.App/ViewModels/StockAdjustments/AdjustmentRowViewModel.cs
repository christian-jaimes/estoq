using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Stock;

namespace Estoq.App.ViewModels.StockAdjustments;

/// <summary>
/// One item of the adjustment: stock now, the counted quantity (the new stock) and the difference between them.
/// Typing the counted quantity sets the difference; the stepper changes the difference directly.
/// </summary>
public sealed partial class AdjustmentRowViewModel : ObservableObject
{
    private readonly Func<AdjustmentType?> _type;
    private readonly Action<AdjustmentRowViewModel> _remove;
    private readonly Action _changed;
    private readonly ILocalizer _localizer;
    private int _counted;
    private string _countedText;
    private bool _updating;

    public AdjustmentRowViewModel(StockItem item, Func<AdjustmentType?> type, Action<AdjustmentRowViewModel> remove, Action changed, ILocalizer localizer)
    {
        Item = item;
        _type = type;
        _remove = remove;
        _changed = changed;
        _localizer = localizer;

        // A new item starts at its current stock: no difference until the user counts.
        _counted = item.OnHand;
        _countedText = item.OnHand.ToString(CultureInfo.InvariantCulture);
    }

    public StockItem Item { get; }

    [ObservableProperty]
    public partial bool IsAlternateRow { get; set; }

    public int Current => Item.OnHand;

    public string CurrentText => Current.ToString("N0", _localizer.Culture);

    /// <summary>The counted quantity, which becomes the item's new stock.</summary>
    public int Counted => _counted;

    public int Difference => _counted - Current;

    public bool IsIncrease => Difference > 0;

    public bool IsDecrease => Difference < 0;

    public bool IsUnchanged => Difference == 0;

    /// <summary>The difference with its sign, for example "+3" or "−2".</summary>
    public string DifferenceText => Difference.ToString("+#,0;−#,0;0", _localizer.Culture);

    /// <summary>The editable counted quantity. Empty while typing counts as no change; the text never leaves what the type allows.</summary>
    public string CountedText
    {
        get => _countedText;
        set
        {
            if (!SetProperty(ref _countedText, value) || _updating)
            {
                return;
            }

            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var typed))
            {
                // Empty (or not a number) means nothing counted yet: the item keeps its current stock.
                Apply(Current, rewriteText: false);
                return;
            }

            Apply(typed, rewriteText: true);
        }
    }

    /// <summary>Applies the rules of a newly chosen type to this item (a decrease-only type cannot count above stock).</summary>
    public void Reclamp() => Apply(_counted, rewriteText: true);

    /// <summary>Refreshes the numbers shown after a language change.</summary>
    public void RefreshStrings()
    {
        OnPropertyChanged(nameof(CurrentText));
        OnPropertyChanged(nameof(DifferenceText));
    }

    [RelayCommand]
    private void Increase() => Apply(StockAdjustmentRules.Step(_type(), Current, _counted, 1), rewriteText: true);

    [RelayCommand]
    private void Decrease() => Apply(StockAdjustmentRules.Step(_type(), Current, _counted, -1), rewriteText: true);

    [RelayCommand]
    private void Remove() => _remove(this);

    private void Apply(int quantity, bool rewriteText)
    {
        var counted = StockAdjustmentRules.ClampCounted(_type(), Current, quantity);
        if (rewriteText)
        {
            SetText(counted.ToString(CultureInfo.InvariantCulture));
        }

        if (counted == _counted)
        {
            return;
        }

        _counted = counted;
        OnPropertyChanged(nameof(Counted));
        OnPropertyChanged(nameof(Difference));
        OnPropertyChanged(nameof(DifferenceText));
        OnPropertyChanged(nameof(IsIncrease));
        OnPropertyChanged(nameof(IsDecrease));
        OnPropertyChanged(nameof(IsUnchanged));
        _changed();
    }

    private void SetText(string text)
    {
        _updating = true;
        try
        {
            CountedText = text;
        }
        finally
        {
            _updating = false;
        }
    }
}
