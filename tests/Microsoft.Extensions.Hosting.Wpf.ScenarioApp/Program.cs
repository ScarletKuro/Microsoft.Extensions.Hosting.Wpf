using System.Windows.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Threading;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace Microsoft.Extensions.Hosting.Wpf.ScenarioApp;

/// <summary>
/// Runs one <see cref="Scenario"/>: builds the host exactly like an application would, runs it until the scenario ends it,
/// and reports what happened. The process exit code is the real one (Main returns void, so it is Environment.ExitCode).
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var scenario = Scenario.Parse(args);
        var log = new ScenarioLog();
        log.Info("Scenario", scenario);

        var host = BuildHost(scenario, log);
        var thread = host.Services.GetRequiredService<IWpfThread<ScenarioApplication>>();
        var context = host.Services.GetRequiredService<IWpfContext<ScenarioApplication>>();
        log.Info("UiThread", thread.MainThread.ManagedThreadId);
        log.Info("Lifetime", host.Services.GetRequiredService<IHostLifetime>().GetType().Name);

        try
        {
            // Blocks until the host stops, then disposes it.
            if (scenario.Failure == FailureKind.CanceledDuringInitialization)
            {
                host.RunAsync(host.Services.GetRequiredService<StartupCancellation>().Source.Token).GetAwaiter().GetResult();
            }
            else
            {
                host.Run();
            }
        }
        catch (Exception exception)
        {
            log.Info("RunFailed", $"{exception.GetType().Name}: {exception.Message}");
            Environment.ExitCode = Scenario.HostFailedExitCode;
        }

        // Recorded immediately: disposing the host must already have waited for the UI thread. No UI thread may outlive the host.
        log.Info("UiThreadAliveAfterRun", thread.MainThread.IsAlive);
        log.Info("IsRunning", context.IsRunning);

        // Cleanup only (not reported): give a misbehaving UI thread the chance to show what it does after the host is gone.
        if ((thread.MainThread.ThreadState & ThreadState.Unstarted) == 0)
        {
            thread.MainThread.Join(TimeSpan.FromSeconds(5));
        }
        log.Info("IsRunningAfterCleanup", context.IsRunning);
        if (Environment.ExitCode != Scenario.HostFailedExitCode)
        {
            log.Info("Completed", true);
        }
    }

    private static IHost BuildHost(Scenario scenario, ScenarioLog log)
    {
        switch (scenario.Builder)
        {
            case BuilderKind.HostBuilder:
            {
                var hostBuilder = Host.CreateDefaultBuilder()
                    .ConfigureServices(services => ConfigureCommonServices(services, scenario, log));
                if (scenario.Lifetime == LifetimeKind.Wpf)
                {
                    hostBuilder.UseWpfLifetime(options => options.SuppressStatusMessages = true);
                }

                if (scenario.Initialization == InitializationKind.Initialization)
                {
                    hostBuilder.UseWpfInitialization<ScenarioApplication>();
                }
                else if (scenario.Initialization == InitializationKind.Locator)
                {
                    hostBuilder.UseWpfViewModelLocator<ScenarioApplication, ScenarioLocator>(provider => CreateLocator(provider, log));
                }
                else if (scenario.Initialization == InitializationKind.LocatorInstance)
                {
                    hostBuilder.UseWpfViewModelLocator<ScenarioApplication, ScenarioLocator>(new ScenarioLocator(LocatorInstanceSource));
                }

                return ConfigurePostBuild(hostBuilder.Build(), scenario, log);
            }
            case BuilderKind.ApplicationBuilder:
            {
                var builder = Host.CreateApplicationBuilder();
                ConfigureCommonServices(builder.Services, scenario, log);
                ConfigureBuilderServices(builder.Services, scenario, log);
                return ConfigurePostBuild(builder.Build(), scenario, log);
            }
            case BuilderKind.WebApplicationBuilder:
            {
                var builder = WebApplication.CreateBuilder();
                // Any free local port, so parallel runs do not collide.
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                ConfigureCommonServices(builder.Services, scenario, log);
                ConfigureBuilderServices(builder.Services, scenario, log);
                return ConfigurePostBuild(builder.Build(), scenario, log);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario.Builder, null);
        }
    }

    private static void ConfigureCommonServices(IServiceCollection services, Scenario scenario, ScenarioLog log)
    {
        services.AddSingleton(log);
        services.AddSingleton(scenario);
        services.AddSingleton<ScenarioResource>();
        services.AddSingleton<LateScenarioResource>();
        if (scenario.Failure == FailureKind.ApplicationFactory)
        {
            services.AddWpf<ScenarioApplication>(_ => throw new InvalidOperationException(Scenario.StartupFailureMessage));
        }
        else if (scenario.CustomFactory)
        {
            services.AddWpf(provider =>
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                // Queued before the application exists, at the same (highest) priority as WPF's own startup operation,
                // so it runs first: it must not be mistaken for the application's startup.
                dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => log.Event("FactoryQueuedWork")));

                // A worker keeps posting to the UI dispatcher while the application is constructed: none of those
                // operations may be mistaken for the application's startup either.
                var constructing = true;
                var workerPosts = 0;
                var worker = new Thread(() =>
                {
                    while (Volatile.Read(ref constructing))
                    {
                        dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => { }));
                        workerPosts++;
                    }
                }) { IsBackground = true };
                worker.Start();
                // Let the worker get going before construction starts.
                while (Volatile.Read(ref workerPosts) == 0)
                {
                    Thread.Yield();
                }

                var application = ActivatorUtilities.CreateInstance<ScenarioApplication>(provider);
                Volatile.Write(ref constructing, false);
                worker.Join();
                log.Info("WorkerPostsDuringConstruction", workerPosts);
                return application;
            });
        }
        else
        {
            services.AddWpf<ScenarioApplication>();
        }

        services.AddWpfTrayIcon<ScenarioTrayIcon>();
        services.AddSingleton<StartupCancellation>();
        services.AddSingleton<HostDisposalSignal>();
        if (scenario.Failure == FailureKind.CanceledDuringInitialization)
        {
            // Blocks UI initialization, cancels startup meanwhile, and finishes only after the host has been disposed.
            services.AddWpfTrayIcon<BlockingInitializationComponent>();
        }

        // Both overloads; JoinableTaskFactory is constructor-injected into ScenarioDriver before the UI thread starts.
        if (scenario.Lifetime == LifetimeKind.Wpf)
        {
            services.AddThreadSwitching<ScenarioApplication>();
        }
        else
        {
            services.AddThreadSwitching();
        }

        services.AddHostedService<ScenarioDriver>();
        if (scenario.Failure is FailureKind.LaterHostedService or FailureKind.LaterHostedServiceStarted)
        {
            // Registered after AddWpf. Failing in StartAsync, WPF is never created (it starts after every StartAsync);
            // failing in StartedAsync, WPF is already running and disposing the host must shut it down.
            services.AddHostedService<FailingHostedService>();
        }
    }

    // IHostApplicationBuilder: everything is registered on builder.Services before Build().
    private static void ConfigureBuilderServices(IServiceCollection services, Scenario scenario, ScenarioLog log)
    {
        if (scenario.Lifetime == LifetimeKind.Wpf)
        {
            services.AddWpfLifetime(options => options.SuppressStatusMessages = true);
        }

        if (scenario.Initialization == InitializationKind.Initialization)
        {
            services.AddWpfInitialization<ScenarioApplication>();
        }
        else if (scenario.Initialization == InitializationKind.Locator)
        {
            services.AddWpfViewModelLocator<ScenarioApplication, ScenarioLocator>(provider => CreateLocator(provider, log));
        }
        else if (scenario.Initialization == InitializationKind.LocatorInstance)
        {
            services.AddWpfViewModelLocator<ScenarioApplication, ScenarioLocator>(new ScenarioLocator(LocatorInstanceSource));
        }
    }

    // Post-build IHost extensions, available for every builder.
    private static IHost ConfigurePostBuild(IHost host, Scenario scenario, ScenarioLog log)
    {
        if (scenario.Initialization == InitializationKind.InitializationPostBuild)
        {
            host.UseWpfInitialization<ScenarioApplication>();
        }
        else if (scenario.Initialization == InitializationKind.LocatorPostBuild)
        {
            host.UseWpfViewModelLocator<ScenarioApplication, ScenarioLocator>(provider => CreateLocator(provider, log));
        }
        else if (scenario.Initialization == InitializationKind.LocatorInstancePostBuild)
        {
            host.UseWpfViewModelLocator<ScenarioApplication, ScenarioLocator>(new ScenarioLocator(LocatorInstanceSource));
        }

        return host;
    }

    /// <summary><see cref="ScenarioLocator.Source"/> of a locator created by the factory overloads.</summary>
    public const string LocatorFactorySource = "factory";

    /// <summary><see cref="ScenarioLocator.Source"/> of the instance passed to the instance overloads.</summary>
    public const string LocatorInstanceSource = "instance";

    private static ScenarioLocator CreateLocator(IServiceProvider provider, ScenarioLog log)
    {
        // The factory receives the host's root provider.
        log.Event("LocatorFactory", provider.GetRequiredService<ScenarioLog>() == log);
        return new ScenarioLocator(LocatorFactorySource);
    }
}

