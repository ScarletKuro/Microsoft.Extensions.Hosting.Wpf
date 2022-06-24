# Microsoft.Extensions.Hosting.Wpf
[![Nuget](https://img.shields.io/nuget/v/Extensions.Hosting.Wpf?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Extensions.Hosting.Wpf/)
[![Nuget](https://img.shields.io/nuget/dt/Extensions.Hosting.Wpf?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Extensions.Hosting.Wpf/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Microsoft.Extensions.Hosting.Wpf?color=594ae2&logo=github)](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/blob/main/LICENSE)

Unofficial implementation of Microsoft.Extensions.Hosting for WPF. It is inspired by [Dapplo](https://github.com/dapplo/Dapplo.Microsoft.Extensions.Hosting) and this extensions is focused only on WPF and doesn't have Plugins, SingleInstance etc features like Dapplo. It's main feature is to provide the ability to bind DataContext with ViewModels directly in XAML where the ViewModel gets resolved by DI. The second feature is the ability to use TrayIcon with this library because with Microsoft.Extensions.Hosting it's tricky.

### [Changelog](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/blob/main/CHANGELOG.md)

## Samples
1. [HostingSimple](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingSimple) - Minimal getting started project.
2. [HostingReactiveUI](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingReactiveUI) - More advanced example with using NLog as logging, ReactiveUI as model-view-viewmodel framework, shows how to use the TrayIcon feature.
3. [HostingReactiveUISimpleInjector](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingReactiveUISimpleInjector) - Same as HostingReactiveUI but it also using SimpleInjector. This library doesn't limits your to stick only with `Microsoft.DependencyInjection`. Also shows some more abstractions and internal helpers to handle another DI inside.
4. [HostingReactiveUISimpleInjectorFlowingScope](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples) - Almost like HostingReactiveUISimpleInjector, with the difference that it shows, how can you use Flowing-Scoping(`ScopedLifestyle.Flowing` with `SimpleInjector` or any other DI with similar scoping like MS.DI). This is even more advanced example and shows how can you support closure-scoping ([The Closure Composition Model](https://blogs.cuttingedge.it/steven/posts/2019/closure-composition-model/)), for ambient-scoping ([The Ambient Composition Model](https://blogs.cuttingedge.it/steven/posts/2019/ambient-composition-model/)) there is usually no need for any special changes. This sample doesn't use the `ViewModelLocatorHost` and shows a totally different approach on how to use `IViewModelLocator` and inject viewmodel for view. It also shows the usage of `Microsoft.Extensions.Hosting.Wpf`.

## Getting Started
This steps including the Locator feature for Views. If you don't want it then just skip to 6 and 7 step.
In fact, it has alternative method to inject ViewModels to View. Usually used when you need to use closure-scoping with DI. Please, refer to `HostingReactiveUISimpleInjectorFlowingScope` sample.
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
public partial class App : Application, IViewModelLocatorInitialization<IViewModelLocator>, IApplicationInitializeComponent
{
    public void Initialize()
    {
        //Here we can initialize important things. This method always runs on UI thread. 
        //In this example it's empty as we do not have anything to initialize like ReactiveUI
    }
	
    public void InitializeLocator(IViewModelLocator viewModelLocator)
    {
        //Runs after Initialize method.
        //We need to set it so that our <locator:ViewModelLocatorHost x:Key="Locator"/> could resolve ViewModels for DataContext
        //You can also use it as service locator pattern, but I personally recommend you to use it only inside View xaml to bind the DataContext
        var viewModelLocatorHost = ViewModelLocatorHost.GetInstance(this);
        viewModelLocatorHost?.SetViewModelLocator(viewModelLocator);
    }
}
```
**NB!** `ViewModelLocatorHost.GetInstance(this)` will automatically find the locator even if you rename it(x:Key), but for better perfomance, startup time, memory usage(it will iterate through Application Dictionary) my personal recommendation is to use `ViewModelLocatorHost.GetInstance(this, "Locator")` instead.
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
			.UseWpfViewModelLocator<App, IViewModelLocator>(provider => new ViewModelLocator(provider));
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

### 7. We need to add `StartupObject` in our `.csproj`
```Xml
<StartupObject>[Namespace].Program</StartupObject>
```
### 8. Now in your View you can bind the DataContext like this
```Xml
DataContext="{Binding ViewModelLocator.Main, Mode=OneTime, Source={StaticResource Locator}}"
```

## WPF Lifetime
If you want you can use `UseWpfLifetime` but it's pretty much experimental, the current solution is well adopted to use the default lifetime and was battle tested without `UseWpfLifetime`.
### Usage
```CSharp
private static IHostBuilder CreateHostBuilder(string[] args)
{
	return Host.CreateDefaultBuilder(args)
		.UseWpfLifetime() //<-- new line
		.ConfigureServices(ConfigureServices);
}
```


## Other features
1. [Microsoft.Extensions.Hosting.Wpf.Threading](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/blob/main/docs/Threading.md)
2. [Microsoft.Extensions.Hosting.Wpf.Bootstrap](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/blob/main/docs/Bootstrap.md)
3. [Microsoft.Extensions.Hosting.Wpf.TrayIcon](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/blob/main/docs/TrayIcon.md)
