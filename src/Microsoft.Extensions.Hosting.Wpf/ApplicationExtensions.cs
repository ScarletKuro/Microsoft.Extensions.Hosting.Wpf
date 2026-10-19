using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace Microsoft.Extensions.Hosting.Wpf;

public static class ApplicationExtensions
{
    /// <summary>
    /// This method checks if constructors are configured incorrectly.
    /// </summary>
    /// <param name="application">WPF <see cref="Application" />.</param>
    /// <exception cref="InvalidOperationException">Throws exception if something was configured incorrectly.</exception>
    public static void CheckForInvalidConstructorConfiguration(this Application application)
    {
        ThrowHelper.ThrowIfNull(application, nameof(application));

        var applicationType = application.GetType();
        var constructors = applicationType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //We only care when there are more than one constructor.
        if (constructors.Length > 1)
        {
            foreach (var constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    if (!constructor.IsPrivate)
                    {
                        throw new InvalidOperationException($"You need to have a parameter-less PRIVATE constructor if you have multiple constructor in {applicationType.FullName}.");
                    }
                }
            }
        }
    }

    internal static void InvokeIfRequired(this Dispatcher dispatcher, Action action)
    {
        if (!dispatcher.CheckAccess())
        {
            dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    internal static void CloseAllWindowsIfAny<TApplication>(this TApplication? application)
        where TApplication : Application
    {
        if (application is null)
        {
            return;
        }

        foreach (var window in application.Windows)
        {
            if (window is Window wpfWindow)
            {
                wpfWindow.Close();
            }
        }
    }
}