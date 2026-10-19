# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Migrating from 4.x
Most applications only need the first two rows:

| If you… | Do this | Why it changed |
|---|---|---|
| target `netcoreapp3.1` | Move to .NET 6+ (or .NET Framework 4.6.2 / 4.8), or stay on 4.x | Out of support since 2022, and `Microsoft.Extensions.*` 8.x does not support it (build warnings in your app) |
| reference `Microsoft.Extensions.*` 6.x/7.x | Update to 8.x | `IHostApplicationBuilder` (the new API) only exists from `Microsoft.Extensions.Hosting.Abstractions` 8.0 |
| reference `Microsoft.VisualStudio.Threading` < 17.12 (Threading package) | Update; review new `VSTHRD` analyzer warnings once, or tune them in `.editorconfig` | 17.12 is the newest release that still supports .NET 6 / 7; the analyzers come with it |
| use the UI from a hosted service's `StartAsync` | Do it from `IHostApplicationLifetime.ApplicationStarted`. There is no compile error for this one | WPF now starts after every hosted service has started, so a failing hosted service never leaves WPF running against a disposed host |
| name `WpfLifeTimeOptions` explicitly | Rename to `WpfLifetimeOptions` (`options => …` lambdas are unaffected) | Consistent with `WpfLifetime` / `IHostLifetime` |

Other breaking changes affect rarely used APIs and show up as compile errors; each is listed under **Breaking** in Changed below.

### Added
- An exception thrown from `OnStartup` or a `Startup` handler (also after a nested dispatcher loop such as `ShowDialog()`) now fails host startup (`host.Run()` / `StartAsync` throws it) instead of crashing the process from the UI thread.
- `IHostApplicationBuilder` / `HostApplicationBuilder` / `WebApplicationBuilder` support through new `IServiceCollection` extensions, used as `builder.Services.Add...()`:
  - `AddWpfLifetime()` / `AddWpfLifetime(configureOptions)`
  - `AddWpfInitialization<TApplication>()`
  - `AddWpfViewModelLocator<TApplication, TViewModelLocator>(viewModelLocator | viewModelLocatorFunc)`
- `IHostBuilder` overloads of `UseWpfInitialization` and `UseWpfViewModelLocator` that configure initialization before `Build()`.
- Host startup fails with a clear message, before any hosted service starts, when initialization is configured but `AddWpf<TApplication>()` was not called.
- `HostingMinimal` sample: `Host.CreateApplicationBuilder` with top-level statements in a code-only application. `HostingSimple` now uses `Host.CreateApplicationBuilder` with top-level statements and `App.xaml`.
- README: turning off WPF's generated `Main` with `EnableDefaultApplicationDefinition` as an alternative to `StartupObject`.

