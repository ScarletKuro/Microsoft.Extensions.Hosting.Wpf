using System.Diagnostics.Contracts;
using System.Reflection;
using HostingReactiveUISimpleInjector.Locator;
using HostingReactiveUISimpleInjector.ViewModel;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using SimpleInjector;

namespace HostingReactiveUISimpleInjector.Bootstrap
{
    public class ViewModelBootstrap : IBootstrap<Container>
    {
        public void Boot(Container container, Assembly[] assemblies)
        {
            Contract.Assert(container is not null, nameof(container));
            var viewModelLocator = new ViewModelLocator(container);
            //If we implement IViewModelContainer and use it, in our case SimpleInjectorViewModelContainer we need to register ViewModelLocator
            //We register both for example purpose.
            //User can decide whatever he wants to use .UseWpfViewModelLocator<App, ViewModelLocator> / UseWpfViewModelLocator<App, IViewModelLocator> with IViewModelLocatorInitialization<ViewModelLocator> / IViewModelLocatorInitialization<IViewModelLocator>
            //Since we use SimpleInjector Verify we actually need ViewModelLocator that exposes our Container details
            container.Register<IViewModelLocator>(() => viewModelLocator, Lifestyle.Singleton);
            container.Register<ViewModelLocator>(() => viewModelLocator, Lifestyle.Singleton);

            container.Register<MainViewModel>(Lifestyle.Transient);
            container.Register<ChildViewModel>(Lifestyle.Transient);
        }
    }
}
