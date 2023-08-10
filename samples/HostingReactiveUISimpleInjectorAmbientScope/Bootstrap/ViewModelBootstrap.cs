using System.Diagnostics.Contracts;
using System.Reflection;
using HostingReactiveUISimpleInjectorAmbientScope.Extensions;
using HostingReactiveUISimpleInjectorAmbientScope.Locator;
using HostingReactiveUISimpleInjectorAmbientScope.ViewModel;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using SimpleInjector;

namespace HostingReactiveUISimpleInjectorAmbientScope.Bootstrap
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

            //If ViewModel has IDisposable we need to use RegisterScopeDisposableTransient extension that will automatically dispose once the scope is ended (using AsyncScopedLifestyle.BeginScope)
            container.RegisterScopeDisposableTransient<MainViewModel>();
            //If we don't have IDisposable we can just register normally
            container.Register<ChildViewModel>(Lifestyle.Scoped);
        }
    }
}