### Changed
- **Breaking:** WPF is now created and started once every hosted service has started (in `IHostedLifecycleService.StartedAsync`, the way ASP.NET Core starts serving requests), instead of in `WpfHostedService.StartAsync` at its registration position; `StartedAsync` completes once `OnStartup` has returned (also when it runs a nested dispatcher loop, e.g. `ShowDialog()`). If a hosted service fails to start, WPF is never created, so no application code (`App`, `OnExit`, windows) runs while the failed host is being disposed. Hosted services that use the UI (`IWpfContext.Dispatcher`, `JoinableTaskFactory.SwitchToMainThreadAsync`, ...) in their own `StartAsync` must do that from `IHostApplicationLifetime.ApplicationStarted` instead.
- Started last, WPF is now also stopped first: in `IHostedLifecycleService.StoppingAsync`, before any hosted service's `StopAsync`. The host waits until WPF has fully exited (`OnExit`, component disposal), up to `HostOptions.ShutdownTimeout`, so application cleanup can still use other hosted services and all registered services.
- **Breaking:** Removed the `netcoreapp3.1` target from all packages. It is out of support and `Microsoft.Extensions.*` 8.x does not support it (it produced a "doesn't support" build warning in consuming apps). Supported: .NET Framework 4.6.2 / 4.8 and .NET 6, 7, 8, 9 and 10, all covered by the tests. Applications on .NET Core 3.1 can stay on 4.x.
- **Breaking:** `Extensions.Hosting.Wpf` now depends on `Microsoft.Extensions.Hosting` 8.0.1 (previously 6.0.0) and `Extensions.Hosting.Wpf.Bootstrap` on `Microsoft.Extensions.Hosting.Abstractions` 8.0.1 (previously 6.0.0), which raises the minimum `Microsoft.Extensions.*` package versions for consumers.
- **Breaking:** `Extensions.Hosting.Wpf.Threading` now depends on `Microsoft.VisualStudio.Threading` 17.12.19 (previously 17.5.22), the newest release that still supports .NET 6 / 7 (17.13+ dropped them). Its `VSTHRD` analyzers flow to consuming projects (as before), so newer rules may report new warnings. Applications can reference a newer version themselves.
- **Breaking:** Renamed `WpfLifeTimeOptions` to `WpfLifetimeOptions`.
- **Breaking:** `AbstractViewModelLocatorHost<TViewModelLocator>` is now `abstract`; derive your locator host from it.
- **Breaking:** `WpfHostedService<TApplication>` is now internal. It is registered by `AddWpf<TApplication>()`.
- **Breaking:** Removed `IWpfThread.Start()` from the public interface. The library starts the WPF thread at the right point of the host lifecycle; starting it elsewhere bypassed startup ordering and failure handling.
- **Breaking:** `IWpfContext.IsRunning` is now get-only (it previously had an `internal` setter).
- **Breaking:** Removed `IWpfContext.IsLifetimeLinked`. Since 4.1.1 the host is stopped when WPF exits regardless of the `IHostLifetime`, so the flag no longer affected anything. To check whether `WpfLifetime` is used, test `services.GetService<IHostLifetime>() is WpfLifetime`.
- **Breaking:** `WpfLifetime` constructors no longer take an `IWpfContext` parameter; `WpfLifetime` no longer depends on `AddWpf<TApplication>()`.
- **Breaking:** Renamed `Extensions.Hosting.Wpf.Bootstrap`'s `WpfHostingExtensions` to `WpfBootstrapHostingExtensions` to avoid clashing with the main package's class. Extension-method call sites are unaffected.
- `UseWpfContainerBootstrap` and `AddBootstrap` now throw `ArgumentNullException` for a null container or service collection.
- `AddWpfTrayIcon` now throws `ArgumentNullException` for a null service collection or factory.
- `AddThreadSwitching` can be called more than once, or with both overloads, without registering duplicate `JoinableTaskContext` / `JoinableTaskFactory` services.
- The error thrown by `AbstractViewModelLocatorHost.ViewModelLocator` when no locator is set now describes the current setup (`InitializeLocator`, `AddWpfViewModelLocator`).
- `IWpfContext.IsRunning` is now safe to read from any thread.
- Stopping the host no longer reads WPF's private `Application._appIsShutdown` field via reflection; it uses the public `Dispatcher.HasShutdownStarted`.
- Calling `host.UseWpfInitialization` / `host.UseWpfViewModelLocator` after the host has started now throws `InvalidOperationException` instead of being silently ignored or racing with the UI thread.
- **Breaking:** `AddWpf<TApplication>()` now throws `InvalidOperationException` if `AddWpf` was already called for a different application type (WPF allows only one `Application` per process; this previously failed at runtime). It also throws `ArgumentNullException` for null arguments.
- `IHost.UseWpfViewModelLocator` and `IHostBuilder.UseWpfLifetime(configureOptions)` now throw `ArgumentNullException` for a null locator, factory, or options delegate.

