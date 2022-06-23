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
        //Only single TApplication should exist;
        services.TryAddSingleton<Func<TApplication>>(provider =>
        {
            return () => ActivatorUtilities.CreateInstance<TApplication>(provider);
        });

        //Register WpfContext
        var wpfContext = new WpfContext<TApplication>();
        services.AddSingleton(wpfContext); //for internal usage only
        services.AddSingleton<IWpfContext<TApplication>>(wpfContext);
        services.AddSingleton<IWpfContext>(wpfContext);

        //Register WpfThread
        services.AddSingleton<WpfThread<TApplication>>();  //for internal usage only
        services.AddSingleton<IWpfThread<TApplication>>(s => s.GetRequiredService<WpfThread<TApplication>>());
        services.AddSingleton<IWpfThread>(s => s.GetRequiredService<WpfThread<TApplication>>());

        //Register Wpf IHostedService
        services.AddHostedService<WpfHostedService<TApplication>>();

        return services;
    }
}