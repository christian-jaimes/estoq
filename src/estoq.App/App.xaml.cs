using Estoq.App.Services;
using Estoq.App.ViewModels;
using Estoq.App.ViewModels.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace Estoq.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    // Testing shortcut: skip first-run setup and launch directly at sign-in.
    private const bool SkipSetupForTesting = true;

    private Window? _window;
    private ILogger<App>? _logger;

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            _logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled domain exception");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _logger?.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    public static new App Current => (App)Microsoft.UI.Xaml.Application.Current;

    public IServiceProvider Services { get; }

    /// <summary>The main window; used for dialogs that need its handle (file picker, printing).</summary>
    public MainWindow? MainWindow => _window as MainWindow;

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information).AddDebug());

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IPopupService, PopupService>();
        services.AddSingleton<INotificationService, MockNotificationService>();
        services.AddSingleton<NotificationsViewModel>();
        services.AddSingleton<PopupViewModel>();
        services.AddSingleton<ShellViewModel>();

        // Setup wizard. The view models are singletons so a language change can rebuild the page without losing input.
        services.AddSingleton<ILocalizer, Localizer>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IAccountService, SampleAccountService>();
        services.AddSingleton<ISecuritySettings, SecuritySettings>();
        services.AddSingleton<IInactivityMonitor, InactivityMonitor>();
        services.AddTransient<SignInViewModel>();
        services.AddSingleton<ISetupService, SampleSetupService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<ILogoPickerService, LogoPickerService>();
        services.AddSingleton<IRecoveryKeyPrinter, RecoveryKeyPrinter>();
        services.AddSingleton<WelcomeStepViewModel>();
        services.AddSingleton<OwnerStepViewModel>();
        services.AddSingleton<StoreStepViewModel>();
        services.AddSingleton<PreferencesStepViewModel>();
        services.AddSingleton<RecoveryStepViewModel>();
        services.AddSingleton<SetupWizardViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PlaceholderViewModel>();
        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _logger = Services.GetRequiredService<ILogger<App>>();
        _window = new MainWindow();
        _window.Activate();

        // Splash for two seconds, then: sign-in for testing or once setup is complete; otherwise the first-run wizard.
        var window = (MainWindow)_window;
        window.ShowSplash(() =>
        {
            if (SkipSetupForTesting || Services.GetRequiredService<ISetupService>().IsSetupComplete)
            {
                window.ShowSignIn();
            }
            else
            {
                window.ShowSetup();
            }
        });
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled UI exception");
        e.Handled = true;
    }
}
