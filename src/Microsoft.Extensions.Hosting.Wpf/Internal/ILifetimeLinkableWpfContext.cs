using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.GenericHost;

namespace Microsoft.Extensions.Hosting.Wpf.Internal;

/// <summary>
/// Lets <see cref="WpfLifetime"/> mark the non-generic <see cref="IWpfContext"/> as linked without exposing a public setter.
/// </summary>
/// <remarks>This type is only used inside the library.</remarks>
internal interface ILifetimeLinkableWpfContext
{
    void LinkLifetime();
}
