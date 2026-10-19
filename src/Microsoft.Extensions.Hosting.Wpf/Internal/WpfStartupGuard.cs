using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Resolved by the UI thread once the application's Startup has completed, so the DI container, which disposes in reverse
/// creation order, disposes it before every service the application resolved during startup (including in OnStartup).
/// If the host never finished starting (a hosted service failed in StartedAsync after WPF had started), the host does not stop its
/// services; disposing this guard then stops the application, running its Exit handlers and disposing its components,
/// while those services still work.
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection.")]
internal sealed class WpfStartupGuard<TApplication> : IDisposable
    where TApplication : Application, IApplicationInitializeComponent
{
    private readonly WpfThread<TApplication> _wpfThread;

    public WpfStartupGuard(WpfThread<TApplication> wpfThread)
    {
        _wpfThread = wpfThread;
    }

    // After a normal stop the UI thread has already ended (StoppingAsync waited for it), so this returns immediately.
    public void Dispose() => _wpfThread.StopAndWait();
}