/// <summary>Token passed to <c>host.RunAsync</c> in <see cref="FailureKind.CanceledDuringInitialization"/>.</summary>
public sealed class StartupCancellation
{
    public CancellationTokenSource Source { get; } = new();
}

/// <summary>
/// Disposed by the host when it disposes its services: created after the WPF thread, so (reverse creation order) before it.
/// Signals that host disposal has begun.
/// </summary>
public sealed class HostDisposalSignal : IDisposable
{
    private readonly ScenarioLog _log;
    private readonly ManualResetEventSlim _disposed = new();

    public HostDisposalSignal(ScenarioLog log)
    {
        _log = log;
    }

    public bool WaitForHostDisposal(TimeSpan timeout) => _disposed.Wait(timeout);

    public void Dispose()
    {
        _log.Event("HostDisposalStarted");
        _disposed.Set();
    }
}

/// <summary>
/// An <see cref="Microsoft.Extensions.Hosting.Wpf.Core.IWpfComponent"/> whose initialization cancels host startup and then
/// keeps the UI thread busy until the host has begun disposing its services. This establishes the interleaving explicitly:
/// startup is cancelled and abandoned while the UI thread still initializes. Afterwards it must not start the application.
/// </summary>
public sealed class BlockingInitializationComponent : ITrayIcon
{
    private readonly ScenarioLog _log;
    private readonly StartupCancellation _cancellation;
    private readonly HostDisposalSignal _hostDisposal;

