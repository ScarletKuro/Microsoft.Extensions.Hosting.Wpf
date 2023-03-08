using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Internal;

namespace Microsoft.Extensions.Hosting.Wpf;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds WPF functionality for GenericHost with default implementation of <see cref="Application" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpf<TApplication>(this IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent
    {
        //Only single TApplication should exist.
        services.TryAddSingleton<Func<TApplication>>(provider =>
        {
            return () => ActivatorUtilities.CreateInstance<TApplication>(provider);
        });

        return services.AddWpfCommonRegistrations<TApplication>();
    }


    /// <summary>
    /// Adds WPF functionality for GenericHost with default implementation of <see cref="Application" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="createApplication">The function used to create <see cref="Application" /></param>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpf<TApplication>(this IServiceCollection services, Func<IServiceProvider, TApplication> createApplication)
        where TApplication : Application, IApplicationInitializeComponent
    {
        //Only single TApplication should exist.
        services.TryAddSingleton<Func<TApplication>>(provider =>
        {
            //Rare case when someone needs to resolve TApplication implementation manually, or maybe not from the IServiceProvider but another container.
            return () => createApplication(provider);
        });

        return services.AddWpfCommonRegistrations<TApplication>(); ;
    }

    private static IServiceCollection AddWpfCommonRegistrations<TApplication>(this IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent
    {
        //Register WpfContext
        var wpfContext = new WpfContext<TApplication>();
        services.TryAddSingleton(wpfContext); //for internal usage only
        services.TryAddSingleton<IWpfContext<TApplication>>(wpfContext);
        services.TryAddSingleton<IWpfContext>(wpfContext);

        //Register WpfThread
        services.TryAddSingleton<WpfThread<TApplication>>(s => new WpfThread<TApplication>(s, wpfContext));  //for internal usage only
        services.TryAddSingleton<IWpfThread<TApplication>>(s => s.GetRequiredService<WpfThread<TApplication>>());
        services.TryAddSingleton<IWpfThread>(s => s.GetRequiredService<WpfThread<TApplication>>());

        //Register Wpf IHostedService
        services.AddHostedService<WpfHostedService<TApplication>>();

        return services;
    }
}