using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.Extensions.Hosting.Wpf.Threading;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Thread Switching functionality for WPF application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <typeparam name="TApplication">WPF <see cref="Application" /></typeparam>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    /// <see cref="AddThreadSwitching(IServiceCollection)"/>
    /// <note>Should we mark this method as obsolete or keep it? There is no real scenario where we need <see cref="TApplication"/>.</note>
    public static IServiceCollection AddThreadSwitching<TApplication>(this IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent, new()
    {
        services.AddSingleton<JoinableTaskContext>(provider =>
        {
            var wpfThread = provider.GetRequiredService<IWpfThread<TApplication>>();

            return new JoinableTaskContext(wpfThread.MainThread, wpfThread.SynchronizationContext);
        });
        services.AddSingleton<JoinableTaskFactory>();

        return services;
    }

    /// <summary>
    /// Adds Thread Switching functionality for WPF application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddThreadSwitching(this IServiceCollection services)
    {
        services.AddSingleton<JoinableTaskContext>(provider =>
        {
            var wpfThread = provider.GetRequiredService<IWpfThread>();

            return new JoinableTaskContext(wpfThread.MainThread, wpfThread.SynchronizationContext);
        });
        services.AddSingleton<JoinableTaskFactory>();
        return services;
    }
}