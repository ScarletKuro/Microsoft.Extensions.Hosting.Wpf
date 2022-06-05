using HostingSimple.ViewModels;

namespace HostingSimple.Locator
{
    public interface IViewModelLocator
    {
        MainViewModel Main { get; }

        ChildViewModel Child { get; }
    }
}
