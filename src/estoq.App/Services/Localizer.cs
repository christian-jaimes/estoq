using System.Globalization;
using System.Resources;
using Estoq.Core.Setup;
using Windows.Storage;

namespace Estoq.App.Services;

/// <summary>Looks up user-facing text by key in the .resx files (English neutral, Spanish satellite).</summary>
public interface ILocalizer
{
    AppLanguage Language { get; }

    CultureInfo Culture { get; }

    /// <summary>Raised after the language changes. Screens built earlier must be rebuilt or refreshed.</summary>
    event EventHandler? LanguageChanged;

    /// <summary>The text for <paramref name="key"/>; the key itself when it is missing, so gaps are visible.</summary>
    string this[string key] { get; }

    string Format(string key, params object[] args);

    void SetLanguage(AppLanguage language);

    /// <summary>Saves the current language as the store language, so the next launch starts in it.</summary>
    void Remember();
}

public sealed class Localizer : ILocalizer
{
    private const string SettingKey = "store_language";
    private const string SpanishCode = "es";

    private static readonly ResourceManager Resources =
        new("Estoq.App.Resources.Strings", typeof(Localizer).Assembly);

    public Localizer()
    {
        // The saved store language wins; before setup is done the Windows display language is the starting point.
        var saved = LoadSaved();
        var spanish = saved is { } code
            ? code == SpanishCode
            : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == SpanishCode;
        Apply(spanish ? AppLanguage.Spanish : AppLanguage.English);
    }

    public event EventHandler? LanguageChanged;

    public AppLanguage Language { get; private set; }

    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en-US");

    public string this[string key] => Resources.GetString(key, Culture) ?? key;

    public string Format(string key, params object[] args) => string.Format(Culture, this[key], args);

    public void SetLanguage(AppLanguage language)
    {
        if (language == Language)
        {
            return;
        }

        Apply(language);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Remember()
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[SettingKey] = Language == AppLanguage.Spanish ? SpanishCode : "en";
        }
        catch (InvalidOperationException)
        {
            // Not running packaged: nothing to persist to; the language still applies for this run.
        }
    }

    private static string? LoadSaved()
    {
        try
        {
            return ApplicationData.Current.LocalSettings.Values[SettingKey] as string;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void Apply(AppLanguage language)
    {
        Language = language;
        Culture = CultureInfo.GetCultureInfo(language == AppLanguage.Spanish ? "es" : "en-US");

        // Region and date names that come from .NET follow the UI culture.
        CultureInfo.CurrentUICulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
    }
}

/// <summary>Static access for x:Bind function bindings: <c>{x:Bind services:Loc.Get('Key')}</c>. Evaluated once, when a page is built.</summary>
public static class Loc
{
    public static string Get(string key) => AppServices.Get<ILocalizer>()[key];
}
