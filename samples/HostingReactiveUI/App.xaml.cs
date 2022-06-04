using System;
using System.Reactive.Concurrency;
using System.Windows;
using HostingReactiveUI.Locator;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;

namespace HostingReactiveUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IViewModelLocatorInitialization<ViewModelLocator>, IApplicationInitializeComponent
    {
        private readonly ILogger<App> _logger;

        public App() : this(NullLogger<App>.Instance)
        {
        }

        //Example that we can even inject logging here
        public App(ILogger<App> logger)
        {
            _logger = logger;
        }

        public void Initialize(ViewModelLocator viewModelLocator)
        {
            //Here we can initialize important things. This method always runs on UI thread. 
            //If you don't need to initialize anything, just set the ViewModelLocator

            //Initialize Reactive Splat to use the correct schedulers.
            Splat.Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.Wpf);

            //Set correct scheduler, even though it should be correct since we resolve in correct thread(line below), but just in case let it be reassign. 
            RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => new DispatcherScheduler(Dispatcher));

            //We need to set it so that our <locator:DefaultViewModelServiceProviderLocatorHost x:Key="Locator"/> could resolve ViewModels for DataContext
            //You can also use it as service locator pattern, but I personally recommend you to use it only inside View xaml to bind the DataContext
            var viewModelLocatorHost = ViewModelLocatorHost.GetInstance(this);
            viewModelLocatorHost?.SetViewModelLocator(viewModelLocator);
        }
    }
}
