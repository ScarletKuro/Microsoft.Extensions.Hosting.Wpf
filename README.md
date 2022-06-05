# Coming soon...

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
7. We need to add `StartupObject` in our `.csproj`
```Xml
<StartupObject>[Namespace].Program</StartupObject>
```
