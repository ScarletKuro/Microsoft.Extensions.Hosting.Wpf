using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost
{
    public class WpfThread<TApplication>
        where TApplication : Application, IApplicationInitializeComponent, new()
    {
        private readonly IServiceProvider _serviceProvider;
        private SynchronizationContext? _synchronizationContext;

        /// <summary>
        /// Pre initialization that happens before <see cref="Application.Run()"/>. This action happens on UI thread.
        /// </summary>
        public Action<WpfContext<TApplication>>? PreContextInitialization { get; set; }

        public WpfContext<TApplication> WpfContext { get; }

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
            WpfContext = wpfContext;
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
        /// Start the DI service on the thread
        /// </summary>
        public void Start()
        {
            MainThread.Start();
        }

        /// <summary>
        /// Handle the application exit
        /// </summary>
        public void HandleApplicationExit()
        {
            if (!WpfContext.IsRunning)
            {
                return;
            }

            //We need this because otherwise if we have an active open window and call StopApplication, we will get an exception
            //This might happens if HandleApplicationExit was called manually, for example via tray
            WpfContext.WpfApplication.CloseAllWindowsIfAny();

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
            if (!WpfContext.IsLifetimeLinked)
            {
                // Register to the WPF application exit to stop the host application
                application.Exit += (_, _) =>
                {
                    HandleApplicationExit();
                };
            }

            // Store the application for others to interact
            WpfContext.SetWpfApplication(application);
            //Initialize all internal app properties
            application.InitializeComponent();
            PreContextInitialization?.Invoke(WpfContext);
        }

        /// <summary>
        /// Implement all the code which is needed to run the actual UI
        /// </summary>
        private void UiThreadStart()
        {
            // Mark the application as running
            WpfContext.IsRunning = true;
            //Since tray icon should be created in the STA thread we have to use lambda
            var trayIconFunction = _serviceProvider.GetService<Func<WpfThread<TApplication>, ITrayIcon<TApplication>>>();
            if (trayIconFunction is not null)
            {
                // Create icon if we used <see cref="WpfHostingExtensions.AddWpfTrayIcon{TTrayIcon, TApplication}"/>
                using var trayIcon = trayIconFunction(this);
                trayIcon.CreateNotifyIcon();
                WpfContext.WpfApplication?.Run();
            }
            else
            {
                WpfContext.WpfApplication?.Run();
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
    }
}
