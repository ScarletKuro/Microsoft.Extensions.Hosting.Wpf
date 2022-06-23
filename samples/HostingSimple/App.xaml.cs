using System.Windows;
using HostingSimple.Locator;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.Locator;

namespace HostingSimple
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IViewModelLocatorInitialization<ViewModelLocator>, IApplicationInitializeComponent
    {
        public void Initialize()
        {
            //Here we can initialize important things. This method always runs on UI thread. 
            //In this example it's empty as we do not have anything to initialize like ReactiveUI
        }

        public void InitializeLocator(ViewModelLocator viewModelLocator)
        {
            //We need to set it so that our <locator:ViewModelLocatorHost x:Key="Locator"/> could resolve ViewModels for DataContext
            //You can also use it as service locator pattern, but I personally recommend you to use it only inside View xaml to bind the DataContext
            var viewModelLocatorHost = ViewModelLocatorHost.GetInstance(this);
            viewModelLocatorHost?.SetViewModelLocator(viewModelLocator);
        }
    }
}
