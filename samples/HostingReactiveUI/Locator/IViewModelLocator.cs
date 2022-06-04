using HostingReactiveUI.ViewModels;

namespace HostingReactiveUI.Locator
{
    public interface IViewModelLocator
    {
        MainViewModel Main { get; }

        ChildViewModel Child { get; }
    }
}
