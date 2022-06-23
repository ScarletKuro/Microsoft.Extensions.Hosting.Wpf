﻿using System;
using System.Reflection;
using System.Windows;

namespace Microsoft.Extensions.Hosting.Wpf;

internal static class ApplicationExtensions
{
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