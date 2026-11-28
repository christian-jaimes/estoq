using System.Text;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Estoq.App.Services;

public interface IReportExportService
{
    /// <summary>Asks where to save and writes <paramref name="csv"/> there (UTF-8 with a byte-order mark, so spreadsheets read accents). Null when cancelled.</summary>
    Task<string?> SaveCsvAsync(string suggestedName, string csv);
}

public sealed class ReportExportService : IReportExportService
{
    public async Task<string?> SaveCsvAsync(string suggestedName, string csv)
    {
        var window = App.Current.MainWindow;
        if (window is null)
        {
            return null;
        }

        var picker = new FileSavePicker { SuggestedFileName = suggestedName };
        picker.FileTypeChoices.Add("CSV", [".csv"]);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return null;
        }

        await FileIO.WriteBytesAsync(file, [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv)]);
        return file.Path;
    }
}
