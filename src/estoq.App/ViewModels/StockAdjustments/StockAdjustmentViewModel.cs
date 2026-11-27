using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;
using Estoq.Core.Roles;
using Estoq.Core.Stock;

namespace Estoq.App.ViewModels.StockAdjustments;

/// <summary>
/// State and rules of the Stock adjustments pop-up (F3, docs/features/stock-adjustments.md): one adjustment has one type
/// and reason, any number of scanned or searched items, and is applied in one go. A singleton, like the Add product form;
/// <see cref="Reset"/> starts a fresh adjustment each time the pop-up opens.
/// </summary>
public sealed partial class StockAdjustmentViewModel : ObservableObject
{
    private const int MaxSuggestions = 6;

    private readonly IStockAdjustmentService _service;
    private readonly ILocalizer _localizer;
    private readonly ISessionService _session;

    public StockAdjustmentViewModel(IStockAdjustmentService service, ILocalizer localizer, ISessionService session)
    {
        _service = service;
        _localizer = localizer;
        _session = session;

        foreach (var type in Enum.GetValues<AdjustmentType>())
        {
            Types.Add(new AdjustmentTypeChoiceViewModel(type, string.Empty));
        }

        _localizer.LanguageChanged += (_, _) => RefreshStrings();
        Reset();
    }

    /// <summary>Raised when the confirmation's Close button is pressed; the pop-up closes.</summary>
    public event EventHandler? CloseRequested;

    public ObservableCollection<AdjustmentTypeChoiceViewModel> Types { get; } = [];

    /// <summary>The items of the adjustment, in the order they were added.</summary>
    public ObservableCollection<AdjustmentRowViewModel> Rows { get; } = [];

    /// <summary>Matches shown under the search box while typing.</summary>
    public ObservableCollection<StockItem> Suggestions { get; } = [];

    /// <summary>The items written by the last apply.</summary>
    public ObservableCollection<AppliedItemViewModel> CompletedItems { get; } = [];

    /// <summary>True while the "Adjustment applied" confirmation is showing.</summary>
    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    [ObservableProperty]
    public partial string DoneSubtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneIncreasesText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneDecreasesText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DoneNetText { get; set; } = string.Empty;

    /// <summary>Only managers and owners adjust stock (docs/product/roles-and-permissions.md).</summary>
    [ObservableProperty]
    public partial bool HasAccess { get; set; }

    [ObservableProperty]
    public partial AdjustmentTypeChoiceViewModel? SelectedType { get; set; }

    /// <summary>The reason that goes with the chosen type, in the app language; empty until a type is chosen.</summary>
    [ObservableProperty]
    public partial string ReasonText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Reference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    /// <summary>Message under the search box, for example when nothing matches.</summary>
    [ObservableProperty]
    public partial string ScanMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string IncreasesText { get; set; } = "0";

    [ObservableProperty]
    public partial string DecreasesText { get; set; } = "0";

    [ObservableProperty]
    public partial string NetText { get; set; } = "0";

    /// <summary>Why Apply is not available yet; empty when it is.</summary>
    [ObservableProperty]
    public partial string ApplyHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanApply { get; set; }

    public bool NoAccess => !HasAccess;

    public bool HasReason => ReasonText.Length > 0;

    public bool HasNoReason => ReasonText.Length == 0;

    public bool HasRows => Rows.Count > 0;

    public bool IsEmpty => Rows.Count == 0;

    public bool HasScanMessage => ScanMessage.Length > 0;

    public bool HasApplyHint => ApplyHint.Length > 0;

    private AdjustmentType? CurrentType => SelectedType?.Type;

    /// <summary>Starts a fresh, empty adjustment with the signed-in role's permissions.</summary>
    public void Reset()
    {
        IsCompleted = false;
        CompletedItems.Clear();
        Rows.Clear();
        Suggestions.Clear();
        Reference = string.Empty;
        Notes = string.Empty;
        ScanMessage = string.Empty;
        foreach (var type in Types)
        {
            type.IsSelected = false;
        }

        SelectedType = null;
        HasAccess = RolePermissions.IsAllowed(_session.Role, Permission.AdjustStock);
        RefreshStrings();
        Recalculate();
    }

    /// <summary>Chooses the adjustment type. Items already added are brought inside what the new type allows.</summary>
    public void SelectType(AdjustmentTypeChoiceViewModel choice)
    {
        foreach (var type in Types)
        {
            type.IsSelected = ReferenceEquals(type, choice);
        }

        SelectedType = choice;
        foreach (var row in Rows)
        {
            row.Reclamp();
        }

        RefreshStrings();
        Recalculate();
    }

    /// <summary>Fills the suggestions for the text typed in the search box.</summary>
    public void UpdateSuggestions(string text)
    {
        ScanMessage = string.Empty;
        Suggestions.Clear();
        foreach (var match in _service.Search(text, MaxSuggestions))
        {
            Suggestions.Add(match);
        }
    }

