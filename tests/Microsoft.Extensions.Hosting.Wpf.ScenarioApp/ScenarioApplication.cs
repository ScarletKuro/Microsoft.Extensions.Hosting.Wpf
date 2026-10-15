using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf.ScenarioApp;

/// <summary>View model locator passed to <see cref="ScenarioApplication.InitializeLocator"/>; <paramref name="Source"/> tells which overload created it.</summary>
public sealed record ScenarioLocator(string Source);

/// <summary>
/// An application-owned disposable singleton, resolved by the application and its components during startup.
/// WPF cleanup (<c>OnExit</c>, component disposal) uses it, so it must run before the host disposes it.
/// </summary>
public class ScenarioResource : IDisposable
{
    private readonly ScenarioLog _log;
    private volatile bool _disposed;

    public ScenarioResource(ScenarioLog log)
    {
        _log = log;
    }

    /// <summary>Prefix of the logged events, e.g. <c>ResourceDisposed</c>.</summary>
    protected virtual string Name => "Resource";

    public void Use(string user)
    {
        if (_disposed)
        {
            _log.Info($"{Name}UsedAfterDispose", user);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _disposed = true;
        _log.Event($"{Name}Disposed");
    }
}

/// <summary>
/// Resolved only after the host has started (like a view model created when a window opens), so it is created after
/// everything resolved during startup. <c>OnExit</c> uses it, so the host must not dispose services before WPF has exited.
/// </summary>
public sealed class LateScenarioResource : ScenarioResource
{
    public LateScenarioResource(ScenarioLog log)
        : base(log)
    {
    }

    protected override string Name => "LateResource";
}

/// <summary>
/// A WPF application with the default <see cref="ShutdownMode.OnLastWindowClose"/> that shows a window on startup,
/// like a real application, and logs every lifecycle step.
/// </summary>
public sealed class ScenarioApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<ScenarioLocator>
{
    private readonly ScenarioLog _log;
    private readonly ScenarioResource _resource;
    private ScenarioResource? _lateResource;
    private readonly Scenario _scenario;

    public ScenarioApplication(ScenarioLog log, ScenarioResource resource, Scenario scenario)
    {
        _log = log;
        _resource = resource;
        _scenario = scenario;
        _log.Event("Constructor");
    }

    /// <summary>Completes with the main window once <see cref="OnStartup"/> has shown it.</summary>
    public TaskCompletionSource<Window> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Hands the application a service resolved after the host has started; <see cref="OnExit"/> uses it.</summary>
    public void UseLateResource(ScenarioResource resource) => _lateResource = resource;

    public void InitializeComponent() => _log.Event("InitializeComponent");

    public void Initialize() => _log.Event("Initialize");

    public void InitializeLocator(ScenarioLocator viewModelLocator) => _log.Event("InitializeLocator", viewModelLocator?.Source ?? "null");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _log.Event("Startup");
        if (_scenario.Failure == FailureKind.ApplicationStartup)
        {
            throw new InvalidOperationException(Scenario.StartupFailureMessage);
        }


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
        _resource.Use("OnExit");
        _lateResource?.Use("OnExit");
        base.OnExit(e);
    }
}

/// <summary>Stands in for a tray icon: an <see cref="IWpfComponent"/> created and disposed by the library on the UI thread.</summary>
public sealed class ScenarioTrayIcon : ITrayIcon
{
    private readonly ScenarioLog _log;
    private readonly ScenarioResource _resource;

    public ScenarioTrayIcon(ScenarioLog log, ScenarioResource resource)
    {
        _log = log;
        _resource = resource;
    }

    public void InitializeComponent() => _log.Event("TrayInitialize");

    public void Dispose()
    {
        _log.Event("TrayDispose");
        _resource.Use("TrayDispose");
    }
}
