using System.Text;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Estoq.App.Services;

/// <summary>Writes the exported sales to a file the user picks.</summary>
public interface ISalesExporter
{
    /// <summary>Asks where to save and writes <paramref name="csv"/> as UTF-8 with a byte-order mark, so spreadsheets read accents right. Null when cancelled.</summary>
    Task<string?> SaveCsvAsync(string suggestedName, string fileTypeLabel, string csv);
}

public sealed class SalesExporter : ISalesExporter
{
    public async Task<string?> SaveCsvAsync(string suggestedName, string fileTypeLabel, string csv)
    {
        var window = App.Current.MainWindow;
        if (window is null)
        {
            return null;
        }

        var picker = new FileSavePicker { SuggestedFileName = suggestedName };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
        picker.FileTypeChoices.Add(fileTypeLabel, [".csv"]);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return null;
        }

        await FileIO.WriteBytesAsync(file, Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray());
        return file.Name;
    }
}
