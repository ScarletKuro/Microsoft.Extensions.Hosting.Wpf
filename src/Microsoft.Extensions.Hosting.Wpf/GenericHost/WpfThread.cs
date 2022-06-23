using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost;

/// <summary>
/// Internal implementation of <see cref="IWpfThread{TApplication}"/>. 
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
internal class WpfThread<TApplication>
    : IWpfThread<TApplication> where TApplication : Application, IApplicationInitializeComponent, new()
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WpfContext<TApplication> _wpfContext;
    private Action<WpfContext<TApplication>>? _preContextInitialization;
    private SynchronizationContext? _synchronizationContext;

    IWpfContext IWpfThread.WpfContext => _wpfContext;

    public IWpfContext<TApplication> WpfContext => _wpfContext;

    public Thread MainThread { get; }

    public SynchronizationContext SynchronizationContext => _synchronizationContext ?? throw new InvalidOperationException("WPF Thread was not started.");

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
        _wpfContext.WpfApplication.CloseAllWindowsIfAny();

        var applicationLifeTime = _serviceProvider.GetService<IHostApplicationLifetime>();
        applicationLifeTime?.StopApplication();
    }

    /// <summary>
    /// Start UI
    /// </summary>
    private void InternalUiThreadStart()
    {
        // Do the pre initialization, if any
        PreUiThreadStart();
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
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        _synchronizationContext = synchronizationContext;
            
        var application = CreateApplication();

        //We must set this if default / third party lifetime is used.
        //Only observe event if we don't have WpfLifetime linked that already listens and calls StopApplication on demand
        if (!_wpfContext.IsLifetimeLinked)
        {
            // Register to the WPF application exit to stop the host application
            application.Exit += (_, _) =>
            {
                HandleApplicationExit();
            };
        }

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
        //Since tray icon should be created in the STA thread we have to use lambda
        //TODO: make a better abstraction to make possible to move TrayIcon to separate library.
        var trayIconFunction = _serviceProvider.GetService<Func<IWpfThread<TApplication>, ITrayIcon<TApplication>>>();
        if (trayIconFunction is not null)
        {
            // Create icon if we used <see cref="WpfHostingExtensions.AddWpfTrayIcon{TTrayIcon, TApplication}"/>
            using var trayIcon = trayIconFunction(this);
            trayIcon.CreateNotifyIcon();
            _wpfContext.WpfApplication?.Run();
        }
        else
        {
            _wpfContext.WpfApplication?.Run();
        }
    }

    private TApplication CreateApplication()
    {
        var applicationFunction = _serviceProvider.GetService<Func<IServiceProvider, TApplication>>();
        if (applicationFunction is not null)
        {
            return applicationFunction(_serviceProvider);
        }

        return new TApplication();
    }

    /// <summary>
    /// Pre initialization that happens before <see cref="Application.Run()"/>. This action happens on UI thread.
    /// </summary>
    internal void SetPreContextInitialization(Action<IWpfContext<TApplication>> preContextInitialization)
    {
        _preContextInitialization = preContextInitialization;
    }
}