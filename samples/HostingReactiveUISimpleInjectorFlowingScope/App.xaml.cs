using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HostingReactiveUISimpleInjectorFlowingScope.Context;
using HostingReactiveUISimpleInjectorFlowingScope.Service;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.Threading;
using ReactiveUI;
using SimpleInjector;

namespace HostingReactiveUISimpleInjectorFlowingScope
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IViewModelLocatorInitialization<Container>, IApplicationInitializeComponent
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

        public void Initialize(Container container)
        {
            //Here we can initialize important things. This method always runs on UI thread. 

            //Initialize Reactive Splat to use the correct schedulers.
            Splat.Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.Wpf);

            //Set correct scheduler, even though it should be correct since we resolve in correct thread(line below), but just in case let it be reassign. 
            RxApp.MainThreadScheduler = new WaitForDispatcherScheduler(() => new DispatcherScheduler(Dispatcher));

            //This is not really correct(in terms of SimpleInjector philosophy, but correct for ReactiveUI), as we doing Verify after the first resolve
            //Verify was earlier in the end of RootBoot, but turns out this will kill ReactiveCommand(>14.1.1 version) scheduling
            //because Verify would resolve the ViewModels once(for check) on non UI thread making Splat to initialize on wrong scheduler(NB! above code line fixes it).
            //As workaround we performing verify on UI thread.
            //NB! Even though the first lines fixes the problem and we can move Verify to normal place and some changes in ReactiveCommand are rolled back now, I don't want to risk it since sometimes ReactiveUI does some breaking changes.
            container.Verify(); //Calls SimpleInjector verify https://docs.simpleinjector.org/en/latest/howto.html#verify-configuration

            //Simulating a "request".
            _ = new Timer(state=> SimulateRequest(state,container), null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(-1));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "Suppressing it here since im using async body in parallel for testing purpose")]
        private void SimulateRequest(object? state, Container container)
        {
            //Simulates multiple scopes at same time.
            Parallel.For(1, 5, async number =>
            {
                await using Scope scope = new Scope(container);
                var joinableTaskFactory = scope.GetInstance<JoinableTaskFactory>();
                var context = scope.GetInstance<GuidContext>();
                var windowService = scope.GetInstance<WindowService>();
                context.SetId(Guid.NewGuid()); //set unique id for the virtual request
                await joinableTaskFactory.SwitchToMainThreadAsync();
                windowService.OpenMainWindow();
            });
        }
    }
}
