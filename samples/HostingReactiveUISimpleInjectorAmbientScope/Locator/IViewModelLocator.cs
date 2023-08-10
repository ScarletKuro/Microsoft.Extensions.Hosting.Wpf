using HostingReactiveUISimpleInjectorAmbientScope.ViewModel;

namespace HostingReactiveUISimpleInjectorAmbientScope.Locator
{
    public interface IViewModelLocator
    {
        MainViewModel Main { get; }

        ChildViewModel Child { get; }
    }
}
