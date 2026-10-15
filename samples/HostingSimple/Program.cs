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
            var builder = Host.CreateApplicationBuilder(args);
            ConfigureServices(builder.Services);
            builder.Services.AddWpfLifetime();
            builder.Services.AddWpfViewModelLocator<App, IViewModelLocator>(provider => new ViewModelLocator(provider));

            using IHost host = builder.Build();
            host.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
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
