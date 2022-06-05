using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <see cref="IBootstrap{TContainer}"/> to <see cref="IServiceCollection"/> for <see cref="WpfHostingExtensions.UseWpfContainerBootstrap{TContainer}"/>.
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
        /// <param name="createTray">The function used to create <see cref="ITrayIcon{TApplication}" /></param>
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

        /// <summary>
        /// Adds WPF functionality for GenericHost with existing <see cref="Application" />.
        /// </summary>
        /// <param name="createApplication">The function used to create <see cref="Application" /></param>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <typeparam name="TApplication">WPF <see cref="Application" /></typeparam>
        /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddWpf<TApplication>(this IServiceCollection services, TApplication createApplication)
            where TApplication : Application, new()
        {
            services.AddSingleton<Func<IServiceProvider, TApplication>>(_ => createApplication);

            return AddWpf<TApplication>(services);
        }
    }
}
