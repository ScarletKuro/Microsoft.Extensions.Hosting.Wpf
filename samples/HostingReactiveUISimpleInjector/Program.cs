using HostingReactiveUISimpleInjector.Bootstrap;
using HostingReactiveUISimpleInjector.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Wpf;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;
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
            using var container = RootBoot.CreateContainer(); //Creating SimpleInjector container, we didn't register here anything yet
            using IHost host = CreateHostBuilder(container, args)
                .Build()
                .UseSimpleInjector(container)
                .UseWpfContainerBootstrap(container)
                .UseWpfViewModelLocator<App, ViewModelLocator>(new ViewModelLocator(container));
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
            services.AddWpf<App>();
            services.AddWpfTrayIcon<TrayIcon>();
        }
    }
}
