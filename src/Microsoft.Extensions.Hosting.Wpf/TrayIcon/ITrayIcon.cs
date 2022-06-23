using System;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;

namespace Microsoft.Extensions.Hosting.Wpf.TrayIcon;

public interface ITrayIcon<out TApplication>
    : IDisposable where TApplication : Application, IApplicationInitializeComponent, new()
{
    IWpfThread<TApplication> WpfThread { get; }

    void CreateNotifyIcon();
}