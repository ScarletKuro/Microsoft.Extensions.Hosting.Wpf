using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost;

internal sealed class WpfHostedService<TApplication>
    : IHostedService where TApplication : Application, IApplicationInitializeComponent
{
    private readonly ILogger<WpfHostedService<TApplication>> _logger;
    private readonly WpfThread<TApplication> _wpfThread;
    private readonly WpfContext<TApplication> _wpfContext;

    /// <summary>
    /// The constructor which takes all the DI objects
    /// </summary>
    /// <param name="logger">ILogger</param>
    /// <param name="wpfThread">WpfThread</param>
    /// <param name="wpfContext">WpfContext</param>
    public WpfHostedService(ILogger<WpfHostedService<TApplication>> logger, WpfThread<TApplication> wpfThread, WpfContext<TApplication> wpfContext)
    {
        _logger = logger;
        _wpfThread = wpfThread;
        _wpfContext = wpfContext;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        _logger.WpfStarting();
        // Make the UI thread go
        _wpfThread.Start();
        _logger.WpfThreadStarted();

        // Wait until the application is created and initialized on the UI thread. This makes startup failures fail the host
        // instead of crashing the process, and guarantees the application exists before ApplicationStarted is raised.
        return WaitAsync(_wpfThread.Initialized, cancellationToken);
    }

    private static async Task WaitAsync(Task task, CancellationToken cancellationToken)
    {
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken)))
        {
            var completed = await Task.WhenAny(task, cancelled.Task).ConfigureAwait(false);
            await completed.ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_wpfContext is { IsRunning: true, WpfApplication: not null })
        {
            //If true WPF is already shutting down on its own, e.g. ShutdownMode is OnLastWindowClose or OnMainWindowClose
            //and the last window was closed. Then there is nothing to stop; the aborted-operation catch below covers the remaining race.
            bool isShutdown = _wpfContext.Dispatcher.HasShutdownStarted;
            if (!isShutdown)
            {
                _logger.WpfStopping();
                // Stop application
                var operation = _wpfContext.Dispatcher.InvokeAsync(() =>
                {
                    _wpfContext.IsRunning = false;
                    _wpfContext.WpfApplication.Shutdown(0);
                });

                try
                {
                    await operation;
                }
                catch (TaskCanceledException) when (operation.Status == DispatcherOperationStatus.Aborted)
                {
                    //WPF was already shutting down on its own (e.g. Exit triggered StopApplication) and the dispatcher
                    //shut down before running our callback. The application is stopped, so there is nothing left to do.
                }
            }
        }
    }
}