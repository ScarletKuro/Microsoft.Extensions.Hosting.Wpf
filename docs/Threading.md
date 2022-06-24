# Microsoft.Extensions.Hosting.Wpf.Threading
Extension library for `Microsoft.Extensions.Hosting.Wpf`. This adds support to switch to Main UI Thread or ThreadPool Thread  with the help of `Microsoft.VisualStudio.Threading`.

This can be really useful when using WPF inside `Microsoft.Extensions.Hosting` since sometimes you might need to show UI from not UI thread, for example during some network request.

## Getting Started

### Install nuget
```Install-Package Extensions.Hosting.Wpf.Threading```

### Register service
```CSharp
private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
{
    services.AddWpf<App>();
    services.AddThreadSwitching(); //<--- new line
}
```

## Example Usage

```CSharp
private readonly JoinableTaskFactory _joinableTaskFactory;

public SomeConstructor(JoinableTaskFactory joinableTaskFactory)
{
    _joinableTaskFactory = joinableTaskFactory
}

private async Task SomeOperationAsync() {
    // on the caller's thread.
    await DoAsync();

    // Now switch to a threadpool thread explicitly.
    await TaskScheduler.Default;

    // Now switch to the Main thread to talk to some STA object.
    await _joinableTaskFactory.SwitchToMainThreadAsync();
    STAService.DoSomething();
}
```
for more use cases please visit [Microsoft.VisualStudio.Threading](https://github.com/microsoft/vs-threading) repository.
