using System;
using System.Windows;
using System.Windows.Threading;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost
{
    public class WpfContext<TApplication> where TApplication : Application, new()
    {
        public bool IsLifetimeLinked { get; internal set; }

        public bool IsRunning { get; internal set; }

        public TApplication? WpfApplication { get; private set; }

        public Dispatcher Dispatcher => WpfApplication?.Dispatcher ?? throw new InvalidOperationException($"{nameof(WpfApplication)} is not initialized");

        internal void SetWpfApplication(TApplication application)
        {
            WpfApplication = application;
        }
    }
}
