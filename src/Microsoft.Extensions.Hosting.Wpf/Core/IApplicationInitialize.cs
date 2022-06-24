using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf.Core;

public interface IApplicationInitialize
{
    /// <summary>
    /// Pre initialization that happens before <see cref="Application.Run()"/>. This action happens on UI thread.
    /// </summary>
    void Initialize();
}