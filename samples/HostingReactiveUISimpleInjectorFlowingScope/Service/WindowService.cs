using HostingReactiveUISimpleInjectorFlowingScope.Locator;
using HostingReactiveUISimpleInjectorFlowingScope.View;

namespace HostingReactiveUISimpleInjectorFlowingScope.Service
{
    /// <summary>
    /// Just an example we can make different services
    /// </summary>
    public class WindowService
    {
        private readonly IViewModelLocator _locator;

        public WindowService(IViewModelLocator locator)
        {
            _locator = locator;
        }

        public void OpenMainWindow()
        {
            var window = new MainWindow
            {
                ViewModel = _locator.Main
            };
            window.ShowDialog();
        }

        public void OpenChildWindow()
        {
            var window = new ChildWindow
            {
                ViewModel = _locator.Child
            };
            window.ShowDialog();
        }
    }
}
