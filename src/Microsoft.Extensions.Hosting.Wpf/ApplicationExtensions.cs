using System;
using System.Reflection;
using System.Windows;

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

    /// <summary>
    /// Checks if WPF application is already shutdown.
    /// </summary>
    /// <param name="instance">Instance of WPF <see cref="Application"/></param>
    /// <returns>When WPF application is shutdown return <c>true</c></returns>
    internal static bool IsWpfShutdown<TApplication>(this TApplication instance)
        where TApplication : Application
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        FieldInfo? field = typeof(Application).GetField("_appIsShutdown", bindFlags);

        return (bool)(field?.GetValue(instance) ?? false);
    }

    internal static void InvokeIfRequired<TApplication>(this TApplication instance, Action action)
        where TApplication : Application
    {
        if (!instance.CheckAccess())
        {
            instance.Dispatcher.Invoke(action);
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