using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Wpf.Bootstrap;

public static class WpfHostingExtensions
{
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
        if (host is null)
        {
            throw new ArgumentNullException(nameof(host));
        }

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var bootstraps = host.Services.GetServices<IBootstrap<TContainer>>();
        foreach (var bootstrap in bootstraps)
        {
            bootstrap.Boot(container, assemblies);
        }

        return host;
    }
}