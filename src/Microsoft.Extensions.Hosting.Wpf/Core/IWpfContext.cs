using System.Windows;
using System.Windows.Threading;

namespace Microsoft.Extensions.Hosting.Wpf.Core;

/// <summary>
/// The WPF application hosted by <c>AddWpf</c> and its dispatcher. Use it from components that do not know the application
/// type; use <see cref="IWpfContext{TApplication}"/> for typed access to the application.
/// </summary>
public interface IWpfContext
{
    /// <summary>
    /// <c>true</c> while the application runs (<see cref="Application.Run()"/>), i.e. from just before its startup until it has
    /// exited. Safe to read from any thread.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// The WPF application, or <c>null</c> until the UI thread has created it (the host starts WPF after every hosted service
    /// has started).
    /// </summary>
    Application? WpfApplication { get; }

    /// <summary>
    /// The dispatcher of the UI thread. Throws <see cref="System.InvalidOperationException"/> until <see cref="WpfApplication"/>
    /// has been created.
    /// </summary>
    Dispatcher Dispatcher { get; }
}

/// <summary>
/// The WPF application hosted by <c>AddWpf&lt;TApplication&gt;</c>, typed, and its dispatcher.
/// </summary>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
public interface IWpfContext<out TApplication> : IWpfContext
    where TApplication : Application
{
    /// <summary>
    /// The WPF application, typed, or <c>null</c> until the UI thread has created it.
    /// </summary>
    new TApplication? WpfApplication { get; }
}
