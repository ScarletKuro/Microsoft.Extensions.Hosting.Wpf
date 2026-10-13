using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf.ScenarioApp;

/// <summary>View model locator passed to <see cref="ScenarioApplication.InitializeLocator"/>.</summary>
public sealed class ScenarioLocator;

/// <summary>
/// A WPF application with the default <see cref="ShutdownMode.OnLastWindowClose"/> that shows a window on startup,
/// like a real application, and logs every lifecycle step.
/// </summary>
public sealed class ScenarioApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<ScenarioLocator>
{
    private readonly ScenarioLog _log;

    public ScenarioApplication(ScenarioLog log)
    {
        _log = log;
        _log.Event("Constructor");
    }

    /// <summary>Completes with the main window once <see cref="OnStartup"/> has shown it.</summary>
    public TaskCompletionSource<Window> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void InitializeComponent() => _log.Event("InitializeComponent");

    public void Initialize() => _log.Event("Initialize");

    public void InitializeLocator(ScenarioLocator viewModelLocator) => _log.Event("InitializeLocator");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _log.Event("Startup");

        var window = new Window
        {
            Title = "Scenario",
            Width = 200,
            Height = 100,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32000,
            Top = -32000,
        };
        window.Show();
        _log.Event("WindowShown");
        Started.SetResult(window);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _log.Event("Exit", e.ApplicationExitCode);
        base.OnExit(e);
    }
}

/// <summary>Stands in for a tray icon: an <see cref="IWpfComponent"/> created and disposed by the library on the UI thread.</summary>
public sealed class ScenarioTrayIcon : ITrayIcon
{
    private readonly ScenarioLog _log;

    public ScenarioTrayIcon(ScenarioLog log)
    {
        _log = log;
    }

    public void InitializeComponent() => _log.Event("TrayInitialize");

    public void Dispose() => _log.Event("TrayDispose");
}
