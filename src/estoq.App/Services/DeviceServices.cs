using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Estoq.App.Services;

public interface IClipboardService
{
    /// <summary>Copies text. A sensitive value is kept out of clipboard history and cloud sync.</summary>
    void SetText(string text, bool isSensitive = false);
}

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text, bool isSensitive = false)
    {
        var package = new DataPackage();
        package.SetText(text);
        if (isSensitive)
        {
            Clipboard.SetContentWithOptions(package, new ClipboardContentOptions { IsAllowedInHistory = false, IsRoamable = false });
        }
        else
        {
            Clipboard.SetContent(package);
        }
    }
}

public sealed record PickedLogo(string Path, string FileName, long SizeInBytes);

public interface ILogoPickerService
{
    /// <summary>Opens the file picker for a PNG or JPG. Null when cancelled.</summary>
    Task<PickedLogo?> PickAsync();

    /// <summary>Opens the file picker for several PNG or JPG images. Empty when cancelled.</summary>
    Task<IReadOnlyList<PickedLogo>> PickManyAsync();

    Task<ImageSource?> LoadPreviewAsync(string path);
}

public sealed class LogoPickerService : ILogoPickerService
{
    private static void ConfigurePicker(FileOpenPicker picker)
    {
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
    }

    private static bool InitializePicker(FileOpenPicker picker)
    {
        var window = App.Current.MainWindow;
        if (window is null) return false;
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
        ConfigurePicker(picker);
        return true;
    }

    public async Task<PickedLogo?> PickAsync()
    {
        var window = App.Current.MainWindow;
        if (window is null)
        {
            return null;
        }

        var picker = new FileOpenPicker();
        InitializePicker(picker);

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return null;
        }

        var properties = await file.GetBasicPropertiesAsync();
        return new PickedLogo(file.Path, file.Name, (long)properties.Size);
    }

    public async Task<IReadOnlyList<PickedLogo>> PickManyAsync()
    {
        var picker = new FileOpenPicker();
        if (!InitializePicker(picker)) return Array.Empty<PickedLogo>();
        var files = await picker.PickMultipleFilesAsync();
        var picked = new List<PickedLogo>(files.Count);
        foreach (var file in files)
        {
            var properties = await file.GetBasicPropertiesAsync();
            picked.Add(new PickedLogo(file.Path, file.Name, (long)properties.Size));
        }
        return picked;
    }

    public async Task<ImageSource?> LoadPreviewAsync(string path)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            var bitmap = new BitmapImage { DecodePixelWidth = 400 };
            await bitmap.SetSourceAsync(stream);
            return bitmap;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }
}

public interface IRecoveryKeyPrinter
{
    /// <summary>Opens the Windows print dialog with the key on one page. False when printing could not start.</summary>
    Task<bool> PrintAsync(string title, string note, string recoveryKey);
}

public sealed class RecoveryKeyPrinter : IRecoveryKeyPrinter
{
    public async Task<bool> PrintAsync(string title, string note, string recoveryKey)
    {
        var window = App.Current.MainWindow;
        if (window is null || !PrintManager.IsSupported())
        {
            return false;
        }

        var handle = WindowNative.GetWindowHandle(window);
        var document = new PrintDocument();

        UIElement BuildPage() => new StackPanel
        {
            Margin = new Thickness(96),
            Spacing = 24,
            Children =
            {
                new TextBlock { Text = title, FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold },
                new TextBlock { Text = recoveryKey, FontSize = 26, FontFamily = new FontFamily("Consolas"), IsTextSelectionEnabled = false },
                new TextBlock { Text = note, FontSize = 14, TextWrapping = TextWrapping.Wrap },
            },
        };

        document.Paginate += (_, _) => document.SetPreviewPageCount(1, PreviewPageCountType.Final);
        document.GetPreviewPage += (_, e) => document.SetPreviewPage(e.PageNumber, BuildPage());
        document.AddPages += (_, _) =>
        {
            document.AddPage(BuildPage());
            document.AddPagesComplete();
        };

        var manager = PrintManagerInterop.GetForWindow(handle);
        void OnRequested(PrintManager sender, PrintTaskRequestedEventArgs args) =>
            args.Request.CreatePrintTask(title, request => request.SetSource(document.DocumentSource));

        manager.PrintTaskRequested += OnRequested;
        try
        {
            return await PrintManagerInterop.ShowPrintUIForWindowAsync(handle);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            return false;
        }
        finally
        {
            manager.PrintTaskRequested -= OnRequested;
        }
    }
}

public interface IReceiptPrinter
{
    Task<bool> PrintAsync(string title, string receiptText);
}

public sealed class ReceiptPrinter : IReceiptPrinter
{
    public async Task<bool> PrintAsync(string title, string receiptText)
    {
        var window = App.Current.MainWindow;
        if (window is null || !PrintManager.IsSupported())
        {
            return false;
        }

        var handle = WindowNative.GetWindowHandle(window);
        var document = new PrintDocument();
        UIElement BuildPage() => new Border
        {
            Padding = new Thickness(48),
            Child = new TextBlock
            {
                Text = receiptText,
                FontSize = 16,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = false,
            },
        };

        document.Paginate += (_, _) => document.SetPreviewPageCount(1, PreviewPageCountType.Final);
        document.GetPreviewPage += (_, e) => document.SetPreviewPage(e.PageNumber, BuildPage());
        document.AddPages += (_, _) =>
        {
            document.AddPage(BuildPage());
            document.AddPagesComplete();
        };

        var manager = PrintManagerInterop.GetForWindow(handle);
        void OnRequested(PrintManager sender, PrintTaskRequestedEventArgs args) =>
            args.Request.CreatePrintTask(title, request => request.SetSource(document.DocumentSource));

        manager.PrintTaskRequested += OnRequested;
        try
        {
            return await PrintManagerInterop.ShowPrintUIForWindowAsync(handle);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException or InvalidOperationException)
        {
            return false;
        }
        finally
        {
            manager.PrintTaskRequested -= OnRequested;
        }
    }
}
