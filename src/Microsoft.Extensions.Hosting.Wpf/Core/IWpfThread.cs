using System.Threading;
using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf.Core;

public interface IWpfThread
{
    IWpfContext WpfContext { get; }

    Thread MainThread { get; }

    SynchronizationContext SynchronizationContext { get; }

    /// <summary>
    /// Handle the application exit.
    /// </summary>
    void HandleApplicationExit();
}

public interface IWpfThread<out TApplication>
    : IWpfThread where TApplication : Application, IApplicationInitializeComponent
{
    new IWpfContext<TApplication> WpfContext { get; }
}
