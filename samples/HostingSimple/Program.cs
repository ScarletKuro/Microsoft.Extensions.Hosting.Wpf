using HostingSimple;
using HostingSimple.Locator;
using HostingSimple.Service;
using HostingSimple.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Wpf;

// Top-level statements work with App.xaml because WPF's generated Main is turned off:
// see EnableDefaultApplicationDefinition in HostingSimple.csproj.
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLogging();
builder.Services.AddWpf<App>();
builder.Services.AddWpfLifetime();
builder.Services.AddWpfViewModelLocator<App, IViewModelLocator>(provider => new ViewModelLocator(provider));

//Add our view models
builder.Services.AddTransient<MainViewModel>();
builder.Services.AddTransient<ChildViewModel>();

//Add our custom services for wpf
builder.Services.AddSingleton<WindowService>();

using var host = builder.Build();
host.Run();
