using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Internal implementation of <see cref="IWpfThread{TApplication}"/>. 
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
internal sealed class WpfThread<TApplication>
    : IWpfThread<TApplication> where TApplication : Application, IApplicationInitializeComponent
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WpfContext<TApplication> _wpfContext;
    private Action<WpfContext<TApplication>>? _preContextInitialization;
    private bool _initializationLocked;
    private SynchronizationContext? _synchronizationContext;
    private readonly TaskCompletionSource<bool> _initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);

    IWpfContext IWpfThread.WpfContext => _wpfContext;

    public IWpfContext<TApplication> WpfContext => _wpfContext;

    public Thread MainThread { get; }

    public SynchronizationContext SynchronizationContext => _synchronizationContext ?? throw new InvalidOperationException("WPF Thread was not started.");

    /// <summary>
    /// Completes once the application is created and initialized on the UI thread, right before <see cref="Application.Run()"/>.
    /// Faults with the original exception if that fails.
    /// </summary>
    internal Task Initialized => _initialized.Task;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceProvider">IServiceProvider</param>
    /// <param name="wpfContext">WpfContext</param>
    public WpfThread(IServiceProvider serviceProvider, WpfContext<TApplication> wpfContext)
    {
        _serviceProvider = serviceProvider;
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

    /// <inheritdoc />
    public void Start()
    {
        MainThread.Start();
    }

    /// <inheritdoc />
    public void HandleApplicationExit()
    {
        if (!_wpfContext.IsRunning)
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
    /// Start UI
    /// </summary>
    private void InternalUiThreadStart()
    {
        // Disposed when the UI thread ends, also if a later component fails to initialize.
        using var wpfComponents = new DisposableList<IWpfComponent>();
#pragma warning disable CA1031 // The exception is not swallowed: it is rethrown on the host thread by WpfHostedService.StartAsync via Initialized.
        try
        {
            // Do the pre initialization, if any
            PreUiThreadStart();
            InitializeWpfComponents(wpfComponents);
        }
        catch (Exception exception)
        {
            // An exception escaping this background thread would kill the process without being logged.
            _initialized.TrySetException(exception);
            return;
        }
#pragma warning restore CA1031

        // Run the actual code
        UiThreadStart();
    }

    /// <summary>
    /// Do all the pre work, before the UI thread can start
    /// </summary>
    private void PreUiThreadStart()
    {
        // Create our SynchronizationContext, and install it:
        var synchronizationContext = new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        _synchronizationContext = synchronizationContext;

        var application = CreateApplication();

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

    /// <summary>
    /// Implement all the code which is needed to run the actual UI
    /// </summary>
    private void UiThreadStart()
    {
        // Mark the application as running
        _wpfContext.IsRunning = true;
        // Let WpfHostedService.StartAsync complete
        _initialized.TrySetResult(true);

        //Blocking method until application exit.
        _wpfContext.WpfApplication?.Run();
        // WPF has exited (on its own or via StopAsync), the dispatcher is shut down.
        _wpfContext.IsRunning = false;
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
        if (_initializationLocked)
        {
            throw new InvalidOperationException(WpfInitialization<TApplication>.AlreadyConfiguredMessage);
        }

        _preContextInitialization = preContextInitialization;
        _initializationLocked = true;
    }
}