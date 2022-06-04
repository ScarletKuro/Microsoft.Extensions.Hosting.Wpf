using System;
using System.Windows;
using HostingReactiveUI.ViewModels;
using ReactiveUI;

namespace HostingReactiveUI.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewFor<MainViewModel>
    {
        private readonly IDisposable? _whenActivatedSubscription;

        public MainWindow()
        {
            InitializeComponent();
            _whenActivatedSubscription = this.WhenActivated(disposables =>
            {
                if (_whenActivatedSubscription is not null)
                {
                    disposables(_whenActivatedSubscription);
                }
            });
        }

        public MainViewModel? ViewModel
        {
            get => (MainViewModel)DataContext;
            set => DataContext = value;
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainViewModel?)value;
        }
    }
}
