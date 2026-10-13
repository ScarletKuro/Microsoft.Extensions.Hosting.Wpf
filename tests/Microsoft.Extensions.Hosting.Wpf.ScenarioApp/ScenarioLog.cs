namespace Microsoft.Extensions.Hosting.Wpf.ScenarioApp;

/// <summary>
/// Writes machine readable lines to standard output, read by ScenarioTests:
/// <c>SCENARIO|EVENT|name|threadId|value</c> and <c>SCENARIO|INFO|key|value</c>.
/// Lines are written in the order the events happened, across threads.
/// </summary>
public sealed class ScenarioLog
{
    public const string Prefix = "SCENARIO|";

    private readonly object _lock = new();

    /// <summary>Records a lifecycle event and the thread it happened on.</summary>
    public void Event(string name, object? value = null) =>
        Write($"{Prefix}EVENT|{name}|{Environment.CurrentManagedThreadId}|{value}");

    /// <summary>Records a fact about the run.</summary>
    public void Info(string key, object? value) => Write($"{Prefix}INFO|{key}|{value}");

    private void Write(string line)
    {
        lock (_lock)
        {
            Console.Out.WriteLine(line);
            Console.Out.Flush();
        }
    }
}
