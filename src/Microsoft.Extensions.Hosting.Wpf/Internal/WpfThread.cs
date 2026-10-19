using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Internal implementation of <see cref="IWpfThread{TApplication}"/>.
/// </summary>
/// <remarks>
/// Lifecycle of the UI thread: create and initialize the application and its components, run it, and report it as started
/// once its startup operation (OnStartup) has completed (<see cref="Started"/>). Failures until then fail host startup. Whoever stops the
/// application early (<see cref="StopAndWait"/>) waits for the UI thread to end, so application cleanup never runs
/// against services the host has already disposed.
/// </remarks>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
internal sealed class WpfThread<TApplication>
    : IWpfThread<TApplication>, IDisposable where TApplication : Application, IApplicationInitializeComponent
{
    // Used if HostOptions are not registered; matches the HostOptions.ShutdownTimeout default.
    private const int DefaultStopTimeoutSeconds = 30;

    // How long stopping the application early waits for the UI thread to end: the host's shutdown timeout. Read once, when the
    // thread is created, because the service provider may already be disposing when it is needed.
    private readonly TimeSpan _stopTimeout;

    private readonly IServiceProvider _serviceProvider;
    // Guards _disposed, _stopRequested and IsRunning together, so a stopped thread can never start Application.Run().
    private readonly object _stateLock = new();
    private volatile bool _disposed;
    private volatile bool _stopRequested;
    private readonly WpfContext<TApplication> _wpfContext;
    private Action<WpfContext<TApplication>>? _preContextInitialization;
    private bool _initializationLocked;
    private SynchronizationContext? _synchronizationContext;
    private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    // Only touched on the UI thread (dispatcher hooks).
    private DispatcherOperation? _startupOperation;

    IWpfContext IWpfThread.WpfContext => _wpfContext;

    public IWpfContext<TApplication> WpfContext => _wpfContext;

    public Thread MainThread { get; }

    public SynchronizationContext SynchronizationContext => _synchronizationContext ?? throw new InvalidOperationException("WPF Thread was not started.");

    /// <summary>
    /// Completes once the application is initialized, runs, and its Startup has completed.
    /// Faults with the original exception if creating, initializing or starting it fails.
    /// </summary>
    internal Task Started => _started.Task;

    /// <summary>
    /// Completes when the UI thread has ended: the application exited (Exit handlers ran) and the components were disposed.
    /// Already completed if the thread was never started.
    /// </summary>
    internal Task Completed => (MainThread.ThreadState & ThreadState.Unstarted) != 0 ? Task.CompletedTask : _completed.Task;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceProvider">IServiceProvider</param>
    /// <param name="wpfContext">WpfContext</param>
    public WpfThread(IServiceProvider serviceProvider, WpfContext<TApplication> wpfContext)
    {
        _serviceProvider = serviceProvider;
        _stopTimeout = serviceProvider.GetService<IOptions<HostOptions>>()?.Value.ShutdownTimeout ?? TimeSpan.FromSeconds(DefaultStopTimeoutSeconds);
        _wpfContext = wpfContext;
        //Create a thread which runs the UI
        MainThread = new Thread(InternalUiThreadStart)
        {
            Name = "WPF Main UI Thread",
            IsBackground = true
        };
        // Set the apartment state
        MainThread.SetApartmentState(ApartmentState.STA);
    }

    /// <summary>
    /// Starts the UI thread. Called by WpfHostedService.StartedAsync at the right point of the host lifecycle; not public API.
    /// </summary>
    internal void Start()
    {
        MainThread.Start();
    }

    /// <inheritdoc />
    public void HandleApplicationExit()
    {
        // Stopped by the library because the host failed to start or is being disposed: the host must not be stopped
        // (it is not running, and its service provider may already be disposing).
        if (_disposed || _stopRequested || !_wpfContext.IsRunning)
        {
            return;
        }

        //We need this because otherwise if we have an active open window and call StopApplication, we will get an exception.
        //The exception happens only in certain scenario, for example when UIElement.Visibility is involved.
        //The call of StopApplication happens if HandleApplicationExit was called manually, for example via tray.
        _wpfContext.Dispatcher.InvokeIfRequired(() => _wpfContext.WpfApplication.CloseAllWindowsIfAny());

        var applicationLifeTime = _serviceProvider.GetService<IHostApplicationLifetime>();
        applicationLifeTime?.StopApplication();
    }

    /// <summary>
    /// Stops the application without the host's usual stop (host startup failed or was cancelled, or the host is being
    /// disposed), or prevents it from running if it is still initializing, and waits for the UI thread to end.
    /// </summary>
    internal void StopAndWait()
    {
        bool running;
        lock (_stateLock)
        {
            _stopRequested = true;
            running = _wpfContext.IsRunning;
        }

        if (running && _wpfContext.WpfApplication is { } application)
        {
            var dispatcher = application.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                // Called on the UI thread itself: it cannot wait for its own end.
                application.Shutdown();
                return;
            }

            // Ignored if WPF is already shutting down; the dispatcher then aborts the operation.
            _ = dispatcher.InvokeAsync(() => application.Shutdown());
        }

        if (MainThread.IsAlive && Thread.CurrentThread != MainThread)
        {
            MainThread.Join(_stopTimeout);
        }
    }

    /// <summary>
    /// Called by the DI container when the host (its service provider) is disposed; explicit so only the container calls it.
    /// Normally the UI thread has already ended: StoppingAsync waits for it, and <see cref="WpfStartupGuard{TApplication}"/> stops it
    /// if the host never finished starting. This is the last resort: no UI thread may outlive the host.
    /// </summary>
    void IDisposable.Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAndWait();
    }

    /// <summary>
    /// Start UI
    /// </summary>
    private void InternalUiThreadStart()
    {
        try
        {
            RunUiThread();
        }
        finally
        {
            // Startup did not complete (e.g. the application shut itself down in OnStartup): nothing left to wait for.
            _started.TrySetResult(true);
            _completed.TrySetResult(true);
        }
    }

    private void RunUiThread()
    {
        // Disposed when the UI thread ends, also if a later component fails to initialize.
        using var wpfComponents = new DisposableList<IWpfComponent>();
#pragma warning disable CA1031 // The exception is not swallowed: it is rethrown on the host thread by WpfHostedService.StartedAsync via Started.
        try
        {
            // Do the pre initialization, if any
            PreUiThreadStart();
            InitializeWpfComponents(wpfComponents);
        }
        catch (Exception exception)
        {
            // An exception escaping this background thread would kill the process without being logged.
            _started.TrySetException(exception);
            return;
        }

        var application = _wpfContext.WpfApplication!;
        lock (_stateLock)
        {
            // Stopped or disposed while initializing (e.g. startup was cancelled): never run the application without a host.
            if (_disposed || _stopRequested)
            {
                _started.TrySetCanceled();
                return;
            }

            // Mark the application as running
            _wpfContext.IsRunning = true;
        }

        try
        {
            //Blocking method until application exit.
            application.Run();
        }
        catch (Exception exception) when (!_started.Task.IsCompleted)
        {
            // OnStartup or a Startup handler failed: fail host startup instead of crashing the process.
            _started.TrySetException(exception);
        }
        finally
        {
            // WPF has exited (on its own or via StoppingAsync), the dispatcher is shut down.
            _wpfContext.IsRunning = false;
        }
#pragma warning restore CA1031
    }

    /// <summary>
    /// Identifies the dispatcher operation that runs the application's startup (Startup event, OnStartup): Application's
    /// constructor sets Application.Current and then queues it, so it is the first operation queued once Application.Current
    /// exists. Work a custom application factory queues before constructing the application is ignored (Current is still null).
    /// The completion of that operation, not dispatcher priority or order, marks startup as done: OnStartup may run nested
    /// dispatcher loops (e.g. ShowDialog), in which any other queued operation would run while OnStartup has not returned yet.
    /// </summary>
    private void OnDispatcherOperationPosted(object? sender, DispatcherHookEventArgs e)
    {
        // The constructor queues it on the UI thread; work posted concurrently from other threads (this hook runs on the
        // posting thread) is ignored, which also keeps _startupOperation a UI-thread-only field.
        if (e.Dispatcher.CheckAccess() && _startupOperation is null && Application.Current is not null)
        {
            _startupOperation = e.Operation;
        }
    }

    private void OnDispatcherOperationCompleted(object? sender, DispatcherHookEventArgs e)
    {
        // An operation that throws does not complete: the exception then fails host startup (see RunUiThread).
        if (_startupOperation is null || e.Operation != _startupOperation)
        {
            return;
        }

        var hooks = e.Dispatcher.Hooks;
        hooks.OperationCompleted -= OnDispatcherOperationCompleted;
        OnApplicationStarted();
    }

    /// <summary>
    /// Runs on the UI thread once the application's startup (OnStartup) has returned.
    /// </summary>
    private void OnApplicationStarted()
    {
        try
        {
            // Resolved after everything the application resolved during startup (including in OnStartup), so the container,
            // which disposes in reverse creation order, disposes it before those services: if the host never finishes
            // starting, it stops the application while they still work.
            _serviceProvider.GetRequiredService<WpfStartupGuard<TApplication>>();
        }
        catch (ObjectDisposedException)
        {
            // The host is already being disposed.
            _started.TrySetCanceled();
            _wpfContext.WpfApplication?.Shutdown();
            return;
        }

        _started.TrySetResult(true);
    }

    /// <summary>
    /// Do all the pre work, before the UI thread can start
    /// </summary>
    private void PreUiThreadStart()
    {
        // Create our SynchronizationContext, and install it:
        var dispatcher = Dispatcher.CurrentDispatcher;
        var synchronizationContext = new DispatcherSynchronizationContext(dispatcher);
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        _synchronizationContext = synchronizationContext;

        // Before the application is created, to see its constructor queue the startup operation.
        dispatcher.Hooks.OperationPosted += OnDispatcherOperationPosted;
        dispatcher.Hooks.OperationCompleted += OnDispatcherOperationCompleted;

        TApplication application;
        try
        {
            application = CreateApplication();
        }
        finally
        {
            dispatcher.Hooks.OperationPosted -= OnDispatcherOperationPosted;
        }

        if (_startupOperation is null)
        {
            // Not expected with WPF's Application: fail loudly instead of reporting startup at a wrong time.
            throw new InvalidOperationException($"Could not identify the startup operation of {typeof(TApplication).Name}.");
        }

        // Register to the WPF application exit to stop the host application, whatever host lifetime is used.
        // This must happen here, on the UI thread before Run(): WpfLifetime used to subscribe in ApplicationStarted instead,
        // which could run before the application was created and then never stopped the host.
        application.Exit += (_, _) =>
        {
            HandleApplicationExit();
        };

        // Store the application for others to interact
        _wpfContext.SetWpfApplication(application);
        //Initialize all internal app properties
        application.InitializeComponent();
        _preContextInitialization?.Invoke(_wpfContext);
    }

    private void InitializeWpfComponents(DisposableList<IWpfComponent> wpfComponents)
    {
        var wpfComponentsFunc = _serviceProvider.GetServices<Func<IWpfComponent>>();
        foreach (var wpfComponentFunc in wpfComponentsFunc)
        {
            var wpfComponent = wpfComponentFunc.Invoke();
            // Track it before initializing, so it is disposed even if InitializeComponent throws.
            wpfComponents.Add(wpfComponent);
            wpfComponent.InitializeComponent();
        }
    }

    private TApplication CreateApplication()
    {
        var applicationFunction = _serviceProvider.GetRequiredService<Func<TApplication>>();

        return applicationFunction.Invoke();
    }

    /// <summary>
    /// Pre initialization that happens before <see cref="Application.Run()"/>. This action happens on UI thread.
    /// </summary>
    internal void SetPreContextInitialization(Action<IWpfContext<TApplication>> preContextInitialization)
    {
        // After Start() the UI thread may already have run (or be running) the initialization step, so a late call would be ignored or race.
        if ((MainThread.ThreadState & ThreadState.Unstarted) == 0)
        {
            throw new InvalidOperationException(WpfInitialization<TApplication>.AlreadyStartedMessage);
        }

        if (_initializationLocked)
        {
            throw new InvalidOperationException(WpfInitialization<TApplication>.AlreadyConfiguredMessage);
        }

        _preContextInitialization = preContextInitialization;
        _initializationLocked = true;
    }
}
