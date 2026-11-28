using System.Text;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Estoq.App.Services;

/// <summary>Asks where to save a CSV file and writes it.</summary>
public interface ICsvExportService
{
    /// <summary>Opens the save dialog and writes <paramref name="csv"/>. The file name chosen, or null when cancelled.</summary>
    Task<string?> SaveAsync(string suggestedFileName, string csv);
}

public sealed class CsvExportService : ICsvExportService
{
    public async Task<string?> SaveAsync(string suggestedFileName, string csv)
    {
        var window = App.Current.MainWindow;
        if (window is null)
        {
            return null;
        }

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName,
        };
        picker.FileTypeChoices.Add("CSV", [".csv"]);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return null;
        }

        // A byte order mark makes Excel read accented characters (Spanish names) correctly.
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(csv)).ToArray();
        await FileIO.WriteBytesAsync(file, bytes);
        return file.Name;
    }
}