    public BlockingInitializationComponent(ScenarioLog log, StartupCancellation cancellation, HostDisposalSignal hostDisposal)
    {
        _log = log;
        _cancellation = cancellation;
        _hostDisposal = hostDisposal;
    }

    public void InitializeComponent()
    {
        _log.Event("BlockingInitializeStarted");
        _cancellation.Source.Cancel();
        // The library gives up waiting for this initialization (bounded), the host fails and begins disposing; only then continue.
        _log.Info("HostDisposalObserved", _hostDisposal.WaitForHostDisposal(TimeSpan.FromSeconds(60)));
        _log.Event("BlockingInitializeFinished");
    }

    public void Dispose() => _log.Event("BlockingComponentDispose");
}

/// <summary>A hosted service registered after AddWpf whose start fails while WPF is already running.</summary>
public sealed class FailingHostedService : IHostedLifecycleService
{
    private readonly ScenarioLog _log;
    private readonly Scenario _scenario;

    public FailingHostedService(ScenarioLog log, Scenario scenario)
    {
        _log = log;
        _scenario = scenario;
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => FailAsync(FailureKind.LaterHostedService, "FailingHostedServiceStarting");

    public Task StartedAsync(CancellationToken cancellationToken) => FailAsync(FailureKind.LaterHostedServiceStarted, "FailingHostedServiceStarted");

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private Task FailAsync(FailureKind failure, string eventName)
    {
        if (_scenario.Failure != failure)
        {
            return Task.CompletedTask;
        }

        _log.Event(eventName);
        throw new InvalidOperationException(Scenario.StartupFailureMessage);
    }
}
