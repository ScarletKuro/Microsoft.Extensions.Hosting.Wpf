using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Bootstrap;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

public sealed class BootstrapTests
{
    [Fact]
    public void EveryBootstrapBootsTheContainer()
    {
        var builder = Host.CreateApplicationBuilder();
        Assert.Same(builder.Services, builder.Services.AddBootstrap<Container, FirstBootstrap>());
        builder.Services.AddBootstrap<Container, SecondBootstrap>();
        // Bootstraps of another container type are not run.
        builder.Services.AddBootstrap<OtherContainer, OtherBootstrap>();
        using var host = builder.Build();
        var container = new Container();

        Assert.Same(host, host.UseWpfContainerBootstrap(container));

        Assert.Equal(new[] { nameof(FirstBootstrap), nameof(SecondBootstrap) }, container.Booted);
        Assert.Contains(typeof(BootstrapTests).Assembly, container.Assemblies);
    }

    [Fact]
    public void NullArgumentsAreRejected()
    {
        Assert.Throws<ArgumentNullException>("services", () => ((IServiceCollection)null!).AddBootstrap<Container, FirstBootstrap>());

        using var host = Host.CreateApplicationBuilder().Build();
        Assert.Throws<ArgumentNullException>("host", () => ((IHost)null!).UseWpfContainerBootstrap(new Container()));
        Assert.Throws<ArgumentNullException>("container", () => host.UseWpfContainerBootstrap<Container>(null!));
    }

    public sealed class Container
    {
        public List<string> Booted { get; } = new();
        public Assembly[] Assemblies { get; set; } = Array.Empty<Assembly>();
    }

    public sealed class OtherContainer;

    public sealed class FirstBootstrap : IBootstrap<Container>
    {
        public void Boot(Container container, Assembly[] assemblies)
        {
            container.Booted.Add(nameof(FirstBootstrap));
            container.Assemblies = assemblies;
        }
    }

    public sealed class SecondBootstrap : IBootstrap<Container>
    {
        public void Boot(Container container, Assembly[] assemblies) => container.Booted.Add(nameof(SecondBootstrap));
    }

    public sealed class OtherBootstrap : IBootstrap<OtherContainer>
    {
        public void Boot(OtherContainer container, Assembly[] assemblies) => throw new InvalidOperationException("Another container's bootstrap ran.");
    }
}
