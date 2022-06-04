using HostingReactiveUISimpleInjector.Bootstrap;
using HostingReactiveUISimpleInjector.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting.Wpf;
using NLog.Extensions.Logging;
using SimpleInjector;

namespace HostingReactiveUISimpleInjector
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            using var container = RootBoot.CreateContainer();
            //You can skip SimpleInjectorViewModelContainer, see comments in SimpleInjectorViewModelContainer class
            using var viewModelContainer = new SimpleInjectorViewModelContainer(container);
            using IHost host = CreateHostBuilder(container, args)
                .Build()
                .UseSimpleInjector(container)
                .UseWpfContainerBootstrap(container)
                //Or UseWpfViewModelLocator<App, ViewModelLocator>(new ViewModelLocator(container));
                .UseWpfViewModelLocator<App, ViewModelLocator>(viewModelContainer);
            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(Container container, string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, collection) => ConfigureServices(context, collection, container));
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services, Container container)
        {
            services.AddLogging(loggingBuilder =>
            {
                //We need to disable Microsoft LoggerFactory so that our LogLevel works correctly from NLog.config
                loggingBuilder.AddNLog(new NLogProviderOptions
                {
                    ReplaceLoggerFactory = true,
                    RemoveLoggerFactoryFilter = true,
                    ShutdownOnDispose = true
                });
            });
            services.AddSimpleInjector(container, options =>
            {
                options.AddLogging();
            });
            services.AddBootstrap<Container, RootBoot>();
            services.AddWpf(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<App>>();

                return new App(logger);
            });
            services.AddWpfTrayIcon<TrayIcon, App>(wpfThread => new TrayIcon(wpfThread));
        }
    }
}
