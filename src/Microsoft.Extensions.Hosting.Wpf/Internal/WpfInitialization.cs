using System;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

internal sealed class WpfInitialization<TApplication>
    where TApplication : Application, IApplicationInitializeComponent
{
    public WpfInitialization(Action<IServiceProvider, IWpfContext<TApplication>> initialize)
    {
        Initialize = initialize;
    }

    public Action<IServiceProvider, IWpfContext<TApplication>> Initialize { get; }
}
