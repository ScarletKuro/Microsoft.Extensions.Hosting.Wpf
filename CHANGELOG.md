# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/HEAD..3.0.0
[3.0.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/2.0.0..3.0.0
[2.0.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/compare/1.0.0..2.0.0
[1.0.0]: https://github.com/ScarletKuro/Microsoft.Extensions.Hosting.Wpf/commits/1.0.0
