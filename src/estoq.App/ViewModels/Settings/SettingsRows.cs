using CommunityToolkit.Mvvm.ComponentModel;
using Estoq.App.Services;
using Estoq.Core.Roles;
using Estoq.Core.Settings;

namespace Estoq.App.ViewModels.Settings;

/// <summary>One entry of the section list on the left of the Settings pop-up.</summary>
public sealed partial class SettingsSectionItemViewModel : ObservableObject
{
    public SettingsSectionItemViewModel(SettingsSection section, string glyph)
    {
        Section = section;
        Glyph = glyph;
    }

    public SettingsSection Section { get; }

    /// <summary>Material glyph for the section.</summary>
    public string Glyph { get; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>One audited change in a History list, already worded for the current language.</summary>
public sealed class HistoryRowViewModel
{
    public HistoryRowViewModel(SettingChange change, ILocalizer localizer, bool isAlternate)
    {
        var (key, who) = change.Field.Contains('|') ? (change.Field.Split('|')[0], change.Field.Split('|')[1]) : (change.Field, null);
        Field = who is null ? localizer[$"SettingsField_{key}"] : localizer.Format($"SettingsField_{key}", who);
        OldValue = Describe(key, change.OldValue, localizer);
        NewValue = Describe(key, change.NewValue, localizer);
        By = change.User;
        When = change.At.ToLocalTime().ToString("g", localizer.Culture);
        IsAlternateRow = isAlternate;
    }

    public string Field { get; }

    public string OldValue { get; }

    public string NewValue { get; }

    public string By { get; }

    public string When { get; }

    public bool IsAlternateRow { get; }

    private static string Describe(string field, string? value, ILocalizer localizer)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "—";
        }

        return field is "TaxMode" or "StoreLanguage" or "StaffRole" or "StaffStatus"
            ? localizer[$"SettingsValue_{value}"]
            : value;
    }
}

/// <summary>A role as shown in pickers and badges.</summary>
public sealed record RoleChoice(UserRole Role, string Name);
