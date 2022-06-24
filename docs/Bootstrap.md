# Microsoft.Extensions.Hosting.Wpf.Bootstrap
Extension library for `Microsoft.Extensions.Hosting.Wpf`. Helps to bootstrap 3rd party Dependency Injections.
Not necessary to use, you can register your 3rd DI dependencies also in ConfigureServices just like with Microsoft Dependency Injection, it has more of decorative role.

## Getting Started

### Install nuget
```Install-Package Extensions.Hosting.Bootstrap```

### Example usage with SimpleInjector
```CSharp
public class RootBoot : IBootstrap<Container>
{
	private readonly ILogger<RootBoot> _logger;

	public RootBoot(ILogger<RootBoot> logger)
	{
		_logger = logger;
	}

	public void Boot(Container container, Assembly[] assemblies)
	{
		_logger.LogInformation("SimpleInjector registration.");
		container.Register<MainViewModel>(Lifestyle.Transient); //<-- register things to SimpleInjector
	}

	public static Container CreateContainer()
	{
		return new Container
		{
			Options =
			{
				EnableAutoVerification = false,
				DefaultScopedLifestyle = new AsyncScopedLifestyle()
			}
		};
	}
}
```

```CSharp
public static void Main(string[] args)
{
	using var container = RootBoot.CreateContainer(); // <-- new line
	using IHost host = CreateHostBuilder(container, args)
		.Build()
		.UseSimpleInjector(container) // <-- new line(specific to SimpleInjector)
		.UseWpfContainerBootstrap(container) // <-- new line
		.UseWpfViewModelLocator<App, ViewModelLocator>(new ViewModelLocator(container));
	host.Run();
}

private static IHostBuilder CreateHostBuilder(Container container, string[] args)
{
	return Host.CreateDefaultBuilder(args)
		.ConfigureServices((context, collection) => ConfigureServices(context, collection, container));
}

private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services, Container container)
{
	services.AddSimpleInjector(container, options =>{}); //<-- new line(specific to SimpleInjector)
	services.AddBootstrap<Container, RootBoot>(); // <-- new line
	services.AddWpf<App>();
}
```
