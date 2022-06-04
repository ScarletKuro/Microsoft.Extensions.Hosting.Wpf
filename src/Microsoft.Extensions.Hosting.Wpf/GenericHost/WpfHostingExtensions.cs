using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost
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

        public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost host, IViewModelContainer container)
            where TApplication : Application, IViewModelLocatorInitialization<TViewModelLocator>, new()
            where TViewModelLocator : class
        {
            WpfThread<TApplication> wpfThread = host.Services.GetRequiredService<WpfThread<TApplication>>();
            wpfThread.PreContextInitialization = context =>
            {
                TViewModelLocator viewModelLocator = container.GetService<TViewModelLocator>();
                context.WpfApplication?.Initialize(viewModelLocator);
            };

            return host;
        }

        /// <summary>
        /// Bootstraps <see cref="IBootstrap{TContainer}"/> Dependency Injection container that is not Microsoft <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TContainer">Container type.</typeparam>
        /// <param name="host">The <see cref="IHost" /> to configure.</param>
        /// <param name="container">The Dependency Injection container</param>
        /// <returns>The same instance of the <see cref="IHost"/> for chaining.</returns>
        public static IHost UseWpfContainerBootstrap<TContainer>(this IHost host, TContainer container)
            where TContainer : class
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var bootstraps = host.Services.GetServices<IBootstrap<TContainer>>();
            foreach (var bootstrap in bootstraps)
            {
                bootstrap.Boot(container, assemblies);
            }

            return host;
        }

        /// <summary>
        /// Adds <see cref="IBootstrap{TContainer}"/> to <see cref="IServiceCollection"/> for <see cref="UseWpfContainerBootstrap{TContainer}"/>.
        /// </summary>
        /// <typeparam name="TContainer">The Dependency Injection container.</typeparam>
        /// <typeparam name="TBootstrap"><see cref="IBootstrap{TContainer}"/> bootstrap type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddBootstrap<TContainer, TBootstrap>(this IServiceCollection services) 
            where TContainer : class
            where TBootstrap : class, IBootstrap<TContainer>
        {
            services.AddSingleton<IBootstrap<TContainer>, TBootstrap>();

            return services;
        }

        /// <summary>
        /// Adds tray icon functionality for WPF application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="createTray">The function used to create <see cref="ITrayIcon{Application}" /></param>
        /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
        /// <typeparam name="TTrayIcon">Implementation of <see cref="ITrayIcon{Application}" />.</typeparam>
        /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddWpfTrayIcon<TTrayIcon, TApplication>(this IServiceCollection services, Func<WpfThread<TApplication>, TTrayIcon> createTray) 
            where TTrayIcon : class, ITrayIcon<TApplication>
            where TApplication : Application, new()
        {
            services.AddSingleton<Func<WpfThread<TApplication>, ITrayIcon<TApplication>>>(createTray);

            return services;
        }

        /// <summary>
        /// Adds WPF functionality for GenericHost with default implementation of <see cref="Application" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
        /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddWpf<TApplication>(this IServiceCollection services)
            where TApplication : Application, new()
        {
            services.AddSingleton(new WpfContext<TApplication>());
            services.AddSingleton<WpfThread<TApplication>>();
            services.AddHostedService<WpfHostedService<TApplication>>();

            return services;
        }

        /// <summary>
        /// Adds WPF functionality for GenericHost with existing <see cref="Application" />.
        /// </summary>
        /// <param name="createApplication">The function used to create <see cref="Application" /></param>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <typeparam name="TApplication">WPF <see cref="Application" /></typeparam>
        /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddWpf<TApplication>(this IServiceCollection services, Func<IServiceProvider, TApplication> createApplication)
            where TApplication : Application, new()
        {
            services.AddSingleton<Func<IServiceProvider, TApplication>>(createApplication);

            return AddWpf<TApplication>(services);
        }
    }
}