### Fixed
- When the host stopped first (Ctrl+C, `StopApplication()`, `HandleApplicationExit()` from a tray icon), `StopAsync` returned as soon as `Application.Shutdown()` was queued, so the host disposed services before `OnExit` and component disposal ran (`ObjectDisposedException` in application cleanup). `StopAsync` now waits until the UI thread has ended (bounded by the host's shutdown timeout).
- If a hosted service failed in `StartedAsync` after WPF had started, WPF was shut down only after the host had disposed the application's services. WPF is now shut down first, while every service the application resolved during startup (including in `OnStartup`) still works.
- `AddThreadSwitching`: `JoinableTaskFactory` / `JoinableTaskContext` can now be resolved before the WPF UI thread starts, e.g. by a hosted service constructor. Previously this threw "WPF Thread was not started."
- With `UseWpfLifetime`, the exit code passed to `Application.Shutdown(exitCode)` was reset to 0 when the process exited, in applications that do not dispose the host (`host.Run()` does). `WpfLifetime` no longer overrides `Environment.ExitCode` (a `ConsoleLifetime` SIGTERM workaround that does not apply to WPF).
- If a hosted service failed to start after WPF had started, the host was disposed but the WPF application kept running, and closing it later crashed with `ObjectDisposedException`. Disposing the host now shuts down a still running WPF application and waits for the UI thread. If the host is disposed while the UI thread is still initializing (e.g. startup was cancelled), the application is never started.
- `AddWpfInitialization` / `AddWpfViewModelLocator` for a different application type than `AddWpf<TApplication>()` is now rejected at registration (in either order). Previously the mismatch was detected only after the registered application had already started.

## [4.1.1] - 2026-10-13
### Fixed
- With `UseWpfLifetime`, closing the WPF application could leave the host running (and the process alive without a window). `Application.Exit` was subscribed in `ApplicationStarted`, which could run before the application was created. The subscription now always happens on the UI thread before `Application.Run()`.
- An exception while creating or initializing the application (constructor, `InitializeComponent`, `UseWpfInitialization` / `UseWpfViewModelLocator`, the locator factory, or an `IWpfComponent` such as a tray icon) crashed the process from the UI thread without being logged. It now fails `IHost.StartAsync` with the original exception.

### Changed
- `WpfHostedService.StartAsync` now completes once the application is created and initialized on the UI thread, right before `Application.Run()`, so the application exists when `ApplicationStarted` is raised.

## [4.1.0] - 2026-10-13
### Fixed
- Fixed a race where stopping the host after WPF exited on its own (e.g. closing the last window with `WpfLifetime`) could fail with `TaskCanceledException` from `WpfHostedService.StopAsync`. `IWpfContext.IsRunning` is now reset when `Application.Run()` returns.

## [4.0.0] - 2026-08-17
### Changed
- Fixed `WpfThread` shutdown behavior to close WPF windows on the dispatcher thread before stopping the host application.
- Removed the unnecessary `new()` constraint from `AddThreadSwitching<TApplication>()`.
- Added `net9.0-windows` and `net10.0-windows` targets to `Extensions.Hosting.Wpf`.
- `Extensions.Hosting.Wpf.Bootstrap` no longer depends transitively on `Extensions.Hosting.Wpf`; consumers must reference both packages explicitly.
- `Extensions.Hosting.Wpf.Bootstrap` now targets `net6.0`, `net7.0`, and `net8.0` instead of Windows-specific TFMs because the package itself does not require WPF or WindowsDesktop APIs.
- Added `net9.0` and `net10.0` targets to `Extensions.Hosting.Wpf.Bootstrap`.
- Added `net9.0-windows` and `net10.0-windows` targets to `Extensions.Hosting.Wpf.Threading`.
- Added `net9.0-windows` and `net10.0-windows` targets to `Extensions.Hosting.Wpf.TrayIcon`.

### Removed
- Transitive `Extensions.Hosting.Wpf` package dependency from `Extensions.Hosting.Wpf.Bootstrap`. This is a breaking change for consumers that only installed the bootstrap package.

## [3.4.0] - 2023-11-24
#### Added
- Add net8-windows explicitly.

## [3.3.0] - 2023-03-08
#### Added
- Support for .NET Framework 4.6.2 and .NET Framework 4.8.

### Changed
- Update `Microsoft.VisualStudio.Threading` to 17.5.22 in `Microsoft.Extensions.Hosting.Threading`.
- Minor change to logging output.

## [3.2.0] - 2023-02-07
#### Added
- More `GetInstance` overloads in `AbstractViewModelLocatorHost`.

### Changed
- Make `AbstractViewModelLocatorHost.GetInstance` non nullable and throw if locator is not found.
- Use SourceGenerator for Logging (LoggerMessage).

### Removed
- Remove `UseWindowsForms` from `Microsoft.Extensions.Hosting`.

## [3.1.0] - 2023-01-26
#### Added
- Add net7.0 support.

### Changed
- Update `Microsoft.Extensions.Hosting` from 5.0.0 to 6.0.0 to avoid vulnerability warnings.

## [3.0.0] - 2022-06-24
### Removed
- Remove `IViewModelContainer`.
- Remove `new` constraint from `TApplication` in a classes / methods.
- Remove `ITrayIcon` from main `Microsoft.Extensions.Hosting.Wpf` package.

#### Added
- Add Nuget Source Link support.
- Add new `Microsoft.Extensions.Hosting.Wpf.TrayIcon` package.
- Add `IWpfContext<TApplication>` → non-genric `IWpfContext` interfaces.
- Add `IWpfThread<TApplication>` → non-genric `IWpfThread` interfaces.
- Add `IWpfComponent`interface.
- Add `IApplicationInitialize` interface.
- Add `ApplicationExtensions.CheckForInvalidConstructorConfiguration(this Application application)` public method.
- Add `UseWpfInitialization` extension.

### Changed
- Change `WpfThread<TApplication>` to internal class.
- Change `WpfContext<TApplication>` to internal class.
- Change `UseWpfLifetime<TApplication>()` to `UseWpfLifetime()`.
- Change `services.AddWpfTrayIcon<TrayIcon, TApplication>()` to `services.AddWpfTrayIcon<TrayIcon>()`
- Now `App` constructor can be resolved automatically. If you will have multiple constructors there and want to inject any service, please make sure you have a private parametless constructor. For diagnostic purposes you can use `ApplicationExtensions.CheckForInvalidConstructorConfiguration` in all `App` constructors to make sure everything is correct.

## [2.0.0] - 2022-06-08
### Removed
- Remove Boostrap from `Microsoft.Extensions.Hosting.Wpf`.

#### Added
- Add `Microsoft.Extensions.Hosting.Wpf.Bootstrap` library.

### Changed
- Update `Microsoft.VisualStudio.Threading` to 17.2.32 in `Microsoft.Extensions.Hosting.Threading`.
- Add `skipMergedDictionaries` parameter in `AbstractViewModelLocatorHost.GetInstance`.

## [1.0.0] - 2022-06-05
#### Added
- First release.

[Unreleased]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/HEAD..4.1.1
[4.1.1]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/4.1.0..4.1.1
[4.1.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/4.0.0..4.1.0
[4.0.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/3.4.0..4.0.0
[3.4.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/3.3.0..3.4.0
[3.3.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/3.2.0..3.3.0
[3.2.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/3.1.0..3.2.0
[3.1.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/3.0.0..3.1.0
[3.0.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/2.0.0..3.0.0
[2.0.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/1.0.0..2.0.0
[1.0.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/commits/1.0.0
