using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Locator;

namespace Microsoft.Extensions.Hosting.Wpf
{
    public static class WpfHostingExtensions
    {
        /// <summary>
        /// Listens for Application.Current.Exit to start the shutdown process.
        /// This will unblock extensions like RunAsync and WaitForShutdownAsync.
        /// </summary>
        /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
        /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /// <param name="configureOptions">The delegate for configuring the <see cref="WpfLifetime{TApplication}"/>.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder UseWpfLifetime<TApplication>(this IHostBuilder hostBuilder, Action<WpfLifeTimeOptions> configureOptions)
            where TApplication : Application, new()
        {
            return hostBuilder.ConfigureServices((_, collection) =>
            {
                collection.AddSingleton<IHostLifetime, WpfLifetime<TApplication>>();
                collection.Configure(configureOptions);
            });
        }

        /// <summary>
        /// Listens for Application.Current.Exit to start the shutdown process.
        /// This will unblock extensions like RunAsync and WaitForShutdownAsync.
        /// </summary>
        /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
        /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHostBuilder UseWpfLifetime<TApplication>(this IHostBuilder hostBuilder) where TApplication : Application, new() =>
            hostBuilder.ConfigureServices((_, collection) => collection.AddSingleton<IHostLifetime, WpfLifetime<TApplication>>());


        /// <summary>
        /// Adds feature to use ViewModelLocator and calls <see cref="IViewModelLocatorInitialization{TViewModelLocator}"/>
        /// </summary>
        /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
        /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
        /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /// <param name="container">Implementation of <see cref="IViewModelContainer"/></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns
        public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost hostBuilder, IViewModelContainer container)
            where TApplication : Application, IViewModelLocatorInitialization<TViewModelLocator>, new()
            where TViewModelLocator : class
        {
            WpfThread<TApplication> wpfThread = hostBuilder.Services.GetRequiredService<WpfThread<TApplication>>();
            wpfThread.PreContextInitialization = context =>
            {
                TViewModelLocator viewModelLocator = container.GetService<TViewModelLocator>();
                context.WpfApplication?.Initialize(viewModelLocator);
            };

            return hostBuilder;
        }

        /// <summary>
        /// Adds feature to use ViewModelLocator and calls <see cref="IViewModelLocatorInitialization{TViewModelLocator}"/>
        /// </summary>
        /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
        /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
        /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /// <param name="viewModelLocator">Instance of <see cref="TViewModelLocator"/></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost hostBuilder, TViewModelLocator viewModelLocator)
            where TApplication : Application, IViewModelLocatorInitialization<TViewModelLocator>, new()
        {
            WpfThread<TApplication> wpfThread = hostBuilder.Services.GetRequiredService<WpfThread<TApplication>>();
            wpfThread.PreContextInitialization = context =>
            {
                context.WpfApplication?.Initialize(viewModelLocator);
            };

            return hostBuilder;
        }

        /// <summary>
        /// Adds feature to use ViewModelLocator and calls <see cref="IViewModelLocatorInitialization{TViewModelLocator}"/>
        /// </summary>
        /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
        /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
        /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
        /// <param name="viewModelLocatorFunc">Function for creating <see cref="TViewModelLocator"/></param>
        /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
        public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost hostBuilder, Func<IServiceProvider, TViewModelLocator> viewModelLocatorFunc)
            where TApplication : Application, IViewModelLocatorInitialization<TViewModelLocator>, new()
        {
            WpfThread<TApplication> wpfThread = hostBuilder.Services.GetRequiredService<WpfThread<TApplication>>();
            wpfThread.PreContextInitialization = context =>
            {
                var viewModelLocator = viewModelLocatorFunc(hostBuilder.Services);
                context.WpfApplication?.Initialize(viewModelLocator);
            };

            return hostBuilder;
        }
    }
}
