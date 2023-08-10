using HostingReactiveUISimpleInjectorAmbientScope.Context;
using ReactiveUI;

namespace HostingReactiveUISimpleInjectorAmbientScope.ViewModel
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
