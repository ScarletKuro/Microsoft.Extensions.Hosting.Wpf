using System.Windows.Input;
using HostingSimple.Internal;
using HostingSimple.Service;

namespace HostingSimple.ViewModels
{
    public class MainViewModel
    {
        private readonly WindowService _windowService;

        public ICommand OpenChildWindowCommand { get; set; }

        public MainViewModel(WindowService windowService)
        {
            _windowService = windowService;

            OpenChildWindowCommand = new Command(OnOpenChildWindow);

        }

        private void OnOpenChildWindow()
        {
            _windowService.OpenChildWindow();
        }
    }
}
