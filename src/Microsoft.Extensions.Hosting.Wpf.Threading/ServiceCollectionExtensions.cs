using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// <remarks>Same as <see cref="AddThreadSwitching(IServiceCollection)"/>, but uses the WPF thread of <typeparamref name="TApplication"/>.
    /// Since only one WPF application can be registered, both overloads resolve the same thread.</remarks>
    public static IServiceCollection AddThreadSwitching<TApplication>(this IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent
    {
        services.TryAddSingleton<JoinableTaskContext>(provider =>
        {
            var wpfThread = provider.GetRequiredService<IWpfThread<TApplication>>();

            //MainThread exists before the UI thread starts; the synchronization context is resolved when first used.
            return new JoinableTaskContext(wpfThread.MainThread, new WpfThreadSynchronizationContext(wpfThread));
        });
        services.TryAddSingleton<JoinableTaskFactory>();

        return services;
    }

    /// <summary>
    /// Adds Thread Switching functionality for WPF application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddThreadSwitching(this IServiceCollection services)
    {
        services.TryAddSingleton<JoinableTaskContext>(provider =>
        {
            var wpfThread = provider.GetRequiredService<IWpfThread>();

            //MainThread exists before the UI thread starts; the synchronization context is resolved when first used.
            return new JoinableTaskContext(wpfThread.MainThread, new WpfThreadSynchronizationContext(wpfThread));
        });
        services.TryAddSingleton<JoinableTaskFactory>();
        return services;
    }
}