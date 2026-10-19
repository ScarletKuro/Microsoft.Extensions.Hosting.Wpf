using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

// The tray icon's lifecycle on the UI thread (InitializeComponent, disposal before OnExit) is covered by ScenarioTests.
public sealed class TrayIconTests
{
    [Fact]
    public void TrayIconIsCreatedByContainerButNotOwnedByIt()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Dependency>();
        Assert.Same(services, services.AddWpfTrayIcon<TestTrayIcon>());

        TestTrayIcon trayIcon;
        using (var provider = services.BuildServiceProvider())
        {
            var factory = Assert.Single(provider.GetServices<Func<IWpfComponent>>());
            trayIcon = Assert.IsType<TestTrayIcon>(factory());
            Assert.Same(provider.GetRequiredService<Dependency>(), trayIcon.Dependency);
            Assert.NotSame(trayIcon, factory());
        }

        // The library disposes components on the UI thread, so the container must not dispose them.
        Assert.False(trayIcon.Disposed);
    }

    [Fact]
    public void TrayIconFactoryReceivesServiceProvider()
    {
        IServiceProvider? factoryProvider = null;
        var services = new ServiceCollection();
        services.AddSingleton<Dependency>();
        Assert.Same(services, services.AddWpfTrayIcon(provider =>
        {
            factoryProvider = provider;
            return new TestTrayIcon(provider.GetRequiredService<Dependency>());
        }));

        TestTrayIcon trayIcon;
        using (var provider = services.BuildServiceProvider())
        {
            var factory = Assert.Single(provider.GetServices<Func<IWpfComponent>>());
            Assert.Null(factoryProvider);
            trayIcon = Assert.IsType<TestTrayIcon>(factory());
            Assert.NotNull(factoryProvider);
            Assert.Same(provider.GetRequiredService<Dependency>(), trayIcon.Dependency);
        }

        Assert.False(trayIcon.Disposed);
    }

    [Fact]
    public void NullArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>("services", () => ((IServiceCollection)null!).AddWpfTrayIcon<TestTrayIcon>());
        Assert.Throws<ArgumentNullException>("services", () => ((IServiceCollection)null!).AddWpfTrayIcon(_ => new TestTrayIcon(new Dependency())));
        Assert.Throws<ArgumentNullException>("createTray", () => new ServiceCollection().AddWpfTrayIcon<TestTrayIcon>(null!));
    }

    public sealed class Dependency;

    public sealed class TestTrayIcon : ITrayIcon
    {
        public TestTrayIcon(Dependency dependency)
        {
            Dependency = dependency;
        }

        public Dependency Dependency { get; }

        public bool Disposed { get; private set; }

        public void InitializeComponent()
        {
        }

        public void Dispose() => Disposed = true;
    }
}
