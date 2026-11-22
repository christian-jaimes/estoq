using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Estoq.App.Services;

namespace Estoq.App.ViewModels.Setup;

/// <summary>
/// Step 5. The recovery key is shown once; the owner must confirm it is stored safely before finishing.
/// The key is generated on first entry and kept only until the wizard completes.
/// </summary>
public sealed partial class RecoveryStepViewModel : WizardStepViewModel
{
    private readonly ISetupService _setup;
    private readonly IClipboardService _clipboard;
    private readonly IRecoveryKeyPrinter _printer;

    public RecoveryStepViewModel(
        ILocalizer localizer,
        ISetupService setup,
        IClipboardService clipboard,
        IRecoveryKeyPrinter printer)
        : base(localizer, "Recovery", "Recovery")
    {
        _setup = setup;
        _clipboard = clipboard;
        _printer = printer;
    }

    [ObservableProperty]
    public partial string RecoveryKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasConfirmed { get; set; }

    [ObservableProperty]
    public partial bool IsKeyCopied { get; set; }

    [ObservableProperty]
    public partial string PrintMessage { get; set; } = string.Empty;

    public string CopyText => Localizer[IsKeyCopied ? "Common_Copied" : "Common_Copy"];

    /// <summary>Raised when the confirmation changes, so the wizard can enable Finish.</summary>
    public event EventHandler? ConfirmationChanged;

    public override void OnEntered()
    {
        if (RecoveryKey.Length == 0)
        {
            RecoveryKey = _setup.GenerateRecoveryKey();
        }
    }

    protected override bool Evaluate() => HasConfirmed;

    /// <summary>Forgets the key and the confirmation after setup completes; it cannot be shown again.</summary>
    public void Clear()
    {
        RecoveryKey = string.Empty;
        HasConfirmed = false;
    }

    [RelayCommand]
    private async Task CopyKeyAsync()
    {
        _clipboard.SetText(RecoveryKey, isSensitive: true);
        IsKeyCopied = true;
        await Task.Delay(TimeSpan.FromSeconds(2));
        IsKeyCopied = false;
    }

    [RelayCommand]
    private async Task PrintKeyAsync()
    {
        var printed = await _printer.PrintAsync(
            Localizer["Recovery_PrintTitle"],
            Localizer["Recovery_PrintNote"],
            RecoveryKey);
        PrintMessage = printed ? string.Empty : Localizer["Recovery_PrintFailed"];
    }

    partial void OnIsKeyCopiedChanged(bool value) => OnPropertyChanged(nameof(CopyText));

    partial void OnHasConfirmedChanged(bool value) => ConfirmationChanged?.Invoke(this, EventArgs.Empty);
}
