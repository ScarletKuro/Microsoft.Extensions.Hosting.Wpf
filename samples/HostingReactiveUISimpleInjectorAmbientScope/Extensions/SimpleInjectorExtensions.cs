using SimpleInjector.Diagnostics;
using SimpleInjector;
using System;

namespace HostingReactiveUISimpleInjectorAmbientScope.Extensions
{
    public static class SimpleInjectorExtensions
    {
        public static void RegisterScopeDisposableTransient<TImplementation>(this Container container) where TImplementation : class, IDisposable
        {
            RegisterScopeDisposableTransient<TImplementation, TImplementation>(container);
        }

        // https://docs.simpleinjector.org/en/latest/disposabletransientcomponent.html
        public static void RegisterScopeDisposableTransient<TService, TImplementation>(this Container container) where TImplementation : class, IDisposable, TService where TService : class
        {
            ScopedLifestyle lifestyle = Lifestyle.Scoped;
            Registration registration = Lifestyle.Transient.CreateRegistration<TImplementation>(container);
            registration.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "Dispose is called by the end of scope.");
            container.AddRegistration(typeof(TService), registration);
            container.RegisterInitializer<TImplementation>(implementation => lifestyle.RegisterForDisposal(container, implementation));
        }
    }
}
