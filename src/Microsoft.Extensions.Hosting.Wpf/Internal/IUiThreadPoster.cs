using System.Threading;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Posts work to the WPF UI thread at any time, even before WPF has started. Used by the Threading package.
/// </summary>
internal interface IUiThreadPoster
{
    /// <summary>
    /// Posts <paramref name="callback"/> to the UI thread, or queues it until the application exists. Never throws.
    /// </summary>
    void Post(SendOrPostCallback callback, object? state);
}
