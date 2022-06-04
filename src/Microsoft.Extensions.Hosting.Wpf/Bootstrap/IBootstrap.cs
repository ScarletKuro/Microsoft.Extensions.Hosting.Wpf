using System.Reflection;

namespace Microsoft.Extensions.Hosting.Wpf.Bootstrap
{
    public interface IBootstrap<in TContainer> where TContainer : class
    {
        void Boot(TContainer container, Assembly[] assemblies);
    }
}
