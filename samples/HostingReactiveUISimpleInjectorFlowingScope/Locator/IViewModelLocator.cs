using HostingReactiveUISimpleInjectorFlowingScope.ViewModel;

namespace HostingReactiveUISimpleInjectorFlowingScope.Locator
{
    public interface IViewModelLocator
    {
        MainViewModel Main { get; }

        ChildViewModel Child { get; }
    }
}
