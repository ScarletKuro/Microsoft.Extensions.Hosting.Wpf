using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost
{
    public class WpfThread<TApplication>
        where TApplication : Application, new()
    {
        /// <summary>
        /// Pre initialization that happens before <see cref="Application.Run()"/>. This action happens on UI thread.
        /// </summary>
        public Action<WpfContext<TApplication>>? PreContextInitialization { get; set; }

        public WpfContext<TApplication> WpfContext { get; }

        public Thread MainThread { get; }

        public SynchronizationContext? SynchronizationContext { get; private set; }

        /// <summary>
        /// The IServiceProvider used by all IUiContext implementations
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Constructor which is called from the IWinFormsContext
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider</param>
        /// <param name="wpfContext">WpfContext</param>
        public WpfThread(IServiceProvider serviceProvider, WpfContext<TApplication> wpfContext)
        {
            ServiceProvider = serviceProvider;
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

            var applicationLifeTime = ServiceProvider.GetService<IHostApplicationLifetime>();
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
            SynchronizationContext = synchronizationContext;
            
            var application = CreateApplication();

            //Only observe event if we don't have WpfLifetime linked that already listens and calls StopApplication on demand
            if (!WpfContext.IsLifetimeLinked)
            {
                // Register to the WPF application exit to stop the host application
                application.Exit += (s, e) =>
                {
                    HandleApplicationExit();
                };
            }

            // Store the application for others to interact
            WpfContext.SetWpfApplication(application);
            if (application is IApplicationInitializeComponent app)
            {
                //Initialize all internal app properties
                app.InitializeComponent();
            }
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
            var trayIconFunction = ServiceProvider.GetService<Func<WpfThread<TApplication>, ITrayIcon<TApplication>>>();
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
            var applicationFunction = ServiceProvider.GetService<Func<IServiceProvider, TApplication>>();
            if (applicationFunction is not null)
            {
                return applicationFunction(ServiceProvider);
            }

            return new TApplication();
        }
    }
}
