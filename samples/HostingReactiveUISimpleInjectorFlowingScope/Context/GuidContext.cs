using System;

namespace HostingReactiveUISimpleInjectorFlowingScope.Context
{
    public class GuidContext
    {
        public Guid Id { get; private set; }

        public void SetId(Guid id)
        {
            Id = id;
        }
    }
}
