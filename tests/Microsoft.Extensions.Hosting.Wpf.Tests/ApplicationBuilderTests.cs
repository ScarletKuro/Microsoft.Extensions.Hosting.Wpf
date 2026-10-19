using System.Collections.Concurrent;
using System.Reflection;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Hosting.Wpf.Threading;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

public class ApplicationBuilderTests
{
    [Fact]
    public void LifetimeSupportsGenericAndWebApplicationBuilders()
    {
        var genericBuilder = Host.CreateApplicationBuilder();
        var webBuilder = WebApplication.CreateBuilder();
#if NET8_0_OR_GREATER
        // WebApplicationBuilder implements IHostApplicationBuilder from ASP.NET Core 8.
        Assert.IsAssignableFrom<IHostApplicationBuilder>(webBuilder);
#endif

        // builder.Services is the common API of every builder, on every target.
        foreach (var services in new[] { genericBuilder.Services, webBuilder.Services })
        {
            services.AddWpf<TestApplication>();
            services.AddWpfInitialization<TestApplication>();
            Assert.Same(services, services.AddWpfLifetime(options => options.SuppressStatusMessages = true));
            Assert.Equal(typeof(WpfLifetime), services.Last(service => service.ServiceType == typeof(IHostLifetime)).ImplementationType);
        }

        using var genericHost = genericBuilder.Build();
        using var webHost = webBuilder.Build();
        foreach (var host in new IHost[] { genericHost, webHost })
        {
            Assert.IsType<WpfLifetime>(host.Services.GetRequiredService<IHostLifetime>());
            Assert.True(host.Services.GetRequiredService<IOptions<WpfLifetimeOptions>>().Value.SuppressStatusMessages);
            Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<TestApplication>());
        }
    }

    [Fact]
    public void LocatorFactoryIsDeferredWithCustomServiceProviderFactory()
    {
        var builder = Host.CreateApplicationBuilder();
        var calls = 0;
        var factory = new TrackingServiceProviderFactory();
        ((IHostApplicationBuilder)builder).ConfigureContainer(factory);
        Assert.Same(builder.Services, builder.Services.AddWpfViewModelLocator<TestApplication, Locator>(_ =>
        {
            calls++;
            return new Locator();
        }));
        builder.Services.AddWpf<TestApplication>();

        using var host = builder.Build();
        Assert.NotNull(host.Services.GetRequiredService<IWpfThread<TestApplication>>());
        Assert.Equal(0, calls);
        Assert.Equal(1, factory.Builds);
    }

    [Fact]
    public void InstanceLocatorAndInitializationReturnTheSameServices()
    {
        var locatorBuilder = Host.CreateApplicationBuilder();
        Assert.Same(locatorBuilder.Services, locatorBuilder.Services.AddWpfViewModelLocator<TestApplication, Locator>(new Locator()));
        locatorBuilder.Services.AddWpf<TestApplication>();
        using var locatorHost = locatorBuilder.Build();
        Assert.NotNull(locatorHost.Services.GetRequiredService<IWpfThread<TestApplication>>());

        var initializationBuilder = Host.CreateApplicationBuilder();
        Assert.Same(initializationBuilder.Services, initializationBuilder.Services.AddWpfInitialization<TestApplication>());
        initializationBuilder.Services.AddWpf<TestApplication>();
        using var initializationHost = initializationBuilder.Build();
        Assert.NotNull(initializationHost.Services.GetRequiredService<IWpfThread<TestApplication>>());
    }

    [Fact]
    public void DuplicateAndMixedInitializationAreRejected()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddWpf<TestApplication>();
        builder.Services.AddWpfInitialization<TestApplication>();

        Assert.Throws<InvalidOperationException>(() => builder.Services.AddWpfInitialization<TestApplication>());
        Assert.Throws<InvalidOperationException>(() => builder.Services.AddWpfViewModelLocator<TestApplication, Locator>(new Locator()));
        using var host = builder.Build();
        Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<TestApplication>());

        var locatorBuilder = Host.CreateApplicationBuilder();
        locatorBuilder.Services.AddWpfViewModelLocator<TestApplication, Locator>(_ => new Locator());
        Assert.Throws<InvalidOperationException>(() => locatorBuilder.Services.AddWpfInitialization<TestApplication>());
        Assert.Throws<InvalidOperationException>(() => locatorBuilder.Services.AddWpfViewModelLocator<TestApplication, Locator>(_ => new Locator()));
    }

    [Fact]
    public void NullArgumentsAreRejectedBeforeServicesAreRegistered()
    {
        IServiceCollection missing = null!;
        Assert.Throws<ArgumentNullException>(() => missing.AddWpfLifetime());
        Assert.Throws<ArgumentNullException>(() => missing.AddWpfLifetime(_ => { }));

        var builder = Host.CreateApplicationBuilder();
        var count = builder.Services.Count;
        Assert.Throws<ArgumentNullException>(() => builder.Services.AddWpfLifetime(null!));
        Assert.Throws<ArgumentNullException>(() => builder.Services.AddWpfViewModelLocator<TestApplication, Locator>((Locator)null!));
        Assert.Throws<ArgumentNullException>(() => builder.Services.AddWpfViewModelLocator<TestApplication, Locator>((Func<IServiceProvider, Locator>)null!));
        Assert.Equal(count, builder.Services.Count);
    }

    [Fact]
    public void LifetimeDoesNotRequireAddWpf()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddWpfLifetime();

        using var host = builder.Build();

        Assert.IsType<WpfLifetime>(host.Services.GetRequiredService<IHostLifetime>());
    }

    [Fact]
    public void ProcessExitKeepsTheApplicationExitCode()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddWpfLifetime(options => options.SuppressStatusMessages = true);
        using var host = builder.Build();
        var lifetime = Assert.IsType<WpfLifetime>(host.Services.GetRequiredService<IHostLifetime>());
        // Disposing releases the shutdown wait in the ProcessExit handler, which otherwise waits for the host to stop.
        lifetime.Dispose();
        // AppDomain.ProcessExit only fires when the process ends, so invoke the handler directly.
        var onProcessExit = typeof(WpfLifetime).GetMethod("OnProcessExit", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            // WPF sets this from Application.Shutdown(exitCode).
            Environment.ExitCode = 3;
            onProcessExit.Invoke(lifetime, new object?[] { null, EventArgs.Empty });
            Assert.Equal(3, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = 0;
        }
    }

    [Fact]
    public void OnlyOneWpfApplicationTypeCanBeAdded()
    {
        var services = new ServiceCollection();
        services.AddWpf<TestApplication>();
        var count = services.Count;

        // Same application again is allowed (TryAdd), a different one is rejected without registering anything.
        services.AddWpf<TestApplication>();
        var exception = Assert.Throws<InvalidOperationException>(() => services.AddWpf<OtherApplication>());
        Assert.Throws<InvalidOperationException>(() => services.AddWpf<OtherApplication>(_ => new OtherApplication()));

        Assert.Contains("AddWpf<TestApplication>()", exception.Message);
        Assert.Equal(count, services.Count);
    }

    [Fact]
    public void InitializationForAnotherApplicationIsRejectedInEitherOrder()
    {
        // AddWpf<A>() then AddWpfInitialization<B>(): previously A started before B's validation failed.
        var services = new ServiceCollection();
        services.AddWpf<TestApplication>();
        var count = services.Count;
        var exception = Assert.Throws<InvalidOperationException>(() => services.AddWpfInitialization<OtherApplication>());
        Assert.Contains("OtherApplication", exception.Message);
        Assert.Contains("AddWpf<TestApplication>()", exception.Message);
        Assert.Equal(count, services.Count);

        // AddWpfInitialization<B>() then AddWpf<A>().
        services = new ServiceCollection();
        services.AddWpfInitialization<OtherApplication>();
        count = services.Count;
        Assert.Throws<InvalidOperationException>(() => services.AddWpf<TestApplication>());
        Assert.Equal(count, services.Count);

        // IHostBuilder defers registration to Build(), which then fails before anything can start.
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(s => s.AddWpf<TestApplication>())
            .UseWpfInitialization<OtherApplication>();
        Assert.Throws<InvalidOperationException>(() => hostBuilder.Build());
    }

    [Fact]
    public void AddWpfRejectsNullArguments()
    {
        IServiceCollection missing = null!;
        Assert.Throws<ArgumentNullException>(() => missing.AddWpf<TestApplication>());
        Assert.Throws<ArgumentNullException>(() => missing.AddWpf<TestApplication>(_ => throw new InvalidOperationException()));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddWpf<TestApplication>(null!));
    }

    [Fact]
    public void ThreadSwitchingCanBeResolvedBeforeTheUiThreadStarts()
    {
        // The host creates all hosted services before starting any, so constructor injection of
        // JoinableTaskFactory happens before the WPF UI thread exists.
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddWpf<TestApplication>();
        builder.Services.AddThreadSwitching();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetRequiredService<JoinableTaskFactory>());
        var context = host.Services.GetRequiredService<JoinableTaskContext>();
        Assert.Same(host.Services.GetRequiredService<IWpfThread<TestApplication>>().MainThread, context.MainThread);
    }

    [Fact]
    public void LegacyHostingExtensionsRemainAvailable()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<TestApplication>())
            .UseWpfLifetime(options => options.SuppressStatusMessages = true)
            .Build()
            .UseWpfViewModelLocator<TestApplication, Locator>(new Locator());

        Assert.IsType<WpfLifetime>(host.Services.GetRequiredService<IHostLifetime>());
        Assert.True(host.Services.GetRequiredService<IOptions<WpfLifetimeOptions>>().Value.SuppressStatusMessages);
    }

    [Fact]
    public void HostBuilderInitializationIsRegisteredBeforeBuild()
    {
        var calls = 0;
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<TestApplication>())
            .UseWpfViewModelLocator<TestApplication, Locator>(_ =>
            {
                calls++;
                return new Locator();
            })
            .Build();

        Assert.NotNull(host.Services.GetRequiredService<IWpfThread<TestApplication>>());
        Assert.Equal(0, calls);
        Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<TestApplication>());
    }

    [Fact]
    public void HostBuilderDuplicateInitializationIsRejectedOnBuild()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<TestApplication>())
            .UseWpfInitialization<TestApplication>()
            .UseWpfViewModelLocator<TestApplication, Locator>(new Locator());

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void ServiceCollectionDuplicateInitializationIsRejected()
    {
        var services = new ServiceCollection();
        services.AddWpfInitialization<TestApplication>();

        Assert.Throws<InvalidOperationException>(() => services.AddWpfInitialization<TestApplication>());
        Assert.Throws<InvalidOperationException>(() => services.AddWpfViewModelLocator<TestApplication, Locator>(new Locator()));
        Assert.Throws<InvalidOperationException>(() => services.AddWpfViewModelLocator<TestApplication, Locator>(_ => new Locator()));
    }

    [Fact]
    public async Task InitializationWithoutAddWpfFailsOnStartBeforeAnyHostedServiceStarts()
    {
        var builder = Host.CreateApplicationBuilder();
        var started = new StartRecorder();
        builder.Services.AddSingleton(started);
        // Registered before the invalid initialization: validation must still fail first.
        builder.Services.AddHostedService<RecordingHostedService>();
        builder.Services.AddWpfInitialization<TestApplication>();
        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
        Assert.Contains("AddWpf<TestApplication>()", exception.Message);
        Assert.False(started.Started);
    }

    public sealed class StartRecorder
    {
        public bool Started { get; set; }
    }

    private sealed class RecordingHostedService(StartRecorder recorder) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            recorder.Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public void NullArgumentsAreRejectedConsistently()
    {
        IServiceCollection missingServices = null!;
        IHostBuilder missingHostBuilder = null!;
        Assert.Throws<ArgumentNullException>(() => missingServices.AddWpfInitialization<TestApplication>());
        Assert.Throws<ArgumentNullException>(() => missingServices.AddWpfViewModelLocator<TestApplication, Locator>(new Locator()));
        Assert.Throws<ArgumentNullException>(() => missingServices.AddWpfViewModelLocator<TestApplication, Locator>(_ => new Locator()));
        Assert.Throws<ArgumentNullException>(() => missingHostBuilder.UseWpfInitialization<TestApplication>());
        Assert.Throws<ArgumentNullException>(() => missingHostBuilder.UseWpfViewModelLocator<TestApplication, Locator>(new Locator()));
        Assert.Throws<ArgumentNullException>(() => missingHostBuilder.UseWpfViewModelLocator<TestApplication, Locator>(_ => new Locator()));

        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddWpfViewModelLocator<TestApplication, Locator>((Locator)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddWpfViewModelLocator<TestApplication, Locator>((Func<IServiceProvider, Locator>)null!));
        Assert.Empty(services);

        var hostBuilder = Host.CreateDefaultBuilder();
        Assert.Throws<ArgumentNullException>(() => hostBuilder.UseWpfLifetime(null!));
        Assert.Throws<ArgumentNullException>(() => hostBuilder.UseWpfViewModelLocator<TestApplication, Locator>((Locator)null!));
        Assert.Throws<ArgumentNullException>(() => hostBuilder.UseWpfViewModelLocator<TestApplication, Locator>((Func<IServiceProvider, Locator>)null!));

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(s => s.AddWpf<TestApplication>())
            .Build();
        Assert.Throws<ArgumentNullException>(() => host.UseWpfViewModelLocator<TestApplication, Locator>((Locator)null!));
        Assert.Throws<ArgumentNullException>(() => host.UseWpfViewModelLocator<TestApplication, Locator>((Func<IServiceProvider, Locator>)null!));
    }

    public sealed class Locator;

    // Never instantiated: WPF allows only one Application per AppDomain.
    public sealed class OtherApplication : Application, IApplicationInitializeComponent, IApplicationInitialize
    {
        public void InitializeComponent()
        {
        }

        public void Initialize()
        {
        }
    }

    public sealed class ApplicationState
    {
        public ConcurrentQueue<string> Events { get; } = new();
        public ConcurrentQueue<int> Threads { get; } = new();
        public TaskCompletionSource<TestApplication> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(string name)
        {
            Events.Enqueue(name);
            Threads.Enqueue(Environment.CurrentManagedThreadId);
        }
    }

    public sealed class TestApplication : Application, IApplicationInitializeComponent, IViewModelLocatorInitialization<Locator>
    {
        private readonly ApplicationState _state;

        public TestApplication(ApplicationState state)
        {
            _state = state;
            _state.Record("Constructor");
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        public Locator? Locator { get; private set; }

        public void InitializeComponent() => _state.Record("InitializeComponent");
        public void Initialize() => _state.Record("Initialize");

        public void InitializeLocator(Locator viewModelLocator)
        {
            _state.Record("InitializeLocator");
            Locator = viewModelLocator;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _state.Record("Startup");
            _state.Started.SetResult(this);
        }
    }


    private sealed class TrackingServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public int Builds { get; private set; }
        public IServiceCollection CreateBuilder(IServiceCollection services) => services;
        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            Builds++;
            return containerBuilder.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }
    }
}
