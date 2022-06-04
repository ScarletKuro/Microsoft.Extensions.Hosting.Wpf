using Microsoft.Extensions.Hosting.Wpf.Locator;

namespace HostingReactiveUISimpleInjector.Locator
{
    public class ViewModelLocatorHost : AbstractViewModelLocatorHost<IViewModelLocator>
    {
        public ViewModelLocatorHost() : base("Locator")
        {
        }
    }
}
