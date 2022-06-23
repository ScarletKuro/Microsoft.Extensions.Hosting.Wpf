using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.TrayIcon;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds tray icon functionality for WPF application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <typeparam name="TTrayIcon">Implementation of <see cref="ITrayIcon" />.</typeparam>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpfTrayIcon<TTrayIcon>(this IServiceCollection services)
        where TTrayIcon : class, ITrayIcon
    {
        //Currently we can't use directly services.AddSingleton<IWpfComponent, TTrayIcon>() / services.AddTransient<IWpfComponent, TTrayIcon>()
        //Since no matter what lifetime you use, if you let the container to create the type by itself without Func then if IDisposable is implemented it will be auto disposed once the container is disposed https://github.com/dotnet/runtime/issues/36491
        //We do not want this, since:
        //1)It happens on non UI thread
        //2)We want to MANUALLY dispose our IWpfComponent's(if it implements IDisposable)
        //Once this is fixed https://github.com/dotnet/runtime/issues/36461 we could use transient.
        services.AddSingleton<Func<IWpfComponent>>(provider =>
        {
            //Gladly this exist to not make a lot of overloads / expose IServiceProvider :)
            return () => ActivatorUtilities.CreateInstance<TTrayIcon>(provider);
        });

        return services;
    }
}