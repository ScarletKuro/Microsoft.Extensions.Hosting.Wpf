using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.Extensions.Hosting.Wpf.Threading
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Thread Switching functionality for WPF application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <typeparam name="TApplication">WPF <see cref="Application" /></typeparam>
        /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddThreadSwitching<TApplication>(this IServiceCollection services)
            where TApplication : Application, new()
        {
            services.AddSingleton<JoinableTaskContext>(provider =>
            {
                var wpfThread = provider.GetRequiredService<WpfThread<TApplication>>();

                return new JoinableTaskContext(wpfThread.MainThread, wpfThread.SynchronizationContext);
            });
            services.AddSingleton<JoinableTaskFactory>();

            return services;
        }
    }
}
