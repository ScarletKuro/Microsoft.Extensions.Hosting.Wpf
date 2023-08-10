using HostingReactiveUISimpleInjectorAmbientScope.ViewModel;
using SimpleInjector;

namespace HostingReactiveUISimpleInjectorAmbientScope.Locator
{
    public class ViewModelLocator
        : IViewModelLocator
    {
        public Container Container { get; }

        public MainViewModel Main => Container.GetInstance<MainViewModel>();

        public ChildViewModel Child => Container.GetInstance<ChildViewModel>();

        public ViewModelLocator(Container container)
        {
            Container = container;
        }
    }
}
