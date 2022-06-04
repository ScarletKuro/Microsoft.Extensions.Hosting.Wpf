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
            //We register both implementation and interface, implementation for quirky cases and interface when we don't care about details
            container.Register<IViewModelLocator>(() => viewModelLocator, Lifestyle.Singleton);
            container.Register<ViewModelLocator>(() => viewModelLocator, Lifestyle.Singleton);

            container.Register<MainViewModel>(Lifestyle.Transient);
            container.Register<ChildViewModel>(Lifestyle.Transient);
        }
    }
}
