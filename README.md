# Microsoft.Extensions.Hosting.Wpf
Unofficial implementation of Microsoft.Extensions.Hosting for WPF. It is inspired by [Dapplo](https://github.com/dapplo/Dapplo.Microsoft.Extensions.Hosting) and this extensions is focused only on WPF and doesn't have Plugins, SingleInstance etc features like Dapplo. It's main feature is to provide the ability to bind DataContext with ViewModels directly in XAML where the ViewModel gets resolved by DI. The second feature is the ability to use TrayIcon with this library because with Microsoft.Extensions.Hostin it's tricky.

This is more or less how I see Microsoft would do it for WPF without changing the WPF codedbase.

## Samples
1. [HostingSimple](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/master/samples/HostingSimple) - Minimal getting started project.
2. [HostingReactiveUI](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/master/samples/HostingReactiveUI) - More advanced example with using NLog as logging, ReactiveUI as model-view-viewmodel framework, shows how to use the TrayIcon feature.
3. [HostingReactiveUISimpleInjector](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/master/samples/HostingReactiveUISimpleInjector) - Same as HostingReactiveUI but it also using SimpleInjector. This library doesn't limits your to stick only with `Microsoft.DependencyInjection`. Also shows some more abstractions and internal helpers to handle another DI inside.

## Getting Started
This steps including the Locator feature for Views. If you don't want it then just skip to 6 and 7 step.
### 1. First step, make `IViewModelLocator` that will contain your ViewModels. Example:
```CSharp
public interface IViewModelLocator
{
    MainViewModel Main { get; }

    ChildViewModel Child { get; }
}
```
### 2. Make implementation for `IViewModelLocator`. This example is using `IServiceProvider`
```CSharp
public class ViewModelLocator : IViewModelLocator
{
    public IServiceProvider Container { get; }

    public MainViewModel Main => Container.GetRequiredService<MainViewModel>();

    public ChildViewModel Child => Container.GetRequiredService<ChildViewModel>();

    public ViewModelLocator(IServiceProvider container)
    {
        Container = container;
    }
}
```
### 3. Add `ViewModelLocatorHost` that inherits `AbstractViewModelLocatorHost<IViewModelLocator>`
```CSharp
public class ViewModelLocatorHost : AbstractViewModelLocatorHost<IViewModelLocator>
{
}
```
### 4. In your App.xaml add Locator resource
```XML
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!--Resources that you need, for example MahApps-->
        </ResourceDictionary.MergedDictionaries>
        <locator:ViewModelLocatorHost x:Key="Locator"/>
    </ResourceDictionary>
</Application.Resources>
```
### 5. Add in App.xaml.cs two interfaces `IViewModelLocatorInitialization<ViewModelLocator>` and `IApplicationInitializeComponent`
```CSharp
public partial class App : Application, IViewModelLocatorInitialization<ViewModelLocator>, IApplicationInitializeComponent
{
    public void Initialize(ViewModelLocator viewModelLocator)
    {
        //We need to set it so that our <locator:ViewModelLocatorHost x:Key="Locator"/> could resolve ViewModels for DataContext
        //You can also use it as service locator pattern, but I personally recommend you to use it only inside View xaml to bind the DataContext
        var viewModelLocatorHost = ViewModelLocatorHost.GetInstance(this);
        viewModelLocatorHost?.SetViewModelLocator(viewModelLocator);
    }
}
```
### 6. Add Program.cs. Basic example
```CSharp
public class Program
{
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	public static void Main(string[] args)
	{
		using IHost host = CreateHostBuilder(args)
			.Build()
			.UseWpfViewModelLocator<App, ViewModelLocator>(provider => new ViewModelLocator(provider));
		host.Run();
	}

	private static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.ConfigureServices(ConfigureServices);
	}

	private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
	{
		services.AddLogging();
		services.AddWpf<App>();

		//Add our view models
		services.AddTransient<MainViewModel>();
		services.AddTransient<ChildViewModel>();
	}
}
```
**NB!** AddWpf has an overload and you can for example add additional services to App
```CSharp
services.AddWpf(serviceProvider =>
{
	var logger = serviceProvider.GetRequiredService<ILogger<App>>();

	return new App(logger);
});
```
### 7. We need to add `StartupObject` in our `.csproj`
```Xml
<StartupObject>[Namespace].Program</StartupObject>
```
### 8. Now in your View you can bind the DataContext like this
```Xml
DataContext="{Binding ViewModelLocator.Main, Mode=OneTime, Source={StaticResource Locator}}"
```
