using System.Collections.Concurrent;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

public class HostingTests
{
    [Fact]
    public void LifetimeIsRegisteredWithOptions()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<TestApplication>())
            .UseWpfLifetime(options => options.SuppressStatusMessages = true)
            .Build();

        Assert.IsType<WpfLifetime>(host.Services.GetRequiredService<IHostLifetime>());
        Assert.True(host.Services.GetRequiredService<IOptions<WpfLifeTimeOptions>>().Value.SuppressStatusMessages);
    }

    [Fact]
    public void DuplicateAndMixedInitializationAreRejected()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<TestApplication>())
            .Build()
            .UseWpfInitialization<TestApplication>();

        Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<TestApplication>());
        Assert.Throws<InvalidOperationException>(() => host.UseWpfViewModelLocator<TestApplication, Locator>(new Locator()));
        Assert.Throws<InvalidOperationException>(() => host.UseWpfViewModelLocator<TestApplication, Locator>(_ => new Locator()));
    }

    [Fact]
    public void InitializationWithoutAddWpfIsRejected()
    {
        using var host = Host.CreateDefaultBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<TestApplication>());
    }

    // WPF allows only one Application per AppDomain. Keep all real UI startup in this test.
    // Regression: WPF shutting down on its own (Exit -> StopApplication) raced with WpfHostedService.StopAsync,
    // which then awaited an aborted dispatcher operation and failed with TaskCanceledException.
    [Fact]
    public async Task HostInitializesOnStaThreadAndStopsCleanlyWhenWpfExits()
    {
        var state = new ApplicationState();
        var locator = new Locator();
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(state);
                services.AddWpf<TestApplication>();
                services.AddHostedService<AwaitApplicationStartup>();
            })
            .UseWpfLifetime(options => options.SuppressStatusMessages = true)
            .Build()
            .UseWpfViewModelLocator<TestApplication, Locator>(_ =>
            {
                state.Record("Factory");
                return locator;
            });

        var thread = host.Services.GetRequiredService<IWpfThread<TestApplication>>();
        var context = host.Services.GetRequiredService<IWpfContext<TestApplication>>();
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        try
        {
            await host.StartAsync().WaitAsync(TimeSpan.FromSeconds(15));
            var application = await state.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Same(locator, application.Locator);
            Assert.Equal(new[] { "Constructor", "InitializeComponent", "Initialize", "Factory", "InitializeLocator", "Startup" }, state.Events.ToArray());
            Assert.All(state.Threads, id => Assert.Equal(thread.MainThread.ManagedThreadId, id));
            Assert.Equal(ApartmentState.STA, thread.MainThread.GetApartmentState());
            Assert.True(context.IsRunning);

            await application.Dispatcher.InvokeAsync(() => application.Shutdown());
            await Task.Delay(Timeout.Infinite, lifetime.ApplicationStopping).ContinueWith(_ => { }, TaskScheduler.Default).WaitAsync(TimeSpan.FromSeconds(15));
            Assert.True(lifetime.ApplicationStopping.IsCancellationRequested);
        }
        finally
        {
            await host.StopAsync().WaitAsync(TimeSpan.FromSeconds(15));
            Assert.True(thread.MainThread.Join(TimeSpan.FromSeconds(5)));
        }

        Assert.False(context.IsRunning);
    }

    public sealed class Locator;

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

    public sealed class TestApplication : Application, IApplicationInitializeComponent, IApplicationInitialize, IViewModelLocatorInitialization<Locator>
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

    private sealed class AwaitApplicationStartup(ApplicationState state) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken) => await state.Started.Task.WaitAsync(cancellationToken);
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
