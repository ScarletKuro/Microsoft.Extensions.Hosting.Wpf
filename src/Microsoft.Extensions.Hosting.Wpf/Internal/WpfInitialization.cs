using System;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

internal sealed class WpfInitialization<TApplication>
    where TApplication : Application, IApplicationInitializeComponent
{
    public static string AlreadyConfiguredMessage { get; } =
        $"WPF initialization for {typeof(TApplication).Name} is already configured. Use only one of UseWpfInitialization<{typeof(TApplication).Name}> / AddWpfInitialization<{typeof(TApplication).Name}> or UseWpfViewModelLocator<{typeof(TApplication).Name}> / AddWpfViewModelLocator<{typeof(TApplication).Name}>, and call it only once.";

    public static string MissingApplicationMessage { get; } =
        $"WPF initialization for {typeof(TApplication).Name} is configured, but {typeof(TApplication).Name} is not registered. Call services.AddWpf<{typeof(TApplication).Name}>().";

    public WpfInitialization(Action<IServiceProvider, IWpfContext<TApplication>> initialize)
    {
        Initialize = initialize;
    }

    public Action<IServiceProvider, IWpfContext<TApplication>> Initialize { get; }
}
