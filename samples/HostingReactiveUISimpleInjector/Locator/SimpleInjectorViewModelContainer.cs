using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using SimpleInjector;

namespace HostingReactiveUISimpleInjector.Locator
{
    // You actually don't really need to implement IViewModelContainer if you don't want to and use directly UseWpfViewModelLocator<App, ViewModelLocator>(new ViewModelLocator(container))
    // IViewModelContainer was added if you want hide your DI implementation for Locator
    public class SimpleInjectorViewModelContainer : IViewModelContainer, IDisposable, IAsyncDisposable
    {
        private readonly Container _container;

        public SimpleInjectorViewModelContainer(Container container)
        {
            _container = container;
        }

        public T GetService<T>() where T : class
        {
            return _container.GetInstance<T>();
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _container.DisposeAsync();
        }
    }
}
