using HostingReactiveUISimpleInjectorFlowingScope.Context;
using ReactiveUI;

namespace HostingReactiveUISimpleInjectorFlowingScope.ViewModel
{
    public class ChildViewModel : ReactiveObject
    {
        public GuidContext Context { get; }

        public ChildViewModel(GuidContext context)
        {
            Context = context;
        }
    }
}
