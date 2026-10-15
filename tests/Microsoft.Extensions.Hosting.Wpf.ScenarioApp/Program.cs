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
            host.Run();
        }
        catch (Exception exception)
        {
            log.Info("RunFailed", $"{exception.GetType().Name}: {exception.Message}");
            Environment.ExitCode = Scenario.HostFailedExitCode;
        }

        // Unstarted (startup failed before the UI thread started) or ended: no UI thread may outlive the host.
        log.Info("UiThreadEnded", (thread.MainThread.ThreadState & ThreadState.Unstarted) != 0 || thread.MainThread.Join(TimeSpan.FromSeconds(10)));
        log.Info("IsRunning", context.IsRunning);
        if (Environment.ExitCode == Scenario.HostFailedExitCode)
        {
            return;
        }

        log.Info("IsRunning", context.IsRunning);
        log.Info("Completed", true);
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
        if (scenario.Failure == FailureKind.ApplicationFactory)
        {
            services.AddWpf<ScenarioApplication>(_ => throw new InvalidOperationException(Scenario.StartupFailureMessage));
        }
        else
        {
            services.AddWpf<ScenarioApplication>();
        }

        services.AddWpfTrayIcon<ScenarioTrayIcon>();
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
        if (scenario.Failure == FailureKind.LaterHostedService)
        {
            // Registered after AddWpf, so it starts after WPF is already running.
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

/// <summary>A hosted service registered after AddWpf whose start fails while WPF is already running.</summary>
public sealed class FailingHostedService : IHostedService
{
    private readonly ScenarioLog _log;

    public FailingHostedService(ScenarioLog log)
    {
        _log = log;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _log.Event("FailingHostedServiceStarting");
        throw new InvalidOperationException(Scenario.StartupFailureMessage);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
