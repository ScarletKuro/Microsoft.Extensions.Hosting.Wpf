using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost;

public class WpfLifetime : IHostLifetime, IDisposable
{
    private readonly ManualResetEvent _shutdownBlock = new(false);
    private CancellationTokenRegistration _applicationStartedRegistration;
    private CancellationTokenRegistration _applicationStoppingRegistration;
    private CancellationTokenRegistration _applicationStoppedRegistration;

    private IWpfContext WpfContext { get; }

    private WpfLifeTimeOptions Options { get; }

    private IHostEnvironment Environment { get; }

    private IHostApplicationLifetime ApplicationLifetime { get; }

    private HostOptions HostOptions { get; }

    private ILogger Logger { get; }

    public WpfLifetime(
        IWpfContext wpfContext,
        IOptions<WpfLifeTimeOptions> options,
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        IOptions<HostOptions> hostOptions)
        : this(wpfContext, options, environment, applicationLifetime, hostOptions, NullLoggerFactory.Instance)
    {
    }

    public WpfLifetime(
        IWpfContext wpfContext,
        IOptions<WpfLifeTimeOptions> options,
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        IOptions<HostOptions> hostOptions,
        ILoggerFactory loggerFactory)
    {
        ThrowHelper.ThrowIfNull(wpfContext, nameof(wpfContext));
        ThrowHelper.ThrowIfNull(options, nameof(options));
        ThrowHelper.ThrowIfNull(options.Value, nameof(options));
        ThrowHelper.ThrowIfNull(applicationLifetime);
        ThrowHelper.ThrowIfNull(environment);
        ThrowHelper.ThrowIfNull(hostOptions, nameof(hostOptions));
        ThrowHelper.ThrowIfNull(hostOptions.Value, nameof(hostOptions));
        ThrowHelper.ThrowIfNull(loggerFactory);

        WpfContext = wpfContext;
        Options = options.Value;
        Environment = environment;
        ApplicationLifetime = applicationLifetime;
        HostOptions = hostOptions.Value;
        Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        //Indicate that we are using our custom lifetime
        WpfContext.IsLifetimeLinked = true;

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
        //Hookup here because WpfApplication is initialized only when WpfHostedService is up
        //Make sure to do it only on Main UI thread because it have VerifyAccess()
        WpfContext.WpfApplication?.InvokeIfRequired(() =>
        {
            WpfContext.WpfApplication.Exit += OnWpfExiting;
        });
    }

    private void OnApplicationStopping()
    {
        if (!Options.SuppressStatusMessages)
        {
            Logger.WpfApplicationShuttingDown();
        }

        //Make sure to do it only on Main UI thread because it have VerifyAccess()
        WpfContext.WpfApplication?.InvokeIfRequired(() =>
        {
            WpfContext.WpfApplication.Exit -= OnWpfExiting;
        });
    }

    private void OnWpfApplicationStopped()
    {
        if (!Options.SuppressStatusMessages)
        {
            Logger.WpfApplicationStopped();
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        ApplicationLifetime.StopApplication();
        if (!_shutdownBlock.WaitOne(HostOptions.ShutdownTimeout))
        {
            Logger.WaitingHost();
        }

        _shutdownBlock.WaitOne();

        // On Linux if the shutdown is triggered by SIGTERM then that's signaled with the 143 exit code.
        // Suppress that since we shut down gracefully. https://github.com/aspnet/AspNetCore/issues/6526
        System.Environment.ExitCode = 0;
    }

    private void OnWpfExiting(object? sender, ExitEventArgs e)
    {
        ApplicationLifetime.StopApplication();
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