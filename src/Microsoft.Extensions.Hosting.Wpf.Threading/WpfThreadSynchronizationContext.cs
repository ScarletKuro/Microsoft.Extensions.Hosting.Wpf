using System.Threading;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Internal;

namespace Microsoft.Extensions.Hosting.Wpf.Threading;

/// <summary>
/// Forwards to the WPF UI thread when it is used, not when it is created.
/// The WPF synchronization context only exists once the UI thread runs, while JoinableTaskContext may be
/// resolved earlier, e.g. by a hosted service constructor (all hosted services are created before any is started).
/// </summary>
internal sealed class WpfThreadSynchronizationContext : SynchronizationContext
{
    private readonly IWpfThread _wpfThread;

    public WpfThreadSynchronizationContext(IWpfThread wpfThread)
    {
        _wpfThread = wpfThread;
    }

#pragma warning disable VSTHRD001 // This is the synchronization context JoinableTaskContext itself switches with; forwarding is its purpose.
    /// <inheritdoc />
    /// <remarks>
    /// Must not throw: JoinableTaskFactory terminates the process (Environment.FailFast) when it does. Work posted before the WPF
    /// application exists, e.g. SwitchToMainThreadAsync() while a hosted service is still starting, therefore waits until WPF has
    /// started instead of failing.
    /// </remarks>
    public override void Post(SendOrPostCallback d, object? state)
    {
        if (_wpfThread is IUiThreadPoster poster)
        {
            poster.Post(d, state);
        }
        else
        {
            _wpfThread.SynchronizationContext.Post(d, state);
        }
    }

    /// <inheritdoc />
    public override void Send(SendOrPostCallback d, object? state) => _wpfThread.SynchronizationContext.Send(d, state);
#pragma warning restore VSTHRD001

    /// <inheritdoc />
    public override SynchronizationContext CreateCopy() => this;
}
