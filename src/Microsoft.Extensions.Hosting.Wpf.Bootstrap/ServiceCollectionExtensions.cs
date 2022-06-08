using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Wpf.Bootstrap
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
    }
}
