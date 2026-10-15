using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Hosting.Wpf.ScenarioApp;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

/// <summary>
/// Runs the whole flow (build, start, UI initialization, run, stop, process exit) for every combination of builder,
/// lifetime, initialization style and stop trigger, each in its own process: WPF allows only one Application per process.
/// </summary>
public class ScenarioTests
{
    // A hung host or UI thread is reported after this time. Override with SCENARIO_TIMEOUT_SECONDS.
    private static readonly TimeSpan ScenarioTimeout = TimeSpan.FromSeconds(
        int.TryParse(Environment.GetEnvironmentVariable("SCENARIO_TIMEOUT_SECONDS"), out var seconds) ? seconds : 60);

    public static TheoryData<BuilderKind, LifetimeKind, InitializationKind, StopKind> AllScenarios()
    {
        var data = new TheoryData<BuilderKind, LifetimeKind, InitializationKind, StopKind>();
        foreach (var builder in Enum.GetValues<BuilderKind>())
        foreach (var lifetime in Enum.GetValues<LifetimeKind>())
        foreach (var initialization in Enum.GetValues<InitializationKind>())
        foreach (var stop in Enum.GetValues<StopKind>())
        {
            data.Add(builder, lifetime, initialization, stop);
        }

        return data;
    }

    public static TheoryData<BuilderKind, LifetimeKind> AllHosts()
    {
        var data = new TheoryData<BuilderKind, LifetimeKind>();
        foreach (var builder in Enum.GetValues<BuilderKind>())
        foreach (var lifetime in Enum.GetValues<LifetimeKind>())
        {
            data.Add(builder, lifetime);
        }

        return data;
    }

    // Every UI lifecycle event the scenario app can log. Selected independently of the expectation, so unexpected,
    // missing or repeated calls change the sequence and fail the comparison.
    private static readonly string[] UiLifecycleEvents =
    {
        "Constructor", "InitializeComponent", "Initialize", "LocatorFactory", "InitializeLocator", "TrayInitialize",
        "Startup", "WindowShown", "SwitchedToMainThread", "Exit", "TrayDispose",
    };

    [Theory]
    [MemberData(nameof(AllScenarios))]
    public async Task HostAndWpfStartRunAndStopTogether(BuilderKind builder, LifetimeKind lifetime, InitializationKind initialization, StopKind stop)
    {
        var result = await RunAsync(new Scenario(builder, lifetime, initialization, stop));

        // The process ended by itself: host.Run() returned, the UI thread ended and the host is no longer running WPF.
        result.AssertInfo("Completed", "True");
        result.AssertInfo("UiThreadAliveAfterRun", "False");
        result.AssertInfo("IsRunning", "False");
        result.AssertNoInfo("RunFailed");
        result.AssertNoInfo("DriverFailed");
        result.AssertNoInfo("ApplicationMissingAtHostStarted");
        result.AssertInfo("Lifetime", lifetime == LifetimeKind.Wpf ? "WpfLifetime" : "ConsoleLifetime");

        // Everything WPF happens on the UI thread, exactly in this order and exactly once.
        var expectedUiEvents = new List<string> { "Constructor", "InitializeComponent" };
        expectedUiEvents.AddRange(initialization switch
        {
            InitializationKind.None => Array.Empty<string>(),
            InitializationKind.Initialization or InitializationKind.InitializationPostBuild => new[] { "Initialize" },
            InitializationKind.Locator or InitializationKind.LocatorPostBuild => new[] { "Initialize", "LocatorFactory", "InitializeLocator" },
            InitializationKind.LocatorInstance or InitializationKind.LocatorInstancePostBuild => new[] { "Initialize", "InitializeLocator" },
            _ => throw new ArgumentOutOfRangeException(nameof(initialization)),
        });
        expectedUiEvents.AddRange(new[] { "TrayInitialize", "Startup", "WindowShown", "SwitchedToMainThread", "Exit", "TrayDispose" });
        var uiThread = result.Info("UiThread");
        var uiEvents = result.Events.Where(e => UiLifecycleEvents.Contains(e.Name)).ToList();
        result.Assert(expectedUiEvents.SequenceEqual(uiEvents.Select(e => e.Name)),
            $"UI events were [{string.Join(", ", uiEvents.Select(e => e.Name))}], expected [{string.Join(", ", expectedUiEvents)}].");
        foreach (var uiEvent in uiEvents)
        {
            result.Assert(uiEvent.Thread == uiThread, $"{uiEvent.Name} ran on thread {uiEvent.Thread}, not on the UI thread {uiThread}.");
        }

        // The locator passed to InitializeLocator is the one the overload provides, and the factory gets the host's provider.
        if (initialization is InitializationKind.Locator or InitializationKind.LocatorPostBuild)
        {
            result.Assert(result.Event("LocatorFactory").Value == "True", "The locator factory did not receive the host's service provider.");
            result.Assert(result.Event("InitializeLocator").Value == ScenarioApp.Program.LocatorFactorySource, $"InitializeLocator received '{result.Event("InitializeLocator").Value}', not the factory's locator.");
        }
        else if (initialization is InitializationKind.LocatorInstance or InitializationKind.LocatorInstancePostBuild)
        {
            result.Assert(result.Event("InitializeLocator").Value == ScenarioApp.Program.LocatorInstanceSource, $"InitializeLocator received '{result.Event("InitializeLocator").Value}', not the configured instance.");
        }

        // AddThreadSwitching: the JoinableTaskFactory injected before the UI thread existed switched onto it (checked above).
        result.AssertInfo("JoinableTaskFactoryResolvedBeforeUiThread", "True");

        // The host started only once the application was initialized, and stopped.
        result.Assert(result.IndexOf("HostStarted") > result.IndexOf("TrayInitialize"), "ApplicationStarted was raised before the application was initialized.");
        result.Event("HostStopping");
        result.Event("HostStopped");

        // Application.Shutdown(exitCode) is the process exit code, as in a plain WPF application.
        var expectedExitCode = stop == StopKind.WpfExit ? Scenario.ApplicationExitCode : 0;
        result.Assert(result.Event("Exit").Value == expectedExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"WPF exited with {result.Event("Exit").Value}, expected {expectedExitCode}.");
        result.Assert(result.ExitCode == expectedExitCode, $"Process exit code was {result.ExitCode}, expected {expectedExitCode}.");

        // However the application stops, its cleanup runs before the host disposes the services it uses.
        AssertCleanupBeforeServicesDisposed(result);

        // Services resolved after startup (created after everything else) are disposed first: StopAsync must have waited
        // for WPF to exit completely before the host started disposing.
        result.AssertNoInfo("LateResourceUsedAfterDispose");
        result.Assert(result.IndexOf("Exit") < result.IndexOf("LateResourceDisposed"), "OnExit ran after the host disposed a service resolved after startup.");
    }

