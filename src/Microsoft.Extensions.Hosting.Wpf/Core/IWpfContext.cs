using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;

namespace Microsoft.Extensions.Hosting.Wpf.Core;

public interface IWpfContext
{
    /// <summary>
    /// Shows if <see cref="WpfLifetime"/> is used.
    /// </summary>
    bool IsLifetimeLinked { get; internal set; }

    /// <summary>
    /// Shows if WPF is running inside <see cref="Microsoft.Extensions.Hosting"/>
    /// </summary>
    bool IsRunning { get; internal set; }

    /// <summary>
    /// Instance of WPF <see cref="Application"/>.
    /// </summary>
    Application? WpfApplication { get; }

    /// <summary>
    /// WPF Dispatcher of <see cref="WpfApplication"/>.
    /// </summary>
    Dispatcher Dispatcher { get; }
}

public interface IWpfContext<out TApplication> : IWpfContext
    where TApplication : Application
{
    /// <summary>
    /// Instance of WPF <see cref="TApplication"/>.
    /// </summary>
    new TApplication? WpfApplication { get; }
}