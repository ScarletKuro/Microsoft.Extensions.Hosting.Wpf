﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost;

public class WpfLifetime<TApplication> : IHostLifetime, IDisposable where TApplication : Application, new()
{
    private readonly ManualResetEvent _shutdownBlock = new(false);
    private CancellationTokenRegistration _applicationStartedRegistration;
    private CancellationTokenRegistration _applicationStoppingRegistration;
    private CancellationTokenRegistration _applicationStoppedRegistration;

    private IWpfContext<TApplication> WpfContext { get; }

    private WpfLifeTimeOptions Options { get; }

    private IHostEnvironment Environment { get; }

    private IHostApplicationLifetime ApplicationLifetime { get; }

    private HostOptions HostOptions { get; }

    private ILogger Logger { get; }

    public WpfLifetime(
        IWpfContext<TApplication> wpfContext,
        IOptions<WpfLifeTimeOptions> options,
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        IOptions<HostOptions> hostOptions)
        : this(wpfContext, options, environment, applicationLifetime, hostOptions, NullLoggerFactory.Instance)
    {
    }

    public WpfLifetime(
        IWpfContext<TApplication> wpfContext,
        IOptions<WpfLifeTimeOptions> options,
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        IOptions<HostOptions> hostOptions,
        ILoggerFactory loggerFactory)
    {
        WpfContext = wpfContext ?? throw new ArgumentNullException(nameof(wpfContext));
        Options = options.Value ?? throw new ArgumentNullException(nameof(options));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        HostOptions = hostOptions.Value ?? throw new ArgumentNullException(nameof(hostOptions));
        Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        //Indicate that we are using our custom lifetime
        WpfContext.IsLifetimeLinked = true;

        _applicationStartedRegistration = ApplicationLifetime
            .ApplicationStarted
            .Register(state => ((WpfLifetime<TApplication>)state!).OnApplicationStarted(), this);

        _applicationStoppingRegistration = ApplicationLifetime
            .ApplicationStopping
            .Register(state => ((WpfLifetime<TApplication>)state!).OnApplicationStopping(), this);

        _applicationStoppedRegistration = ApplicationLifetime
            .ApplicationStopped
            .Register(state => ((WpfLifetime<TApplication>)state!).OnWpfApplicationStopped(), this);

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
            Logger.LogInformation("Wpf application started");
            Logger.LogInformation("Lifetime: {Lifetime}", nameof(WpfLifetime<TApplication>));
            Logger.LogInformation("Hosting environment: {EnvName}", Environment.EnvironmentName);
            Logger.LogInformation("Content root path: {ContentRoot}", Environment.ContentRootPath);
        }
        //Hookup here because WpfApplication is initialized only when WpfHostedService is up
        if (WpfContext.WpfApplication is not null)
        {
            //Make sure to do it only on Main UI thread because it have VerifyAccess()
            WpfContext.WpfApplication.InvokeIfRequired(() =>
            {
                WpfContext.WpfApplication.Exit += OnWpfExiting;
            });
        }
    }

    private void OnApplicationStopping()
    {
        if (!Options.SuppressStatusMessages)
        {
            Logger.LogInformation("Wpf application is shutting down...");
        }

        if (WpfContext.WpfApplication is not null)
        {
            //Make sure to do it only on Main UI thread because it have VerifyAccess()
            WpfContext.WpfApplication.InvokeIfRequired(() =>
            {
                WpfContext.WpfApplication.Exit -= OnWpfExiting;
            });
        }
    }

    private void OnWpfApplicationStopped()
    {
        if (!Options.SuppressStatusMessages)
        {
            Logger.LogInformation("Wpf application was stopped.");
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        ApplicationLifetime.StopApplication();
        if (!_shutdownBlock.WaitOne(HostOptions.ShutdownTimeout))
        {
            Logger.LogInformation("Waiting for the host to be disposed, please ensure all 'IHost' instances are wrapped in 'using' blocks");
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
        _shutdownBlock.Set();

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        _applicationStartedRegistration.Dispose();
        _applicationStoppingRegistration.Dispose();
        _applicationStoppedRegistration.Dispose();
    }
}