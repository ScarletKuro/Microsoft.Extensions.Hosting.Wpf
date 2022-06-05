using System;
using HostingSimple.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HostingSimple.Locator
{
    public class ViewModelLocator
        : IViewModelLocator
    {
        public IServiceProvider Container { get; }

        public MainViewModel Main => Container.GetRequiredService<MainViewModel>();

        public ChildViewModel Child => Container.GetRequiredService<ChildViewModel>();

        public ViewModelLocator(IServiceProvider container)
        {
            Container = container;
        }
    }
}
