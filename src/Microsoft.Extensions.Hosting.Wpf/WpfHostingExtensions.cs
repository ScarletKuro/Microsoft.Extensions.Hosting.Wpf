using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Internal;
using Microsoft.Extensions.Hosting.Wpf.Locator;

namespace Microsoft.Extensions.Hosting.Wpf;

public static partial class WpfHostingExtensions
{
    /// <summary>
    /// Listens for Application.Current.Exit to start the shutdown process.
    /// This will unblock extensions like RunAsync and WaitForShutdownAsync.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
    /// <param name="configureOptions">The delegate for configuring the <see cref="WpfLifetime"/>.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseWpfLifetime(this IHostBuilder hostBuilder, Action<WpfLifeTimeOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);
        ThrowHelper.ThrowIfNull(configureOptions);

        return hostBuilder.ConfigureServices((_, collection) =>
        {
            collection.AddSingleton<IHostLifetime, WpfLifetime>();
            collection.Configure(configureOptions);
        });
    }

    /// <summary>
    /// Listens for Application.Current.Exit to start the shutdown process.
    /// This will unblock extensions like RunAsync and WaitForShutdownAsync.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IHostBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseWpfLifetime(this IHostBuilder hostBuilder)
    {
        ThrowHelper.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, collection) => collection.AddSingleton<IHostLifetime, WpfLifetime>());
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
    /// Adds feature to use ViewModelLocator and calls <see cref="IViewModelLocatorInitialization{TViewModelLocator}"/>
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <param name="host">The <see cref="IHostBuilder" /> to configure.</param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHost UseWpfInitialization<TApplication>(this IHost host)
        where TApplication : Application, IApplicationInitializeComponent, IApplicationInitialize
    {
        ThrowHelper.ThrowIfNull(host);

        WpfThread<TApplication> wpfThread = host.Services.GetRequiredService<WpfThread<TApplication>>();
        wpfThread.SetPreContextInitialization(context =>
        {
            context.WpfApplication?.Initialize();
        });

        return host;
    }

    /// <summary>
    /// Adds feature to use ViewModelLocator and calls <see cref="IViewModelLocatorInitialization{TViewModelLocator}"/>
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="host">The <see cref="IHostBuilder" /> to configure.</param>
    /// <param name="viewModelLocator">Instance of <see cref="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost host, TViewModelLocator viewModelLocator)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(viewModelLocator);

        WpfThread<TApplication> wpfThread = host.Services.GetRequiredService<WpfThread<TApplication>>();
        wpfThread.SetPreContextInitialization(context=>
        {
            context.WpfApplication?.Initialize();
            context.WpfApplication?.InitializeLocator(viewModelLocator);
        });

        return host;
    }

    /// <summary>
    /// Adds feature to use ViewModelLocator and calls <see cref="IViewModelLocatorInitialization{TViewModelLocator}"/>
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <typeparam name="TViewModelLocator">The View Model Locator</typeparam>
    /// <param name="host">The <see cref="IHostBuilder" /> to configure.</param>
    /// <param name="viewModelLocatorFunc">Function for creating <see cref="TViewModelLocator"/></param>
    /// <returns>The same instance of the <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHost UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHost host, Func<IServiceProvider, TViewModelLocator> viewModelLocatorFunc)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(viewModelLocatorFunc);

        WpfThread<TApplication> wpfThread = host.Services.GetRequiredService<WpfThread<TApplication>>();
        wpfThread.SetPreContextInitialization(context =>
        {
            context.WpfApplication?.Initialize();
            var viewModelLocator = viewModelLocatorFunc(host.Services);
            context.WpfApplication?.InitializeLocator(viewModelLocator);
        });

        return host;
    }
}
