using System;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Locator;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// The initialization steps run on the UI thread after InitializeComponent and before Run. Shared by the registration
/// (services.AddWpf...) and the post-build (host.UseWpf...) extensions, so both behave the same.
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
internal static class WpfInitializationActions
{
    /// <summary>Calls <see cref="IApplicationInitialize.Initialize"/>.</summary>
    public static Action<IServiceProvider, IWpfContext<TApplication>> Initialize<TApplication>()
        where TApplication : Application, IApplicationInitializeComponent, IApplicationInitialize
    {
        return (_, context) => context.WpfApplication?.Initialize();
    }

    /// <summary>
    /// Calls <see cref="IApplicationInitialize.Initialize"/>, then creates the view model locator with the host's service provider
    /// and passes it to <see cref="IViewModelLocatorInitialization{TViewModelLocator}.InitializeLocator"/>.
    /// </summary>
    public static Action<IServiceProvider, IWpfContext<TApplication>> ViewModelLocator<TApplication, TViewModelLocator>(Func<IServiceProvider, TViewModelLocator> viewModelLocatorFunc)
        where TApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<TViewModelLocator>
    {
        return (provider, context) =>
        {
            context.WpfApplication?.Initialize();
            var viewModelLocator = viewModelLocatorFunc(provider);
            context.WpfApplication?.InitializeLocator(viewModelLocator);
        };
    }
}
