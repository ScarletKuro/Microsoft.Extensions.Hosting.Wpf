using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf.Locator
{
    public class AbstractViewModelLocatorHost<TViewModelLocator> where TViewModelLocator : class
    {
        // ReSharper disable once StaticMemberInGenericType
        private static string _locatorName = "Locator";

        public static AbstractViewModelLocatorHost<TViewModelLocator>? Instance => Application.Current.Resources[_locatorName] as AbstractViewModelLocatorHost<TViewModelLocator>;

        public string LocatorName
        {
            get => _locatorName;
            private set => _locatorName = value;
        }

        public TViewModelLocator ViewModelLocator { get; private set; } = null!;

        public AbstractViewModelLocatorHost(string locatorName)
        {
            LocatorName = locatorName;
        }

        public void SetViewModelLocator(TViewModelLocator viewModelLocator)
        {
            ViewModelLocator = viewModelLocator;
        }
    }
}
