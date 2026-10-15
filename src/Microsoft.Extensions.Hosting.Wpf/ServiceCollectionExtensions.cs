using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Internal;
using Microsoft.Extensions.Hosting.Wpf.Locator;

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
        ThrowHelper.ThrowIfNull(services);
        EnsureSingleWpfApplication<TApplication>(services);

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
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(createApplication);
        EnsureSingleWpfApplication<TApplication>(services);

        //Only single TApplication should exist.
        services.TryAddSingleton<Func<TApplication>>(provider =>
        {
            //Rare case when someone needs to resolve TApplication implementation manually, or maybe not from the IServiceProvider but another container.
            return () => createApplication(provider);
        });

        return services.AddWpfCommonRegistrations<TApplication>();
    }

    /// <summary>
    /// Uses <see cref="WpfLifetime"/> as the host lifetime: the host stops when the WPF application exits.
    /// This unblocks extensions like RunAsync and WaitForShutdownAsync.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpfLifetime(this IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        services.AddSingleton<IHostLifetime, WpfLifetime>();

        return services;
    }

    /// <summary>
    /// Uses <see cref="WpfLifetime"/> as the host lifetime: the host stops when the WPF application exits.
    /// This unblocks extensions like RunAsync and WaitForShutdownAsync.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configureOptions">The delegate for configuring the <see cref="WpfLifetime"/>.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpfLifetime(this IServiceCollection services, Action<WpfLifetimeOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(configureOptions);

        services.AddWpfLifetime();
        services.Configure(configureOptions);

        return services;
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> on the WPF UI thread after InitializeComponent and before Run.
    /// Requires <see cref="AddWpf{TApplication}(IServiceCollection)"/> for the same <typeparamref name="TApplication"/>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpfInitialization<TApplication>(this IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent, IApplicationInitialize
    {
        ThrowHelper.ThrowIfNull(services);

        return services.AddWpfPreContextInitialization<TApplication>((_, context) => context.WpfApplication?.Initialize());
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> and <see cref="IViewModelLocatorInitialization{TViewModelLocator}.InitializeLocator"/> on the WPF UI thread before Run.
    /// Requires <see cref="AddWpf{TApplication}(IServiceCollection)"/> for the same <typeparamref name="TApplication"/>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="viewModelLocator">Instance of <typeparamref name="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpfViewModelLocator<TApplication, TViewModelLocator>(this IServiceCollection services, TViewModelLocator viewModelLocator)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(viewModelLocator);

        return services.AddWpfViewModelLocator<TApplication, TViewModelLocator>(_ => viewModelLocator);
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> and <see cref="IViewModelLocatorInitialization{TViewModelLocator}.InitializeLocator"/> on the WPF UI thread before Run.
    /// The locator factory is invoked on that thread with the built host's service provider.
    /// Requires <see cref="AddWpf{TApplication}(IServiceCollection)"/> for the same <typeparamref name="TApplication"/>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="viewModelLocatorFunc">Function for creating <typeparamref name="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWpfViewModelLocator<TApplication, TViewModelLocator>(this IServiceCollection services, Func<IServiceProvider, TViewModelLocator> viewModelLocatorFunc)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(viewModelLocatorFunc);

        return services.AddWpfPreContextInitialization<TApplication>((provider, context) =>
        {
            context.WpfApplication?.Initialize();
            var viewModelLocator = viewModelLocatorFunc(provider);
            context.WpfApplication?.InitializeLocator(viewModelLocator);
        });
    }

    private static IServiceCollection AddWpfPreContextInitialization<TApplication>(this IServiceCollection services, Action<IServiceProvider, IWpfContext<TApplication>> initialize)
        where TApplication : Application, IApplicationInitializeComponent
    {
        EnsureInitializationMatchesWpfApplication<TApplication>(services);

        foreach (var service in services)
        {
            if (service.ServiceType == typeof(WpfInitialization<TApplication>))
            {
                throw new InvalidOperationException(WpfInitialization<TApplication>.AlreadyConfiguredMessage);
            }
        }

        services.AddSingleton(new WpfInitialization<TApplication>(initialize)); //for internal usage only
        services.AddHostedService<WpfInitializationValidator<TApplication>>();

        return services;
    }

    private static void EnsureSingleWpfApplication<TApplication>(IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent
    {
        // WPF allows only one Application per AppDomain, and IWpfContext / IWpfThread can only point to one of them.
        // Calling AddWpf again for the same TApplication is fine (registrations use TryAdd).
        foreach (var service in services)
        {
            if (service.ServiceType == typeof(IWpfContext) && service.ImplementationInstance is IWpfContext existing && existing is not WpfContext<TApplication>)
            {
                var existingApplication = existing.GetType().GetGenericArguments()[0].Name;
                throw new InvalidOperationException($"AddWpf<{existingApplication}>() was already called. Only one WPF application can be hosted per process, so AddWpf<{typeof(TApplication).Name}>() is not allowed.");
            }

            // Initialization registered first for another application (AddWpfInitialization<B>() then AddWpf<A>()).
            if (InitializationApplicationType(service) is { } initializationApplication && initializationApplication != typeof(TApplication))
            {
                throw new InvalidOperationException(MismatchedInitializationMessage(initializationApplication, typeof(TApplication)));
            }
        }
    }

    private static void EnsureInitializationMatchesWpfApplication<TApplication>(IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent
    {
        // AddWpf<A>() then AddWpfInitialization<B>(): reject now, before the host starts A.
        foreach (var service in services)
        {
            if (service.ServiceType == typeof(IWpfContext) && service.ImplementationInstance is IWpfContext existing && existing is not WpfContext<TApplication>)
            {
                throw new InvalidOperationException(MismatchedInitializationMessage(typeof(TApplication), existing.GetType().GetGenericArguments()[0]));
            }
        }
    }

    private static Type? InitializationApplicationType(ServiceDescriptor service) =>
        service.ServiceType.IsGenericType && service.ServiceType.GetGenericTypeDefinition() == typeof(WpfInitialization<>)
            ? service.ServiceType.GetGenericArguments()[0]
            : null;

    private static string MismatchedInitializationMessage(Type initializationApplication, Type wpfApplication) =>
        $"WPF initialization is configured for {initializationApplication.Name}, but the WPF application is {wpfApplication.Name} (AddWpf<{wpfApplication.Name}>()). Use the same application type.";

    private static IServiceCollection AddWpfCommonRegistrations<TApplication>(this IServiceCollection services)
        where TApplication : Application, IApplicationInitializeComponent
    {
        //Register WpfContext
        var wpfContext = new WpfContext<TApplication>();
        services.TryAddSingleton(wpfContext); //for internal usage only
        services.TryAddSingleton<IWpfContext<TApplication>>(wpfContext);
        services.TryAddSingleton<IWpfContext>(wpfContext);

        //Register WpfThread, for internal usage only. Applies initialization registered via AddWpfInitialization / AddWpfViewModelLocator.
        services.TryAddSingleton<WpfThread<TApplication>>(s =>
        {
            var thread = new WpfThread<TApplication>(s, wpfContext);
            var initialization = s.GetService<WpfInitialization<TApplication>>();
            if (initialization is not null)
            {
                thread.SetPreContextInitialization(context => initialization.Initialize(s, context));
            }

            return thread;
        });
        services.TryAddSingleton<IWpfThread<TApplication>>(s => s.GetRequiredService<WpfThread<TApplication>>());
        services.TryAddSingleton<IWpfThread>(s => s.GetRequiredService<WpfThread<TApplication>>());

        //Register Wpf IHostedService
        services.AddHostedService<WpfHostedService<TApplication>>();

        return services;
    }
}
