using System.Reflection;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace HostingReactiveUISimpleInjectorAmbientScope.Bootstrap
{
    public class RootBoot : IBootstrap<Container>
    {
        private readonly ILogger<RootBoot> _logger;

        public RootBoot(ILogger<RootBoot> logger)
        {
            _logger = logger;
        }

        public void Boot(Container container, Assembly[] assemblies)
        {
            //Application registrations going here
            //I usually split it in Config, Mapping, Mediator, Provider(Bootstrap) etc
            IBootstrap<Container>[] bootstraps =
            {
                new ViewModelBootstrap(),
                new ServiceBootstrap(),
            };
            _logger.LogInformation("SimpleInjector registration.");
            foreach (var bootstrap in bootstraps)
            {
                _logger.LogInformation($"Booting {bootstrap.GetType().Name}.");
                bootstrap.Boot(container, assemblies);
            }
        }

        public static Container CreateContainer()
        {
            return new Container
            {
                Options =
                {
                    EnableAutoVerification = false,
                    DefaultScopedLifestyle = new AsyncScopedLifestyle()
                }
            };
        }
    }
}
