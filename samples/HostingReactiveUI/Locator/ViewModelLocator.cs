using System;
using HostingReactiveUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HostingReactiveUI.Locator
{
    public class ViewModelLocator : IViewModelLocator
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
