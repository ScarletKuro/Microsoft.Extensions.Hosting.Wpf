using System;
using System.Windows;
using System.Windows.Threading;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost
{
    public class WpfContext<TApplication>
        where TApplication : Application, new()
    {
        /// <summary>
        /// Shows if <see cref="WpfLifetime{TApplication}"/> is used.
        /// </summary>
        public bool IsLifetimeLinked { get; internal set; }

        /// <summary>
        /// Shows if WPF is running inside Microsoft.Extensions.Hosting
        /// </summary>
        public bool IsRunning { get; internal set; }

        /// <summary>
        /// Instance of WPF <see cref="Application"/>.
        /// </summary>
        public TApplication? WpfApplication { get; private set; }

        /// <summary>
        /// WPF Dispatcher of <see cref="WpfApplication"/>.
        /// </summary>
        public Dispatcher Dispatcher => WpfApplication?.Dispatcher ?? throw new InvalidOperationException($"{nameof(WpfApplication)} is not initialized");

        internal void SetWpfApplication(TApplication application)
        {
            WpfApplication = application;
        }
    }
}
