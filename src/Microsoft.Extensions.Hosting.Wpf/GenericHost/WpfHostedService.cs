using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost;

public class WpfHostedService<TApplication>
    : IHostedService where TApplication : Application, IApplicationInitializeComponent
{
    private readonly ILogger<WpfHostedService<TApplication>> _logger;
    private readonly IWpfThread<TApplication> _wpfThread;
    private readonly IWpfContext<TApplication> _wpfContext;

    /// <summary>
    /// The constructor which takes all the DI objects
    /// </summary>
    /// <param name="logger">ILogger</param>
    /// <param name="wpfThread">WpfThread</param>
    /// <param name="wpfContext">WpfContext</param>
    public WpfHostedService(ILogger<WpfHostedService<TApplication>> logger, IWpfThread<TApplication> wpfThread, IWpfContext<TApplication> wpfContext)
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

        _logger.WpfStarting(nameof(WpfHostedService<TApplication>));
        // Make the UI thread go
        _wpfThread.Start();
        _logger.WpfThreadStarted();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_wpfContext is { IsRunning: true, WpfApplication: { } })
        {
            //If true means that WPF is already shutdown internally. Usually happens when ShutdownMode is set to OnLastWindowClose or OnMainWindowClose
            //We need to check otherwise if we call Shutdown twice we get an exception
            bool isShutdown = _wpfContext.WpfApplication.IsWpfShutdown();
            if (!isShutdown)
            {
                _logger.WpfStopping();
                // Stop application
                await _wpfContext.Dispatcher.InvokeAsync(() =>
                {
                    _wpfContext.IsRunning = false;
                    _wpfContext.WpfApplication.Shutdown(0);
                });
            }
        }
    }
}