using System.Threading;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;

namespace Microsoft.Extensions.Hosting.Wpf.Core
{
    public interface IWpfThread
    {
        IWpfContext WpfContext { get; }

        Thread MainThread { get; }

        SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        /// Start the WPF thread.
        /// </summary>
        void Start();

        /// <summary>
        /// Handle the application exit.
        /// </summary>
        void HandleApplicationExit();
    }

    public interface IWpfThread<out TApplication>
        : IWpfThread where TApplication : Application, IApplicationInitializeComponent, new()
    {
        new IWpfContext<TApplication> WpfContext { get; }
    }
}
