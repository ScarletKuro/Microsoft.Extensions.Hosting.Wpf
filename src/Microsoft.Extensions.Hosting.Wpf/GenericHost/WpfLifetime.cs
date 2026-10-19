using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost;

/// <summary>
/// <see cref="IHostLifetime"/> for a windowed WPF application, used instead of the default ConsoleLifetime: it does not handle
/// Ctrl+C or print console shutdown hints, logs WPF status messages (see <see cref="WpfLifetimeOptions"/>), and waits for the host
/// to shut down gracefully when the process exits. Stopping the host when WPF exits does not depend on it.
/// </summary>
public class WpfLifetime : IHostLifetime, IDisposable
{
#pragma warning disable CA2213
    private readonly ManualResetEvent _shutdownBlock = new(false);
#pragma warning restore CA2213
    private CancellationTokenRegistration _applicationStartedRegistration;
    private CancellationTokenRegistration _applicationStoppingRegistration;
    private CancellationTokenRegistration _applicationStoppedRegistration;

    private WpfLifetimeOptions Options { get; }

    private IHostEnvironment Environment { get; }

    private IHostApplicationLifetime ApplicationLifetime { get; }

    private HostOptions HostOptions { get; }

    private ILogger Logger { get; }

    public WpfLifetime(
        IOptions<WpfLifetimeOptions> options,
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        IOptions<HostOptions> hostOptions)
        : this(options, environment, applicationLifetime, hostOptions, NullLoggerFactory.Instance)
    {
    }

    public WpfLifetime(
        IOptions<WpfLifetimeOptions> options,
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        IOptions<HostOptions> hostOptions,
        ILoggerFactory loggerFactory)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfNull(options.Value, nameof(options));
        ThrowHelper.ThrowIfNull(applicationLifetime);
        ThrowHelper.ThrowIfNull(environment);
        ThrowHelper.ThrowIfNull(hostOptions);
        ThrowHelper.ThrowIfNull(hostOptions.Value, nameof(hostOptions));
        ThrowHelper.ThrowIfNull(loggerFactory);

        Options = options.Value;
        Environment = environment;
        ApplicationLifetime = applicationLifetime;
        HostOptions = hostOptions.Value;
        Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _applicationStartedRegistration = ApplicationLifetime
            .ApplicationStarted
            .Register(state => ((WpfLifetime)state!).OnApplicationStarted(), this);

        _applicationStoppingRegistration = ApplicationLifetime
            .ApplicationStopping
            .Register(state => ((WpfLifetime)state!).OnApplicationStopping(), this);

        _applicationStoppedRegistration = ApplicationLifetime
            .ApplicationStopped
            .Register(state => ((WpfLifetime)state!).OnWpfApplicationStopped(), this);

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        //Applications start immediately.
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnApplicationStarted()
    {
        if (!Options.SuppressStatusMessages)
        {
            Logger.WpfApplicationStarted();
            Logger.LifeTime(nameof(WpfLifetime));
            Logger.HostingEnvironment(Environment.EnvironmentName);
            Logger.ContentRootPath(Environment.ContentRootPath);
        }
        //Stopping the host when WPF exits is done by WpfThread, which subscribes to Application.Exit on the UI thread before Run().
    }

    private void OnApplicationStopping()
    {
        if (!Options.SuppressStatusMessages)
        {
            Logger.WpfApplicationShuttingDown();
        }
    }

    private void OnWpfApplicationStopped()
    {
        if (!Options.SuppressStatusMessages)
        {
            Logger.WpfApplicationStopped();
        }
    }

    internal void OnProcessExit(object? sender, EventArgs e)
    {
        ApplicationLifetime.StopApplication();
        if (!_shutdownBlock.WaitOne(HostOptions.ShutdownTimeout))
        {
            Logger.WaitingHost();
        }

        // wait one more time after the above log message, but only for ShutdownTimeout, so it doesn't hang forever
        _shutdownBlock.WaitOne(HostOptions.ShutdownTimeout);

        // Unlike ConsoleLifetime, do not reset Environment.ExitCode: there is no SIGTERM (143) to suppress for WPF,
        // and it would discard the exit code passed to Application.Shutdown(exitCode).
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shutdownBlock.Set();

            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

            _applicationStartedRegistration.Dispose();
            _applicationStoppingRegistration.Dispose();
            _applicationStoppedRegistration.Dispose();
        }
    }
}