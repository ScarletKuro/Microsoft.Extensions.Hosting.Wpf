using System.Windows;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

/// <summary>
/// The test process's only WPF <see cref="Application"/> (WPF allows one per AppDomain), running on its own STA thread.
/// Real host startup is tested out of process by <see cref="ScenarioTests"/>; this one serves tests that need
/// <see cref="Application.Current"/> and application resources.
/// </summary>
public sealed class WpfApplicationFixture : IAsyncLifetime
{
    private Thread? _thread;
    private Application? _application;

    public Application Application => _application ?? throw new InvalidOperationException("The fixture is not initialized.");

    public async Task InitializeAsync()
    {
        var created = new TaskCompletionSource<Application>(TaskCreationOptions.RunContinuationsAsynchronously);
        _thread = new Thread(() =>
        {
            Application application;
            try
            {
                application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (Exception exception)
            {
                created.SetException(exception);
                return;
            }

            created.SetResult(application);
            application.Run();
        })
        {
            IsBackground = true,
            Name = "WPF test application",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _application = await created.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    /// <summary>Runs <paramref name="action"/> on the application's UI thread.</summary>
    public void Invoke(Action<Application> action) => Application.Dispatcher.Invoke(() => action(Application));

    public async Task DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.Dispatcher.InvokeAsync(_application.Shutdown);
        }

        _thread?.Join(TimeSpan.FromSeconds(15));
    }
}

[CollectionDefinition(Name)]
public sealed class WpfApplicationCollection : ICollectionFixture<WpfApplicationFixture>
{
    public const string Name = "WPF application";
}
