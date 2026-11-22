using System.Globalization;
using System.Resources;
using Estoq.Core.Setup;

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
}

public sealed class Localizer : ILocalizer
{
    private static readonly ResourceManager Resources =
        new("Estoq.App.Resources.Strings", typeof(Localizer).Assembly);

    public Localizer()
    {
        // First launch follows the Windows display language; the wizard lets the owner change it.
        var spanish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es";
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
