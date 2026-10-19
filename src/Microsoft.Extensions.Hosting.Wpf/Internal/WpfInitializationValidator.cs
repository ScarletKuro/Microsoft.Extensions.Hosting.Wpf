using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Fails host startup when WPF initialization is configured but the matching application was never registered with AddWpf.
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
/// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection (AddHostedService).")]
internal sealed class WpfInitializationValidator<TApplication>
    : IHostedService where TApplication : Application, IApplicationInitializeComponent
{
    private readonly IServiceProvider _serviceProvider;

    public WpfInitializationValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_serviceProvider.GetService<WpfThread<TApplication>>() is null)
        {
            throw new InvalidOperationException(WpfInitialization<TApplication>.MissingApplicationMessage);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
