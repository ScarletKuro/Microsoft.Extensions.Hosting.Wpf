using System.Diagnostics.Contracts;
using System.Reflection;
using HostingReactiveUISimpleInjectorFlowingScope.Locator;
using HostingReactiveUISimpleInjectorFlowingScope.ViewModel;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using SimpleInjector;

namespace HostingReactiveUISimpleInjectorFlowingScope.Bootstrap
{
    public class ViewModelBootstrap : IBootstrap<Container>
    {
        public void Boot(Container container, Assembly[] assemblies)
        {
            Contract.Assert(container is not null, nameof(container));
            //If we implement IViewModelContainer and use it
            //We register both for example purpose.
            //Since we use SimpleInjector Verify we actually need ViewModelLocator that exposes our Container details
            container.Register<IViewModelLocator, ViewModelLocator>(Lifestyle.Scoped);
            container.Register<ViewModelLocator>(Lifestyle.Scoped);

            container.Register<MainViewModel>(Lifestyle.Transient);
            container.Register<ChildViewModel>(Lifestyle.Transient);
        }
    }
}
