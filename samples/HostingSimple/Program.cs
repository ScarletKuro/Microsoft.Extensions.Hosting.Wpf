using HostingSimple.Locator;
using HostingSimple.Service;
using HostingSimple.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Wpf;

namespace HostingSimple
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args)
                .Build()
                .UseWpfViewModelLocator<App, IViewModelLocator>(provider => new ViewModelLocator(provider));
            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(ConfigureServices);
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddLogging();
            services.AddWpf<App>();

            //Add our view models
            services.AddTransient<MainViewModel>();
            services.AddTransient<ChildViewModel>();

            //Add our custom services for wpf
            services.AddSingleton<WindowService>();
        }
    }
}
