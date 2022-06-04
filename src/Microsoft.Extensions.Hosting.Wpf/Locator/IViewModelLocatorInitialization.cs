using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf.Locator
{
    public interface IViewModelLocatorInitialization<in TViewModelLocator>
    {
        /// <summary>
        /// Pre initialization that happens before <see cref="Application.Run()"/>. This action happens on UI thread.
        /// </summary>
        void Initialize(TViewModelLocator viewModelLocator);
    }
}
