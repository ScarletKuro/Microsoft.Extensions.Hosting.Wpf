using System.Reactive;
using System.Reactive.Disposables;
using HostingReactiveUI.Service;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace HostingReactiveUI.ViewModels
{
    public class MainViewModel : ReactiveObject, IActivatableViewModel
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly WindowService _windowService;

        public ReactiveCommand<Unit, Unit> OpenChildWindowCommand { get; set; }

        public ViewModelActivator Activator { get; }

        public MainViewModel(ILogger<MainViewModel> logger, WindowService windowService)
        {
            _logger = logger;
            _windowService = windowService;

            Activator = new ViewModelActivator();

            OpenChildWindowCommand = ReactiveCommand.Create(OnOpenChildWindow);

            this.WhenActivated(disposables =>
            {
                HandleActivation(disposables);
                Disposable
                    .Create(HandleDeactivation)
                    .DisposeWith(disposables);
            });
        }


        private void HandleActivation(CompositeDisposable disposable)
        {
            _logger.LogInformation($"Activate {nameof(MainViewModel)}.");
        }

        private void HandleDeactivation()
        {
            _logger.LogInformation($"Deactivate {nameof(MainViewModel)}.");
        }

        private void OnOpenChildWindow()
        {
            _windowService.OpenChildWindow();
        }
    }
}
