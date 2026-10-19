﻿using System.Threading;
using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf.Core;

/// <summary>
/// The WPF UI thread hosted by <c>AddWpf</c>. Use it from components that do not know the application type,
/// e.g. a tray icon; use <see cref="IWpfThread{TApplication}"/> for typed access to the application.
/// </summary>
public interface IWpfThread
{
    /// <summary>
    /// The WPF application and its dispatcher.
    /// </summary>
    IWpfContext WpfContext { get; }

    /// <summary>
    /// The STA thread that runs the WPF application.
    /// </summary>
    Thread MainThread { get; }

    /// <summary>
    /// The synchronization context of the UI thread. Throws <see cref="System.InvalidOperationException"/> until the UI thread
    /// has started.
    /// </summary>
    SynchronizationContext SynchronizationContext { get; }

    /// <summary>
    /// Exits the application from code that is not a window, e.g. a tray icon's Exit command: closes all windows and stops
    /// the host, which then shuts WPF down. Does nothing if the application is not running. Can be called from any thread.
    /// </summary>
    void HandleApplicationExit();
}

/// <summary>
/// The WPF UI thread hosted by <c>AddWpf&lt;TApplication&gt;</c>, with typed access to the application.
/// </summary>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
public interface IWpfThread<out TApplication>
    : IWpfThread where TApplication : Application, IApplicationInitializeComponent
{
    /// <summary>
    /// The WPF application, typed, and its dispatcher.
    /// </summary>
    new IWpfContext<TApplication> WpfContext { get; }
}
