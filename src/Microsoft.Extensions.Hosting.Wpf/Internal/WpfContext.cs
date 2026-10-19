using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Internal implementation of <see cref="IWpfContext{TApplication}"/>. 
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
internal sealed class WpfContext<TApplication>
    : IWpfContext<TApplication> where TApplication : Application
{
    // Written on the UI thread and read from host threads (StoppingAsync, HandleApplicationExit callers).
    private volatile bool _isRunning;

    /// <summary>
    /// Shows if WPF is running inside Microsoft.Extensions.Hosting
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        internal set => _isRunning = value;
    }

    /// <inheritdoc />
    Application? IWpfContext.WpfApplication => WpfApplication;

    /// <inheritdoc />
    public TApplication? WpfApplication { get; private set; }

    /// <inheritdoc />
    public Dispatcher Dispatcher => WpfApplication?.Dispatcher ?? throw new InvalidOperationException($"{nameof(WpfApplication)} is not initialized!");

    internal void SetWpfApplication(TApplication application)
    {
        WpfApplication = application;
    }
}