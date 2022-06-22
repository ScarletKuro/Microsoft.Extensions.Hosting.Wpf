using HostingReactiveUISimpleInjectorFlowingScope.ViewModel;
using SimpleInjector;

namespace HostingReactiveUISimpleInjectorFlowingScope.Locator
{
    public class ViewModelLocator
        : IViewModelLocator
    {
        public Scope Scope { get; }

        public MainViewModel Main => Scope.GetInstance<MainViewModel>();

        public ChildViewModel Child => Scope.GetInstance<ChildViewModel>();

        public ViewModelLocator(Scope scope)
        {
            //Use SimpleInjector scope
            Scope = scope;
        }
    }
}
