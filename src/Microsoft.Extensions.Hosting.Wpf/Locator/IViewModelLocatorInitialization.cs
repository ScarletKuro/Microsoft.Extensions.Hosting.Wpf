using Microsoft.Extensions.Hosting.Wpf.Core;

namespace Microsoft.Extensions.Hosting.Wpf.Locator;

public interface IViewModelLocatorInitialization<in TViewModelLocator> 
    : IApplicationInitialize
{
    /// <summary>
    /// Pre initialization that happens after <see cref="IApplicationInitialize.Initialize"/>. This action happens on UI thread.
    /// This method should be used to set <see cref="AbstractViewModelLocatorHost{TViewModelLocator}.SetViewModelLocator"/>
    /// </summary>
    void InitializeLocator(TViewModelLocator viewModelLocator);
}