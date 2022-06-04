using HostingReactiveUI.Locator;
using HostingReactiveUI.Service;
using HostingReactiveUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Wpf;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace HostingReactiveUI
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            //LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(NLogFilePath);
            using IHost host = CreateHostBuilder(args)
                .Build()
                .UseWpfViewModelLocator<App, ViewModelLocator>(provider => new ViewModelLocator(provider));
            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices);
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
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
            services.AddWpf(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<App>>();

                return new App(logger);
            });
            services.AddWpfTrayIcon<TrayIcon, App>(wpfThread => new TrayIcon(wpfThread));

            //Add our view models
            services.AddTransient<MainViewModel>();
            services.AddTransient<ChildViewModel>();

            //Add our custom services for wpf
            services.AddSingleton<WindowService>();
        }
    }
}