    // OnExit and component disposal use ScenarioResource, an application-owned disposable singleton.
    private static void AssertCleanupBeforeServicesDisposed(ScenarioResult result)
    {
        result.AssertNoInfo("ResourceUsedAfterDispose");
        var resourceDisposed = result.IndexOf("ResourceDisposed");
        result.Assert(result.IndexOf("Exit") < resourceDisposed, "OnExit ran after the host disposed the application's services.");
        result.Assert(result.IndexOf("TrayDispose") < resourceDisposed, "Components were disposed after the host disposed the application's services.");
    }

    public static TheoryData<BuilderKind, LifetimeKind, FailureKind> AllStartupFailures()
    {
        var data = new TheoryData<BuilderKind, LifetimeKind, FailureKind>();
        foreach (var builder in Enum.GetValues<BuilderKind>())
        foreach (var lifetime in Enum.GetValues<LifetimeKind>())
        foreach (var failure in new[] { FailureKind.ApplicationFactory, FailureKind.ApplicationStartup, FailureKind.LaterHostedService, FailureKind.LaterHostedServiceStarted, FailureKind.CanceledDuringInitialization })
        {
            data.Add(builder, lifetime, failure);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllStartupFailures))]
    public async Task StartupFailureFailsHostRunAndLeavesNoUiThread(BuilderKind builder, LifetimeKind lifetime, FailureKind failure)
    {
        var result = await RunAsync(new Scenario(builder, lifetime, InitializationKind.None, StopKind.HostStop, failure));

        // host.Run() threw the original exception on the main thread; the process did not crash from the UI thread.
        result.Assert(result.ExitCode == Scenario.HostFailedExitCode, $"Process exit code was {result.ExitCode}, expected {Scenario.HostFailedExitCode}.");
        var expectedFailure = failure == FailureKind.CanceledDuringInitialization ? "canceled" : Scenario.StartupFailureMessage;
        result.Assert(result.Info("RunFailed").Contains(expectedFailure, StringComparison.Ordinal), $"host.Run() did not fail with '{expectedFailure}'.");
        result.AssertNoInfo("Completed");
        result.Assert(result.Events.All(e => e.Name != "HostStarted"), "The host reported it started although startup failed.");

        // No UI thread outlives the host: checked right after host.Run() returned, so disposing the host must have waited.
        result.AssertInfo("UiThreadAliveAfterRun", "False");
        result.AssertInfo("IsRunning", "False");
        result.AssertInfo("IsRunningAfterCleanup", "False");
        var uiThread = result.Info("UiThread");
        switch (failure)
        {
            case FailureKind.ApplicationFactory:
                result.Assert(result.Events.All(e => e.Name != "Constructor"), "The application was created although its factory failed.");
                break;
            case FailureKind.ApplicationStartup:
                // OnStartup threw inside Application.Run(): host startup fails with that exception (checked above) instead of
                // the process crashing from the UI thread, and the components are disposed on the UI thread.
                result.Event("Startup");
                result.Assert(result.Event("TrayDispose").Thread == uiThread, "The tray component was not disposed on the UI thread.");
                break;
            case FailureKind.LaterHostedService:
                // WPF starts only after every hosted service has started, so a failing StartAsync means WPF is never created
                // and no application code can run while the host is disposed.
                result.Event("FailingHostedServiceStarting");
                result.Assert(result.Events.All(e => e.Name != "Constructor"), "The application was created although a hosted service failed to start.");
                break;
            case FailureKind.LaterHostedServiceStarted:
                // A hosted service failed in StartedAsync after WPF had started, so StopAsync is never called. WPF is still shut
                // down (Exit handlers, component disposal on the UI thread) before the host disposes the application's services.
                result.Event("FailingHostedServiceStarted");
                result.Event("Exit");
                result.Assert(result.Event("TrayDispose").Thread == uiThread, "The tray component was not disposed on the UI thread.");
                AssertCleanupBeforeServicesDisposed(result);
                break;
            case FailureKind.CanceledDuringInitialization:
                // The initialization deliberately outlives startup: it only continues once the host has begun disposing.
                // The application must never start, and the components created so far are disposed on the UI thread.
                // (Services used by that late cleanup may already be disposed: the initialization outran the stop timeout.)
                result.AssertInfo("HostDisposalObserved", "True");
                result.Assert(result.IndexOf("HostDisposalStarted") < result.IndexOf("BlockingInitializeFinished"), "Initialization finished before host disposal began; the intended interleaving was not established.");
                result.Assert(result.Events.All(e => e.Name is not ("Startup" or "WindowShown")), "The application started after the host was disposed.");
                result.Assert(result.Event("BlockingComponentDispose").Thread == uiThread, "The initialized components were not disposed on the UI thread.");
                break;
        }
    }

    private static async Task<ScenarioResult> RunAsync(Scenario scenario)
    {
        var executable = Path.Combine(AppContext.BaseDirectory, "Microsoft.Extensions.Hosting.Wpf.ScenarioApp.exe");
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory,
        };
        foreach (var argument in scenario.ToArguments())
        {
            startInfo.ArgumentList.Add(argument);
        }

        var output = new StringBuilder();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (output) output.AppendLine("stderr: " + e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeout = new CancellationTokenSource(ScenarioTimeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new Xunit.Sdk.XunitException($"Scenario {scenario} did not exit within {ScenarioTimeout} (the host or UI thread hung).{Environment.NewLine}{output}");
        }

        // Make sure all redirected output has been read.
        process.WaitForExit();
        lock (output)
        {
            return new ScenarioResult(scenario, process.ExitCode, output.ToString());
        }
    }

    private sealed record ScenarioEvent(string Name, string Thread, string Value);

    private sealed class ScenarioResult
    {
        private readonly Scenario _scenario;
        private readonly string _output;
        private readonly Dictionary<string, string> _info = new();

        public ScenarioResult(Scenario scenario, int exitCode, string output)
        {
            _scenario = scenario;
            _output = output;
            ExitCode = exitCode;
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.StartsWith(ScenarioLog.Prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var parts = line.Substring(ScenarioLog.Prefix.Length).Split('|');
                if (parts[0] == "EVENT")
                {
                    Events.Add(new ScenarioEvent(parts[1], parts[2], parts[3]));
                }
                else if (parts[0] == "INFO")
                {
                    _info[parts[1]] = parts[2];
                }
            }
        }

        public int ExitCode { get; }

        public List<ScenarioEvent> Events { get; } = new();

        public string Info(string key)
        {
            Assert(_info.TryGetValue(key, out var value), $"'{key}' was not reported.");
            return value!;
        }

        public void AssertInfo(string key, string expected) => Assert(Info(key) == expected, $"'{key}' was '{Info(key)}', expected '{expected}'.");

        public void AssertNoInfo(string key) => Assert(!_info.TryGetValue(key, out var value), $"Unexpected '{key}': {value}");

        public ScenarioEvent Event(string name)
        {
            var scenarioEvent = Events.FirstOrDefault(e => e.Name == name);
            Assert(scenarioEvent is not null, $"Event '{name}' did not happen.");
            return scenarioEvent!;
        }

        public int IndexOf(string name) => Events.IndexOf(Event(name));

        public void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Xunit.Sdk.XunitException($"{_scenario}: {message}{Environment.NewLine}Exit code: {ExitCode}{Environment.NewLine}{_output}");
            }
        }
    }
}
