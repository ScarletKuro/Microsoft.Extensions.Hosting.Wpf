using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Internal;
using Microsoft.Extensions.Hosting.Wpf.Locator;

namespace Microsoft.Extensions.Hosting.Wpf;

public static class WpfHostingExtensions
{
    /// <summary>
    /// Uses <see cref="WpfLifetime"/> as the host lifetime instead of the default ConsoleLifetime. Optional: with any lifetime,
    /// AddWpf already stops the host when the WPF application exits. WpfLifetime suits a windowed application: it does not
    /// handle Ctrl+C or print console shutdown hints, logs WPF status messages (see <see cref="WpfLifetimeOptions"/>),
    /// and waits for the host to shut down gracefully when the process exits.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
    /// <param name="configureOptions">The delegate for configuring the <see cref="WpfLifetime"/>.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseWpfLifetime(this IHostBuilder hostBuilder, Action<WpfLifetimeOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);
        ThrowHelper.ThrowIfNull(configureOptions);

        return hostBuilder.ConfigureServices((_, collection) => collection.AddWpfLifetime(configureOptions));
    }

    /// <summary>
    /// Uses <see cref="WpfLifetime"/> as the host lifetime instead of the default ConsoleLifetime. Optional: with any lifetime,
    /// AddWpf already stops the host when the WPF application exits. WpfLifetime suits a windowed application: it does not
    /// handle Ctrl+C or print console shutdown hints, logs WPF status messages (see <see cref="WpfLifetimeOptions"/>),
    /// and waits for the host to shut down gracefully when the process exits.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseWpfLifetime(this IHostBuilder hostBuilder)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, collection) => collection.AddWpfLifetime());
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> on the WPF UI thread after InitializeComponent and before Run.
    /// Requires <c>services.AddWpf&lt;TApplication&gt;()</c>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseWpfInitialization<TApplication>(this IHostBuilder hostBuilder)
        where TApplication : Application, IApplicationInitializeComponent, IApplicationInitialize
    {
        ThrowHelper.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, collection) => collection.AddWpfInitialization<TApplication>());
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> and <see cref="IViewModelLocatorInitialization{TViewModelLocator}.InitializeLocator"/> on the WPF UI thread before Run.
    /// Requires <c>services.AddWpf&lt;TApplication&gt;()</c>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
    /// <param name="viewModelLocator">Instance of <typeparamref name="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHostBuilder hostBuilder, TViewModelLocator viewModelLocator)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(hostBuilder);
        ThrowHelper.ThrowIfNull(viewModelLocator);

        return hostBuilder.ConfigureServices((_, collection) => collection.AddWpfViewModelLocator<TApplication, TViewModelLocator>(viewModelLocator));
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> and <see cref="IViewModelLocatorInitialization{TViewModelLocator}.InitializeLocator"/> on the WPF UI thread before Run.
    /// The locator factory is invoked on that thread with the built host's service provider.
    /// Requires <c>services.AddWpf&lt;TApplication&gt;()</c>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
    /// <param name="viewModelLocatorFunc">Function for creating <typeparamref name="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHostBuilder hostBuilder, Func<IServiceProvider, TViewModelLocator> viewModelLocatorFunc)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(hostBuilder);
        ThrowHelper.ThrowIfNull(viewModelLocatorFunc);

        return hostBuilder.ConfigureServices((_, collection) => collection.AddWpfViewModelLocator<TApplication, TViewModelLocator>(viewModelLocatorFunc));
    }


    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> on the WPF UI thread after InitializeComponent and before Run.
    /// Post-build alternative to <c>services.AddWpfInitialization&lt;TApplication&gt;()</c>, for setups that must configure it
    /// after <c>Build()</c> (e.g. bootstrapping a third-party container from the built host). Call it before the host starts.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <param name="host">The built <see cref="IHost" />.</param>
    /// <returns>The same instance of the <see cref="IHost"/> for chaining.</returns>
    public static IHost UseWpfInitialization<TApplication>(this IHost host)
        where TApplication : Application, IApplicationInitializeComponent, IApplicationInitialize
    {
        ThrowHelper.ThrowIfNull(host);

        return host.UsePreContextInitialization(WpfInitializationActions.Initialize<TApplication>());
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> and <see cref="IViewModelLocatorInitialization{TViewModelLocator}.InitializeLocator"/>
    /// with <paramref name="viewModelLocator"/> on the WPF UI thread before Run. Post-build alternative to
    /// <c>services.AddWpfViewModelLocator</c>. Call it before the host starts.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="host">The built <see cref="IHost" />.</param>
    /// <param name="viewModelLocator">Instance of <typeparamref name="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IHost"/> for chaining.</returns>
    public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost host, TViewModelLocator viewModelLocator)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(viewModelLocator);

        return host.UsePreContextInitialization(WpfInitializationActions.ViewModelLocator<TApplication, TViewModelLocator>(_ => viewModelLocator));
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/> and <see cref="IViewModelLocatorInitialization{TViewModelLocator}.InitializeLocator"/>
    /// on the WPF UI thread before Run; the locator is created there by <paramref name="viewModelLocatorFunc"/> with the host's
    /// service provider. Post-build alternative to <c>services.AddWpfViewModelLocator</c>. Call it before the host starts.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="host">The built <see cref="IHost" />.</param>
    /// <param name="viewModelLocatorFunc">Function for creating <typeparamref name="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IHost"/> for chaining.</returns>
    public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost host, Func<IServiceProvider, TViewModelLocator> viewModelLocatorFunc)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(viewModelLocatorFunc);

        return host.UsePreContextInitialization(WpfInitializationActions.ViewModelLocator<TApplication, TViewModelLocator>(viewModelLocatorFunc));
    }

    private static IHost UsePreContextInitialization<TApplication>(this IHost host, Action<IServiceProvider, IWpfContext<TApplication>> initialize)
        where TApplication : Application, IApplicationInitializeComponent
    {
        var wpfThread = host.Services.GetRequiredService<WpfThread<TApplication>>();
        wpfThread.SetPreContextInitialization(context => initialize(host.Services, context));

        return host;
    }
}
