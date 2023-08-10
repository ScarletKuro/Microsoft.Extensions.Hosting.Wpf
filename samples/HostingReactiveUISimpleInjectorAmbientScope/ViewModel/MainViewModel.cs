using System;
using System.Reactive;
using System.Reactive.Disposables;
using HostingReactiveUISimpleInjectorAmbientScope.Context;
using HostingReactiveUISimpleInjectorAmbientScope.Service;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace HostingReactiveUISimpleInjectorAmbientScope.ViewModel
{
    public class MainViewModel : ReactiveObject, IActivatableViewModel, IDisposable
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly WindowService _windowService;

        public ReactiveCommand<Unit, Unit> OpenChildWindowCommand { get; set; }

        public ViewModelActivator Activator { get; }

        public GuidContext Context { get; }

        public MainViewModel(ILogger<MainViewModel> logger, WindowService windowService, GuidContext context)
        {
            _logger = logger;
            _windowService = windowService;
            Context = context;

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

        public void Dispose()
        {
            //Example of dispose
            OpenChildWindowCommand.Dispose();
            Activator.Dispose();
        }
    }
}
