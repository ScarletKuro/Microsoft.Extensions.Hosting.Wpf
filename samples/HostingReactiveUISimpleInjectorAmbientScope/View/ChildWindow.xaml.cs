using System.Windows;
using HostingReactiveUISimpleInjectorAmbientScope.ViewModel;
using ReactiveUI;

namespace HostingReactiveUISimpleInjectorAmbientScope.View
{
    /// <summary>
    /// Interaction logic for ChildWindow.xaml
    /// </summary>
    public partial class ChildWindow : Window, IViewFor<ChildViewModel>
    {
        public ChildWindow()
        {
            InitializeComponent();
        }

        public ChildViewModel? ViewModel
        {
            get => (ChildViewModel)DataContext;
            set => DataContext = value;
        }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (ChildViewModel?)value;
        }
    }
}
