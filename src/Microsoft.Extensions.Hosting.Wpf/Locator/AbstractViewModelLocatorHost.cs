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

            return GetInstance(applicationInstance, locatorName);
        }

        public static AbstractViewModelLocatorHost<TViewModelLocator>? GetInstance<TApplication>(TApplication applicationInstance, string locatorName)
            where TApplication : Application
        {
            return applicationInstance.Resources[locatorName] as AbstractViewModelLocatorHost<TViewModelLocator>;
        }

        private TViewModelLocator? _viewModelLocator;

        public TViewModelLocator ViewModelLocator
        {
            get
            {
                if (_viewModelLocator is null)
                {
                    throw new InvalidOperationException("Please add IViewModelLocatorInitialization<DiContainer> to your App wpf class. \n" +
                                                        "Then in Initialize(viewModelLocator) add `var viewModelLocatorHost = ViewModelLocatorHost.GetInstance(this) [ViewModelLocatorHost should implement AbstractViewModelLocatorHost<DiContainer>]`" +
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
            //Look in merged dictionaries too just in case someone adds it there.
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
