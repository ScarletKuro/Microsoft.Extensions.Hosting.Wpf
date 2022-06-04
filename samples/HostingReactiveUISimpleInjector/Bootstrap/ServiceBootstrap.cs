using System.Diagnostics.Contracts;
using System.Reflection;
using HostingReactiveUISimpleInjector.Service;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using SimpleInjector;

namespace HostingReactiveUISimpleInjector.Bootstrap
{
    public class ServiceBootstrap : IBootstrap<Container>
    {
        public void Boot(Container container, Assembly[] assemblies)
        {
            Contract.Assert(container is not null, nameof(container));
            container.Register<WindowService>(Lifestyle.Singleton);
        }
    }
}
