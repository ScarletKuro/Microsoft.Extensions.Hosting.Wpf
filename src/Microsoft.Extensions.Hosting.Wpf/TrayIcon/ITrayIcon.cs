using System;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;

namespace Microsoft.Extensions.Hosting.Wpf.TrayIcon
{
    public interface ITrayIcon<TApplication>
        : IDisposable where TApplication : Application, IApplicationInitializeComponent, new()
    {
        WpfThread<TApplication> WpfThread { get; }

        void CreateNotifyIcon();
    }
}
