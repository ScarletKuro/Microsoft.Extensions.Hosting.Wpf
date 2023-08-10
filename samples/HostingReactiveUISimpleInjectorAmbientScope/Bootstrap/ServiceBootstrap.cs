using System.Diagnostics.Contracts;
using System.Reflection;
using HostingReactiveUISimpleInjectorAmbientScope.Context;
using HostingReactiveUISimpleInjectorAmbientScope.Service;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using SimpleInjector;

namespace HostingReactiveUISimpleInjectorAmbientScope.Bootstrap
{
    public class ServiceBootstrap : IBootstrap<Container>
    {
        public void Boot(Container container, Assembly[] assemblies)
        {
            Contract.Assert(container is not null, nameof(container));
            container.Register<WindowService>(Lifestyle.Transient); //can be scope too
            container.Register<GuidContext>(Lifestyle.Scoped);
        }
    }
}
