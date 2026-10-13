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
    /// Uses the WPF application lifetime to stop the host when the application exits.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHostApplicationBuilder UseWpfLifetime(this IHostApplicationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.AddSingleton<IHostLifetime, WpfLifetime>();
        return builder;
    }

    /// <summary>
    /// Uses the WPF application lifetime with the specified options.
    /// </summary>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="configureOptions">The delegate for configuring the WPF lifetime.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHostApplicationBuilder UseWpfLifetime(this IHostApplicationBuilder builder, Action<WpfLifeTimeOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configureOptions);

        builder.UseWpfLifetime();
        builder.Services.Configure(configureOptions);
        return builder;
    }

    /// <summary>
    /// Configures application initialization on the WPF UI thread after InitializeComponent and before Run.
    /// Call <c>builder.Services.AddWpf&lt;TApplication&gt;()</c> to register the application.
    /// </summary>
    /// <typeparam name="TApplication">The WPF application type.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHostApplicationBuilder UseWpfInitialization<TApplication>(this IHostApplicationBuilder builder)
        where TApplication : Application, IApplicationInitializeComponent, IApplicationInitialize
    {
        ThrowHelper.ThrowIfNull(builder);

        return AddWpfInitialization<TApplication>(builder, (_, context) => context.WpfApplication?.Initialize());
    }

    /// <summary>
    /// Configures application and view model locator initialization on the WPF UI thread before Run.
    /// Call <c>builder.Services.AddWpf&lt;TApplication&gt;()</c> to register the application.
    /// </summary>
    /// <typeparam name="TApplication">The WPF application type.</typeparam>
    /// <typeparam name="TViewModelLocator">The view model locator type.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="viewModelLocator">The view model locator instance.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHostApplicationBuilder UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHostApplicationBuilder builder, TViewModelLocator viewModelLocator)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(viewModelLocator);

        return builder.UseWpfViewModelLocator<TApplication, TViewModelLocator>(_ => viewModelLocator);
    }

    /// <summary>
    /// Configures application and view model locator initialization on the WPF UI thread before Run.
    /// The locator factory is invoked on that thread using the built host's service provider.
    /// Call <c>builder.Services.AddWpf&lt;TApplication&gt;()</c> to register the application.
    /// </summary>
    /// <typeparam name="TApplication">The WPF application type.</typeparam>
    /// <typeparam name="TViewModelLocator">The view model locator type.</typeparam>
    /// <param name="builder">The builder to configure.</param>
    /// <param name="viewModelLocatorFunc">The factory used to create the view model locator.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHostApplicationBuilder UseWpfViewModelLocator<TApplication, TViewModelLocator>(this IHostApplicationBuilder builder, Func<IServiceProvider, TViewModelLocator> viewModelLocatorFunc)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(viewModelLocatorFunc);

        return AddWpfInitialization<TApplication>(builder, (provider, context) =>
        {
            context.WpfApplication?.Initialize();
            var locator = viewModelLocatorFunc(provider);
            context.WpfApplication?.InitializeLocator(locator);
        });
    }

    private static IHostApplicationBuilder AddWpfInitialization<TApplication>(IHostApplicationBuilder builder, Action<IServiceProvider, IWpfContext<TApplication>> initialize)
        where TApplication : Application, IApplicationInitializeComponent
    {
        foreach (var service in builder.Services)
        {
            if (service.ServiceType == typeof(WpfInitialization<TApplication>))
            {
                throw new InvalidOperationException($"Do not use .UseWpfInitialization<{typeof(TApplication).Name}> and .UseWpfViewModelLocator<{typeof(TApplication).Name}> together or call them multiple times.");
            }
        }

        builder.Services.AddSingleton(new WpfInitialization<TApplication>(initialize));
        return builder;
    }
}
