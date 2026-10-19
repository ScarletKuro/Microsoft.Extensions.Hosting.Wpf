using System.Runtime.CompilerServices;
using System.Windows;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

public sealed class ApplicationExtensionsTests
{
    [Theory]
    [InlineData(typeof(DefaultConstructorApplication))]
    [InlineData(typeof(InjectedApplication))]
    [InlineData(typeof(PrivateParameterlessApplication))]
    public void ValidConstructorsAreAccepted(Type applicationType)
    {
        CreateWithoutConstructor(applicationType).CheckForInvalidConstructorConfiguration();
    }

    [Theory]
    [InlineData(typeof(PublicParameterlessApplication))]
    [InlineData(typeof(ProtectedParameterlessApplication))]
    public void NonPrivateParameterlessConstructorNextToAnotherIsRejected(Type applicationType)
    {
        var application = CreateWithoutConstructor(applicationType);

        var exception = Assert.Throws<InvalidOperationException>(application.CheckForInvalidConstructorConfiguration);
        Assert.Contains(applicationType.FullName!, exception.Message);
    }

    [Fact]
    public void NullApplicationIsRejected()
    {
        Assert.Throws<ArgumentNullException>("application", () => ((Application)null!).CheckForInvalidConstructorConfiguration());
    }

    // Only the constructors are inspected; skipping them avoids creating a second WPF Application (one per AppDomain).
    private static Application CreateWithoutConstructor(Type applicationType) =>
        (Application)RuntimeHelpers.GetUninitializedObject(applicationType);

    public sealed class Dependency;

    public sealed class DefaultConstructorApplication : Application;

    public sealed class InjectedApplication : Application
    {
        public InjectedApplication(Dependency dependency)
        {
            _ = dependency;
        }
    }

    public sealed class PrivateParameterlessApplication : Application
    {
        private PrivateParameterlessApplication()
        {
        }

        public PrivateParameterlessApplication(Dependency dependency)
        {
            _ = dependency;
        }
    }

    public sealed class PublicParameterlessApplication : Application
    {
        public PublicParameterlessApplication()
        {
        }

        public PublicParameterlessApplication(Dependency dependency)
        {
            _ = dependency;
        }
    }

    public class ProtectedParameterlessApplication : Application
    {
        protected ProtectedParameterlessApplication()
        {
        }

        public ProtectedParameterlessApplication(Dependency dependency)
        {
            _ = dependency;
        }
    }
}
