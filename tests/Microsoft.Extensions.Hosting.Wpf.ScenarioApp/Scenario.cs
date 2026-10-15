namespace Microsoft.Extensions.Hosting.Wpf.ScenarioApp;

/// <summary>How the host is built.</summary>
public enum BuilderKind
{
    /// <summary><c>Host.CreateDefaultBuilder()</c> (<c>IHostBuilder</c>).</summary>
    HostBuilder,
    /// <summary><c>Host.CreateApplicationBuilder()</c> (<c>IHostApplicationBuilder</c>).</summary>
    ApplicationBuilder,
    /// <summary><c>WebApplication.CreateBuilder()</c> (<c>IHostApplicationBuilder</c>).</summary>
    WebApplicationBuilder,
}

/// <summary>Which <c>IHostLifetime</c> is used.</summary>
public enum LifetimeKind
{
    /// <summary>The builder's default (<c>ConsoleLifetime</c>).</summary>
    Default,
    /// <summary><c>WpfLifetime</c>.</summary>
    Wpf,
}

/// <summary>How application initialization is configured.</summary>
public enum InitializationKind
{
    /// <summary>No initialization.</summary>
    None,
    /// <summary><c>IApplicationInitialize</c>, configured before <c>Build()</c>.</summary>
    Initialization,
    /// <summary><c>IApplicationInitialize</c>, configured with <c>host.UseWpfInitialization</c> after <c>Build()</c>.</summary>
    InitializationPostBuild,
    /// <summary>View model locator factory, configured before <c>Build()</c>.</summary>
    Locator,
    /// <summary>View model locator factory, configured with <c>host.UseWpfViewModelLocator</c> after <c>Build()</c>.</summary>
    LocatorPostBuild,
    /// <summary>View model locator instance, configured before <c>Build()</c>.</summary>
    LocatorInstance,
    /// <summary>View model locator instance, configured with <c>host.UseWpfViewModelLocator</c> after <c>Build()</c>.</summary>
    LocatorInstancePostBuild,
}

/// <summary>What fails during host startup, if anything.</summary>
public enum FailureKind
{
    /// <summary>Nothing fails.</summary>
    None,
    /// <summary>The WPF application factory throws, before the application exists.</summary>
    ApplicationFactory,
    /// <summary>A hosted service registered after <c>AddWpf</c> throws in <c>StartAsync</c>; WPF starts only after all have started.</summary>
    LaterHostedService,
    /// <summary>A hosted service registered after <c>AddWpf</c> throws in <c>StartedAsync</c>, after WPF has started.</summary>
    LaterHostedServiceStarted,
    /// <summary>The application's <c>OnStartup</c> throws.</summary>
    ApplicationStartup,
    /// <summary>Startup is cancelled while the UI thread is still initializing; the host is disposed before initialization ends.</summary>
    CanceledDuringInitialization,
}

/// <summary>What ends the application once it runs.</summary>
public enum StopKind
{
    /// <summary>WPF exits on its own: <c>Application.Shutdown(exitCode)</c>.</summary>
    WpfExit,
    /// <summary>The host stops first: <c>IHostApplicationLifetime.StopApplication()</c>, e.g. Ctrl+C.</summary>
    HostStop,
    /// <summary>A tray icon menu calls <c>IWpfThread.HandleApplicationExit()</c> on the UI thread.</summary>
    TrayExit,
    /// <summary>The user closes the last window (<c>ShutdownMode.OnLastWindowClose</c>).</summary>
    LastWindowClosed,
}

/// <summary>One combination run by the scenario app.</summary>
/// <param name="CustomFactory">Creates the application with <c>AddWpf(factory)</c>, which queues dispatcher work before constructing it.</param>
public sealed record Scenario(BuilderKind Builder, LifetimeKind Lifetime, InitializationKind Initialization, StopKind Stop, FailureKind Failure = FailureKind.None, bool CustomFactory = false)
{
    /// <summary>Exit code passed to <c>Application.Shutdown</c> in <see cref="StopKind.WpfExit"/>.</summary>
    public const int ApplicationExitCode = 3;

    /// <summary>Process exit code when <c>host.Run()</c> throws.</summary>
    public const int HostFailedExitCode = 90;

    /// <summary>Message of the exception thrown for <see cref="Failure"/>.</summary>
    public const string StartupFailureMessage = "Scenario startup failure";

    /// <summary>Formats the scenario as command line arguments.</summary>
    public string[] ToArguments() =>
        new[] { Builder.ToString(), Lifetime.ToString(), Initialization.ToString(), Stop.ToString(), Failure.ToString(), CustomFactory ? "CustomFactory" : "DefaultFactory" };

    /// <summary>Parses the arguments produced by <see cref="ToArguments"/>.</summary>
    public static Scenario Parse(string[] arguments)
    {
        return new Scenario(
            Enum.Parse<BuilderKind>(arguments[0]),
            Enum.Parse<LifetimeKind>(arguments[1]),
            Enum.Parse<InitializationKind>(arguments[2]),
            Enum.Parse<StopKind>(arguments[3]),
            Enum.Parse<FailureKind>(arguments[4]),
            arguments.Length > 5 && arguments[5] == "CustomFactory");
    }
}
