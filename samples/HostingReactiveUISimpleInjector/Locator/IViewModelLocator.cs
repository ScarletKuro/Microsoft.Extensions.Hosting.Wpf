using HostingReactiveUISimpleInjector.ViewModel;

namespace HostingReactiveUISimpleInjector.Locator
{
    public interface IViewModelLocator
    {
        MainViewModel Main { get; }

        ChildViewModel Child { get; }
    }
}