    /// <summary>Adds the item a scanned code or typed text matches. Returns false when nothing matches.</summary>
    public bool AddByQuery(string text)
    {
        var query = text.Trim();
        if (query.Length == 0)
        {
            return false;
        }

        var item = _service.FindByCode(query) ?? _service.Search(query, 1).FirstOrDefault();
        if (item is null)
        {
            ScanMessage = _localizer.Format("Adjust_NoMatch", query);
            return false;
        }

        return AddItem(item);
    }

    /// <summary>Adds one item, starting at its current stock. An item already in the list is not added twice.</summary>
    public bool AddItem(StockItem item)
    {
        ScanMessage = string.Empty;
        Suggestions.Clear();
        if (Rows.Any(row => string.Equals(row.Item.ArticleId, item.ArticleId, StringComparison.OrdinalIgnoreCase)))
        {
            ScanMessage = _localizer.Format("Adjust_AlreadyAdded", item.Name);
            return false;
        }

        Rows.Add(new AdjustmentRowViewModel(item, () => CurrentType, RemoveRow, Recalculate, _localizer));
        Recalculate();
        return true;
    }

    partial void OnScanMessageChanged(string value) => OnPropertyChanged(nameof(HasScanMessage));

    partial void OnApplyHintChanged(string value) => OnPropertyChanged(nameof(HasApplyHint));

    partial void OnReasonTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasReason));
        OnPropertyChanged(nameof(HasNoReason));
    }

    partial void OnHasAccessChanged(bool value)
    {
        OnPropertyChanged(nameof(NoAccess));
        Recalculate();
    }

    [RelayCommand]
    private void Apply()
    {
        if (!CanApply || CurrentType is not { } type)
        {
            return;
        }

        var reason = StockAdjustmentRules.Reasons(type)[0];
        var request = new AdjustmentRequest(
            type,
            reason,
            Reference.Trim(),
            Notes.Trim(),
            Rows.Select(row => new AdjustmentItem(row.Item.ArticleId, row.Counted)).ToList());
        var receipt = _service.Apply(request);
        ShowCompleted(receipt, type, reason);
    }

    /// <summary>Leaves the confirmation and starts a fresh adjustment; the pop-up stays open.</summary>
    [RelayCommand]
    private void NewAdjustment() => Reset();

    /// <summary>Closes the confirmation and the pop-up.</summary>
    [RelayCommand]
    private void Done() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void ShowCompleted(AdjustmentReceipt receipt, AdjustmentType type, string reason)
    {
        CompletedItems.Clear();
        foreach (var line in receipt.Items)
        {
            CompletedItems.Add(new AppliedItemViewModel(
                line.Item.Name,
                line.Item.DetailText,
                line.Previous.ToString("N0", _localizer.Culture),
                Signed(line.Difference),
                line.NewStock.ToString("N0", _localizer.Culture),
                line.Difference > 0,
                CompletedItems.Count % 2 == 1));
        }

        var summary = StockAdjustmentRules.Summarize(receipt.Items.Select(line => new AdjustmentLine(line.Previous, line.NewStock)));
        DoneSubtitle = $"{_localizer[$"Adjust_Type_{type}"]} · {_localizer[$"Adjust_Reason_{reason}"]}";
        DoneIncreasesText = Signed(summary.Increases);
        DoneDecreasesText = Signed(-summary.Decreases);
        DoneNetText = Signed(summary.Net);
        IsCompleted = true;
    }

    private void RemoveRow(AdjustmentRowViewModel row)
    {
        Rows.Remove(row);
        Recalculate();
    }

    private void RefreshStrings()
    {
        foreach (var type in Types)
        {
            type.Name = _localizer[$"Adjust_Type_{type.Type}"];
        }

        ReasonText = SelectedType is { } selected
            ? _localizer[$"Adjust_Reason_{StockAdjustmentRules.Reasons(selected.Type)[0]}"]
            : string.Empty;
        foreach (var row in Rows)
        {
            row.RefreshStrings();
        }

        Recalculate();
    }

    /// <summary>Recomputes the summary, the row banding and whether Apply is available.</summary>
    private void Recalculate()
    {
        for (var index = 0; index < Rows.Count; index++)
        {
            Rows[index].IsAlternateRow = index % 2 == 1;
        }

        var lines = Rows.Select(row => new AdjustmentLine(row.Current, row.Counted)).ToList();
        var summary = StockAdjustmentRules.Summarize(lines);
        IncreasesText = Signed(summary.Increases);
        DecreasesText = Signed(-summary.Decreases);
        NetText = Signed(summary.Net);

        var validation = StockAdjustmentRules.Validate(lines);
        ApplyHint = !HasAccess ? string.Empty
            : SelectedType is null ? _localizer["Adjust_HintType"]
            : validation == AdjustmentValidation.NoItems ? _localizer["Adjust_HintItems"]
            : validation == AdjustmentValidation.NothingChanged ? _localizer["Adjust_HintChange"]
            : string.Empty;
        CanApply = HasAccess && SelectedType is not null && validation == AdjustmentValidation.None;

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private string Signed(int value) => value.ToString("+#,0;−#,0;0", _localizer.Culture);
}
