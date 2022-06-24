using System;
using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.TrayIcon;

public interface ITrayIcon : IWpfComponent, IDisposable
{
}