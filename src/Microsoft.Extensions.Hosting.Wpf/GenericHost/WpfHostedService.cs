using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost
{
    public class WpfHostedService<TApplication>
        : IHostedService where TApplication : Application, IApplicationInitializeComponent, new()
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

            _logger.LogInformation($"Starting WPF application {nameof(WpfHostedService<TApplication>)}.");
            // Make the UI thread go
            _wpfThread.Start();
            _logger.LogInformation("WPF thread started.");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_wpfContext.IsRunning)
            {
                if (_wpfContext.WpfApplication is not null)
                {
                    //If true means that WPF is already shutdown internally. Usually happens when ShutdownMode is set to OnLastWindowClose or OnMainWindowClose
                    //We need to check otherwise if we call Shutdown twice we get an exception
                    bool isShutdown = _wpfContext.WpfApplication.IsWpfShutdown();
                    if (!isShutdown)
                    {
                        _logger.LogInformation("Stopping WPF with Application.Shutdown() due to application exit.");
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
    }
}
