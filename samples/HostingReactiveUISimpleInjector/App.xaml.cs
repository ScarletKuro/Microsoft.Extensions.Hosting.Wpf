using System.Reactive.Concurrency;
using System.Windows;
using HostingReactiveUISimpleInjector.Locator;
using Microsoft.Extensions.Hosting.Wpf;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace HostingReactiveUISimpleInjector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IViewModelLocatorInitialization<ViewModelLocator>, IApplicationInitializeComponent
    {
        private readonly ILogger<App> _logger;

        private App() : this(null!)
        {
            //!!It's important to have an parametless constructor because app.g.cs is auto-generating own Main that requires a parametless constructor.
            //We cannot disable this behaviour but we can trick it.
            //Its also important to have  the constructor PRIVATE if you are injecting something in App like below i.e. having multiple constructors.
            //Otherwise the activator will use parametless constructor to create the instance and not the one with services.

            //A diagnostic method that will check for invalid constructor configuration
            //It will throw and error in case if you have multiple constructor and you don't have a private parametless constructor
            this.CheckForInvalidConstructorConfiguration();
        }

        //Example that we can even inject logging here
        public App(ILogger<App> logger)
        {
            this.CheckForInvalidConstructorConfiguration();
            _logger = logger;
        }

        public void Initialize()
        {
            //Here we can initialize important things. This method always runs on UI thread. 
            //If you don't need to initialize anything, just set the ViewModelLocator

            //Initialize Reactive Splat to use the correct schedulers.
            Splat.Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.Wpf);

            //Set correct scheduler, even though it should be correct since we resolve in correct thread(line below), but just in case let it be reassign. 
            RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => new DispatcherScheduler(Dispatcher));
        }

        public void InitializeLocator(ViewModelLocator viewModelLocator)
        {
            //This is not really correct(in terms of SimpleInjector philosophy, but correct for ReactiveUI), as we doing Verify after the first resolve
            //Verify was earlier in the end of RootBoot, but turns out this will kill ReactiveCommand(>14.1.1 version) scheduling
            //because Verify would resolve the ViewModels once(for check) on non UI thread making Splat to initialize on wrong scheduler(NB! above code line fixes it).
            //As workaround we performing verify on UI thread.
            //NB! Even though the first lines fixes the problem and we can move Verify to normal place and some changes in ReactiveCommand are rolled back now, I don't want to risk it since sometimes ReactiveUI does some breaking changes.
            viewModelLocator.Container.Verify(); //Calls SimpleInjector verify https://docs.simpleinjector.org/en/latest/howto.html#verify-configuration

            //We need to set it so that our <locator:ViewModelLocatorHost x:Key="Locator"/> could resolve ViewModels for DataContext
            //You can also use it as service locator pattern, but I personally recommend you to use it only inside View xaml to bind the DataContext
            var viewModelLocatorHost = ViewModelLocatorHost.GetInstance(this);
            viewModelLocatorHost.SetViewModelLocator(viewModelLocator);
        }
    }
}
