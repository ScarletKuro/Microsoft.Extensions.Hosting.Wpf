using System;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf.Locator
{
    public class AbstractViewModelLocatorHost<TViewModelLocator>
        where TViewModelLocator : class
    {
        private const string DefaultLocatorName = "Locator";

        public static AbstractViewModelLocatorHost<TViewModelLocator>? GetInstance<TApplication>(TApplication applicationInstance)
            where TApplication : Application
        {
            var locatorName = FindNameFromApplication(applicationInstance) ?? DefaultLocatorName;

            return applicationInstance.Resources[locatorName] as AbstractViewModelLocatorHost<TViewModelLocator>;
        }

        private TViewModelLocator? _viewModelLocator;

        public TViewModelLocator ViewModelLocator
        {
            get
            {
                if (_viewModelLocator is null)
                {
                    throw new InvalidOperationException("Please add IViewModelServiceProviderLocatorInitialization / IViewModelLocatorInitialization<DiContainer> to your App wpf class. \n" +
                                                        "Then in Initialize(viewModelLocator) add `var viewModelLocatorHost = DefaultViewModelServiceProviderLocatorHost.GetInstance(this) / or LocatorHost that implement AbstractViewModelLocatorHost<DiContainer>`" +
                                                        " and `viewModelLocatorHost?.SetViewModelLocator(viewModelLocator);`");
                }

                return _viewModelLocator;
            }
        }

        public void SetViewModelLocator(TViewModelLocator viewModelLocator)
        {
            _viewModelLocator = viewModelLocator;
        }

        private static string? FindNameFromApplication<TApplication>(TApplication applicationInstance)
            where TApplication : Application
        {
            var collection = new List<ResourceDictionary> { applicationInstance.Resources };
            collection.AddRange(applicationInstance.Resources.MergedDictionaries);

            return FindNameFromResource(collection);
        }

        private static string? FindNameFromResource(IEnumerable<ResourceDictionary> dictionaries)
        {
            foreach (var dictionary in dictionaries)
            {
                foreach (var key in dictionary.Keys)
                {
                    if (key is not null)
                    {
                        if (dictionary[key] is AbstractViewModelLocatorHost<TViewModelLocator>)
                        {
                            return key.ToString();
                        }
                    }
                }
            }

            return null;
        }
    }
}
