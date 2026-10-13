using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.ScenarioApp;

/// <summary>Once the host has started and the window is shown, ends the application the way the scenario says.</summary>
public sealed class ScenarioDriver : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IWpfContext<ScenarioApplication> _context;
    private readonly IWpfThread<ScenarioApplication> _thread;
    private readonly Scenario _scenario;
    private readonly ScenarioLog _log;

    public ScenarioDriver(IHostApplicationLifetime lifetime, IWpfContext<ScenarioApplication> context, IWpfThread<ScenarioApplication> thread, Scenario scenario, ScenarioLog log)
    {
        _lifetime = lifetime;
        _context = context;
        _thread = thread;
        _scenario = scenario;
        _log = log;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() =>
        {
            _log.Event("HostStarted");
            _ = Task.Run(StopScenarioAsync);
        });
        _lifetime.ApplicationStopping.Register(() => _log.Event("HostStopping"));
        _lifetime.ApplicationStopped.Register(() => _log.Event("HostStopped"));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task StopScenarioAsync()
    {
        try
        {
            // WpfHostedService.StartAsync waits for the application, so it must exist once the host has started.
            var application = _context.WpfApplication;
            if (application is null)
            {
                _log.Info("ApplicationMissingAtHostStarted", true);
                _lifetime.StopApplication();
                return;
            }

            var window = await application.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            _log.Event("Stopping", _scenario.Stop);
            switch (_scenario.Stop)
            {
                case StopKind.WpfExit:
                    await application.Dispatcher.InvokeAsync(() => application.Shutdown(Scenario.ApplicationExitCode));
                    break;
                case StopKind.HostStop:
                    _lifetime.StopApplication();
                    break;
                case StopKind.TrayExit:
                    await application.Dispatcher.InvokeAsync(_thread.HandleApplicationExit);
                    break;
                case StopKind.LastWindowClosed:
                    await application.Dispatcher.InvokeAsync(window.Close);
                    break;
            }
        }
        catch (Exception exception)
        {
            _log.Info("DriverFailed", $"{exception.GetType().Name}: {exception.Message}");
            _lifetime.StopApplication();
        }
    }
}
