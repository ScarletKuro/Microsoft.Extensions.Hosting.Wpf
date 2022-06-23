using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.GenericHost
{
    /// <summary>
    /// Internal implementation of <see cref="IWpfContext{TApplication}"/>. 
    /// </summary>
    /// <remarks>This type is only used inside the library.</remarks>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    internal class WpfContext<TApplication>
        : IWpfContext<TApplication> where TApplication : Application, new()
    {
        private TApplication? _wpfApplication;
        private bool _isLifetimeLinked;
        private bool _isRunning;

        /// <summary>
        /// Shows if <see cref="WpfLifetime{TApplication}"/> is used.
        /// </summary>
        public bool IsLifetimeLinked
        {
            get => _isLifetimeLinked;
            internal set => _isLifetimeLinked = value;
        }

        /// <summary>
        /// Shows if WPF is running inside Microsoft.Extensions.Hosting
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            internal set => _isRunning = value;
        }

        /// <inheritdoc />
        bool IWpfContext.IsLifetimeLinked
        {
            get => _isLifetimeLinked;
            set => _isLifetimeLinked = value;
        }

        /// <inheritdoc />
        bool IWpfContext.IsRunning
        {
            get => _isRunning;
            set => _isRunning = value;
        }

        /// <inheritdoc />
        Application? IWpfContext.WpfApplication => _wpfApplication;

        /// <inheritdoc />
        public TApplication? WpfApplication
        {
            get => _wpfApplication;
            private set => _wpfApplication = value;
        }

        /// <inheritdoc />
        public Dispatcher Dispatcher => _wpfApplication?.Dispatcher ?? throw new InvalidOperationException($"{nameof(WpfApplication)} is not initialized!");

        internal void SetWpfApplication(TApplication application)
        {
            _wpfApplication = application;
        }
    }
}
