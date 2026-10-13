using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Internal implementation of <see cref="IWpfContext{TApplication}"/>. 
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
internal sealed class WpfContext<TApplication>
    : IWpfContext<TApplication>, ILifetimeLinkableWpfContext where TApplication : Application
{
    /// <summary>
    /// Shows if <see cref="WpfLifetime"/> is used.
    /// </summary>
    public bool IsLifetimeLinked { get; internal set; }

    /// <summary>
    /// Shows if WPF is running inside Microsoft.Extensions.Hosting
    /// </summary>
    public bool IsRunning { get; internal set; }

    /// <inheritdoc />
    Application? IWpfContext.WpfApplication => WpfApplication;

    /// <inheritdoc />
    public TApplication? WpfApplication { get; private set; }

    /// <inheritdoc />
    public Dispatcher Dispatcher => WpfApplication?.Dispatcher ?? throw new InvalidOperationException($"{nameof(WpfApplication)} is not initialized!");

    /// <inheritdoc />
    void ILifetimeLinkableWpfContext.LinkLifetime() => IsLifetimeLinked = true;

    internal void SetWpfApplication(TApplication application)
    {
        WpfApplication = application;
    }
}