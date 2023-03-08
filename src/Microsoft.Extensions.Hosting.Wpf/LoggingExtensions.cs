using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Wpf;

internal static partial class LoggingExtensions
{
    [LoggerMessage(0, LogLevel.Information, "Starting WPF application")]
    internal static partial void WpfStarting(this ILogger logger);

    [LoggerMessage(1, LogLevel.Information, "WPF thread started")]
    internal static partial void WpfThreadStarted(this ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Stopping WPF with Application.Shutdown() due to application exit")]
    internal static partial void WpfStopping(this ILogger logger);

    [LoggerMessage(3, LogLevel.Information, "WPF application started")]
    internal static partial void WpfApplicationStarted(this ILogger logger);

    [LoggerMessage(4, LogLevel.Information, "WPF application is shutting down...")]
    internal static partial void WpfApplicationShuttingDown(this ILogger logger);

    [LoggerMessage(5, LogLevel.Information, "WPF application was stopped")]
    internal static partial void WpfApplicationStopped(this ILogger logger);

    [LoggerMessage(6, LogLevel.Information, "Waiting for the host to be disposed, please ensure all 'IHost' instances are wrapped in 'using' blocks")]
    internal static partial void WaitingHost(this ILogger logger);

    [LoggerMessage(7, LogLevel.Information, "Lifetime: {lifetime}")]
    internal static partial void LifeTime(this ILogger logger, string lifetime);

    [LoggerMessage(8, LogLevel.Information, "Hosting environment: {envName}")]
    internal static partial void HostingEnvironment(this ILogger logger, string envName);

    [LoggerMessage(9, LogLevel.Information, "Content root path: {contentRoot}")]
    internal static partial void ContentRootPath(this ILogger logger, string contentRoot);
}