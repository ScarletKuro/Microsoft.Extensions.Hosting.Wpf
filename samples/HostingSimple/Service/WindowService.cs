using HostingSimple.View;

namespace HostingSimple.Service
{
    /// <summary>
    /// Just an example we can make different services
    /// </summary>
    public class WindowService
    {
        public void OpenMainWindow()
        {
            var window = new MainWindow();
            window.ShowDialog();
        }

        public void OpenChildWindow()
        {
            var window = new ChildWindow();
            window.ShowDialog();
        }
    }
}
