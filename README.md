# Microsoft.Extensions.Hosting.Wpf
[![Nuget](https://img.shields.io/nuget/v/Extensions.Hosting.Wpf?color=ff4081&logo=nuget)](https://www.nuget.org/packages/Extensions.Hosting.Wpf/)
[![Nuget](https://img.shields.io/nuget/dt/Extensions.Hosting.Wpf?color=ff4081&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/Extensions.Hosting.Wpf/)
[![GitHub](https://img.shields.io/github/license/ScarletKuro/Microsoft.Extensions.Hosting.Wpf?color=594ae2&logo=github)](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/blob/main/LICENSE)

Unofficial implementation of Microsoft.Extensions.Hosting for WPF. It is inspired by [Dapplo](https://github.com/dapplo/Dapplo.Microsoft.Extensions.Hosting) and this extensions is focused only on WPF and doesn't have Plugins, SingleInstance etc features like Dapplo. It's main feature is to provide the ability to bind DataContext with ViewModels directly in XAML where the ViewModel gets resolved by DI.
This library also has few extensions packages to add features like tray icon, thread switching between main thread and threadpool thread, 3rd party DI support.

### [Changelog](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/blob/main/CHANGELOG.md) | [Wiki](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki)

## NuGet Packages

| Name | Description | Latest version |
| --- | --- | --- |
| **Extensions.Hosting.Wpf** | The main library, with **Microsoft.Extensions.Hosting** for WPF support. | [![NuGet](https://img.shields.io/nuget/vpre/Extensions.Hosting.Wpf.svg)](https://www.nuget.org/packages/Extensions.Hosting.Wpf/) |
| **Extensions.Hosting.Wpf.TrayIcon** | An extension enabling tray icon support. | [![NuGet](https://img.shields.io/nuget/vpre/Extensions.Hosting.Wpf.TrayIcon.svg)](https://www.nuget.org/packages/Extensions.Hosting.Wpf.TrayIcon/) |
| **Extensions.Hosting.Wpf.Threading** | An extension enabling thread switching between Main UI Thread and ThreadPool Thread via **Microsoft.VisualStudio.Threading**. | [![NuGet](https://img.shields.io/nuget/vpre/Extensions.Hosting.Wpf.Threading.svg)](https://www.nuget.org/packages/Extensions.Hosting.Wpf.Threading/) |
| **Extensions.Hosting.Wpf.Bootstrap** | An extension enabling easier bootstrap for 3rd-party DI containers. | [![NuGet](https://img.shields.io/nuget/vpre/Extensions.Hosting.Wpf.Bootstrap.svg)](https://www.nuget.org/packages/Extensions.Hosting.Wpf.Bootstrap/) |

## Quick Start

### 1. Add `IApplicationInitializeComponent` to `App.xaml.cs`
```csharp
public partial class App : Application, IApplicationInitializeComponent
{
}
```

### 2. Create `Program.cs`
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();
        host.Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices);
    }

    private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
    {
        services.AddWpf<App>();
    }
}
```

### 3. Update `.csproj`
```xml
<StartupObject>[Namespace].Program</StartupObject>
```

For the full guide including **ViewModel Locator**, **constructor injection**, and more, see the **[Wiki](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki)**.

## Samples
1. [HostingSimple](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingSimple): A minimalistic, beginner-friendly introduction. Offers a basic starting point for understanding the framework.
2. [HostingReactiveUI](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingReactiveUI): An advanced example using **NLog** for logging, **ReactiveUI** as the MVVM framework, and the **TrayIcon** feature.
3. [HostingReactiveUISimpleInjector](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingReactiveUISimpleInjector): Incorporates **SimpleInjector** alongside **ReactiveUI**. Demonstrates that you're not limited to `Microsoft.DependencyInjection`.
4. [HostingReactiveUISimpleInjectorAmbientScope](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingReactiveUISimpleInjectorAmbientScope): Demonstrates `AsyncScopedLifestyle` with **SimpleInjector** for [ambient-scoping](https://blogs.cuttingedge.it/steven/posts/2019/ambient-composition-model/) and integrates **Threading**.
5. [HostingReactiveUISimpleInjectorFlowingScope](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/tree/main/samples/HostingReactiveUISimpleInjectorFlowingScope): Demonstrates `ScopedLifestyle.Flowing` with **SimpleInjector** for [closure-scoping](https://blogs.cuttingedge.it/steven/posts/2019/closure-composition-model/). Presents an alternative approach without `ViewModelLocatorHost`.

## Documentation

Full documentation is available in the **[Wiki](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki)**:

- [Getting Started](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/3.-Getting-started-%F0%9F%9A%80) — Minimal setup guide
- [ViewModelLocator Feature](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/4.-ViewModelLocator-feature-%F0%9F%92%89) — Bind DataContext in XAML via DI
- [Constructor Injection in App](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/5.-Constructor-injection-in-App-%F0%9F%94%A7) — Inject services into your App class
- [Threading](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/7.-Threading-extension-%F0%9F%A7%B5) — Switch between UI and background threads
- [TrayIcon](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/8.-TrayIcon-extension-%F0%9F%94%94) — System tray icon support
- [Bootstrap (3rd-party DI)](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/9.-Bootstrap-extension-%F0%9F%8F%97%EF%B8%8F) — SimpleInjector, Autofac, and other DI containers
- [Architecture & How It Works](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/10.-Architecture-&-How-it-works-%F0%9F%93%90) — Internal architecture and lifecycle
- [API Reference](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/11.-API-Reference-%F0%9F%93%9A) — Complete API reference
- [FAQ & Troubleshooting](https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/wiki/13.-FAQ-&-Troubleshooting-%E2%9D%93) — Common questions and error solutions
