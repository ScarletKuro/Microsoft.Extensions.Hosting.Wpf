using System;
using System.Collections.Generic;
using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf.Locator;

public class AbstractViewModelLocatorHost<TViewModelLocator>
    where TViewModelLocator : class
{
    private const string DefaultLocatorName = "Locator";

    /// <summary>
    /// Searches in <see cref="ResourceDictionary"/> for <see cref="AbstractViewModelLocatorHost{TViewModelLocator}"/>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <param name="applicationInstance">Instance of <see cref="Application" />.</param>
    /// <param name="skipMergedDictionaries">If <b>True</b> skips to lookup in ResourceDictionary.MergedDictionaries for performance purpose.</param>
    /// <exception cref="InvalidOperationException">Throws if <see cref="AbstractViewModelLocatorHost{TViewModelLocator}"/> is not found in resource.</exception>
#pragma warning disable CA1000
    public static AbstractViewModelLocatorHost<TViewModelLocator> GetInstance<TApplication>(TApplication applicationInstance, bool skipMergedDictionaries = false)
#pragma warning restore CA1000
        where TApplication : Application
    {
        ThrowHelper.ThrowIfNull(applicationInstance, nameof(applicationInstance));

        //DefaultLocator for a fast search to not enumerate whole ResourceDictionary.
        var viewModelLocator = DefaultLocator(applicationInstance) ?? FindFromResource(applicationInstance, skipMergedDictionaries);
        if (viewModelLocator is null)
        {
            throw new InvalidOperationException($"The {nameof(AbstractViewModelLocatorHost<TViewModelLocator>)} is not present in ResourceDictionary. Add <locator:ViewModelLocatorHost x:Key=\"{DefaultLocatorName}\"/> in {applicationInstance.GetType().Name}.xaml.");
        }

        return viewModelLocator;
    }

    /// <summary>
    /// Searches in <see cref="ResourceDictionary"/> for <see cref="AbstractViewModelLocatorHost{TViewModelLocator}"/>.
    /// </summary>
    /// <typeparam name="TApplication">WPF <see cref="Application" />.</typeparam>
    /// <param name="applicationInstance">Instance of <see cref="Application" />.</param>
    /// <param name="locatorName">Key in ResourceDictionary.</param>
    /// <exception cref="InvalidOperationException">Throws if <see cref="AbstractViewModelLocatorHost{TViewModelLocator}"/> is not found in resource.</exception>
#pragma warning disable CA1000
    public static AbstractViewModelLocatorHost<TViewModelLocator> GetInstance<TApplication>(TApplication applicationInstance, string locatorName)
#pragma warning restore CA1000
        where TApplication : Application
    {
        ThrowHelper.ThrowIfNull(applicationInstance, nameof(applicationInstance));

        var resource = applicationInstance.Resources[locatorName];

        if (resource is null)
        {
            throw new InvalidOperationException($"Key '{locatorName}' is not found in ResourceDictionary. Add <locator:ViewModelLocatorHost x:Key=\"{locatorName}\"/> in {applicationInstance.GetType().Name}.xaml.");
        }

        if (resource is not AbstractViewModelLocatorHost<TViewModelLocator> viewModelLocator)
        {
            throw new InvalidOperationException($"Type '{resource.GetType()}' doesn't implement {nameof(AbstractViewModelLocatorHost<TViewModelLocator>)}.");
        }

        return viewModelLocator;
    }

    private TViewModelLocator? _viewModelLocator;

    public TViewModelLocator ViewModelLocator
    {
        get
        {
            if (_viewModelLocator is null)
            {
                throw new InvalidOperationException("Please add IViewModelLocatorInitialization<DIContainer> to your App WPF class. \n" +
                                                    "Then in Initialize(viewModelLocator) add `var viewModelLocatorHost = ViewModelLocatorHost.GetInstance(this) [ViewModelLocatorHost should implement AbstractViewModelLocatorHost<DIContainer>]`\n" +
                                                    " and `viewModelLocatorHost.SetViewModelLocator(viewModelLocator);`");
            }

            return _viewModelLocator;
        }
    }

    public void SetViewModelLocator(TViewModelLocator viewModelLocator)
    {
        _viewModelLocator = viewModelLocator;
    }

    private static AbstractViewModelLocatorHost<TViewModelLocator>? DefaultLocator<TApplication>(TApplication applicationInstance) where TApplication : Application
    {
        if (applicationInstance.Resources[DefaultLocatorName] is AbstractViewModelLocatorHost<TViewModelLocator> viewModelLocator)
        {
            return viewModelLocator;
        }

        return null;
    }

    private static AbstractViewModelLocatorHost<TViewModelLocator>? FindFromResource<TApplication>(TApplication applicationInstance, bool skipMergedDictionaries = false)
        where TApplication : Application
    {
        var collection = new List<ResourceDictionary> { applicationInstance.Resources };
        //Look in merged dictionaries too just in case someone adds it there.
        if (!skipMergedDictionaries)
        {
            collection.AddRange(applicationInstance.Resources.MergedDictionaries);
        }

        return FindFromResource(collection);
    }

    private static AbstractViewModelLocatorHost<TViewModelLocator>? FindFromResource(IEnumerable<ResourceDictionary> dictionaries)
    {
        foreach (ResourceDictionary dictionary in dictionaries)
        {
            foreach (var key in dictionary.Keys)
            {
                if (key is not null)
                {
                    if (dictionary[key] is AbstractViewModelLocatorHost<TViewModelLocator> viewModelLocator)
                    {
                        return viewModelLocator;
                    }
                }
            }
        }

        return null;
    }
}