using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

// Post-build IHost extensions. The real UI startup lives in ApplicationBuilderTests:
// WPF allows only one Application per AppDomain.
public class HostingTests
{
    [Fact]
    public void DuplicateAndMixedInitializationAreRejected()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<ApplicationBuilderTests.TestApplication>())
            .Build()
            .UseWpfInitialization<ApplicationBuilderTests.TestApplication>();

        Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<ApplicationBuilderTests.TestApplication>());
        Assert.Throws<InvalidOperationException>(() => host.UseWpfViewModelLocator<ApplicationBuilderTests.TestApplication, ApplicationBuilderTests.Locator>(new ApplicationBuilderTests.Locator()));
        Assert.Throws<InvalidOperationException>(() => host.UseWpfViewModelLocator<ApplicationBuilderTests.TestApplication, ApplicationBuilderTests.Locator>(_ => new ApplicationBuilderTests.Locator()));
    }

    [Fact]
    public void InitializationWithoutAddWpfIsRejected()
    {
        using var host = Host.CreateDefaultBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<ApplicationBuilderTests.TestApplication>());
    }

    [Fact]
    public async Task InitializationAfterStartIsRejected()
    {
        // The failing factory starts the UI thread without creating a WPF Application (one per AppDomain).
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<ApplicationBuilderTests.TestApplication>(_ => throw new InvalidOperationException("Not created.")))
            .Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync().WaitAsync(TimeSpan.FromSeconds(15)));

        var exception = Assert.Throws<InvalidOperationException>(() => host.UseWpfInitialization<ApplicationBuilderTests.TestApplication>());
        Assert.Contains("before the host is started", exception.Message);
    }

    [Fact]
    public async Task ApplicationStartupFailureFailsHostStart()
    {
        var failure = new InvalidOperationException("Application failed to start.");
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddWpf<ApplicationBuilderTests.TestApplication>(_ => throw failure))
            .UseWpfLifetime(options => options.SuppressStatusMessages = true)
            .Build();
        var thread = host.Services.GetRequiredService<IWpfThread<ApplicationBuilderTests.TestApplication>>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync().WaitAsync(TimeSpan.FromSeconds(15)));

        Assert.Same(failure, exception);
        Assert.True(thread.MainThread.Join(TimeSpan.FromSeconds(5)));
        Assert.False(host.Services.GetRequiredService<IWpfContext<ApplicationBuilderTests.TestApplication>>().IsRunning);
    }
}
