using System.Windows;
using Microsoft.Extensions.Hosting.Wpf.Locator;
using Xunit;

namespace Microsoft.Extensions.Hosting.Wpf.Tests;

[Collection(WpfApplicationCollection.Name)]
public sealed class ViewModelLocatorHostTests
{
    private readonly WpfApplicationFixture _fixture;

    public ViewModelLocatorHostTests(WpfApplicationFixture fixture)
    {
        _fixture = fixture;
        // Every test starts with empty application resources.
        _fixture.Invoke(application => application.Resources = new ResourceDictionary());
    }

    [Fact]
    public void FindsHostUnderDefaultKey()
    {
        var host = new LocatorHost();
        _fixture.Invoke(application =>
        {
            application.Resources.Add("Other", "not a locator host");
            application.Resources.Add("Locator", host);

            Assert.Same(host, LocatorHost.GetInstance(application));
            Assert.Same(host, LocatorHost.GetInstance());
        });
    }

    [Fact]
    public void FindsHostUnderAnyKey()
    {
        var host = new LocatorHost();
        _fixture.Invoke(application =>
        {
            // A host for another locator type under the default key is skipped.
            application.Resources.Add("Locator", new OtherLocatorHost());
            application.Resources.Add("Custom", host);

            Assert.Same(host, LocatorHost.GetInstance(application));
            Assert.Same(host, LocatorHost.GetInstance(application, skipMergedDictionaries: true));
        });
    }

    [Fact]
    public void FindsHostInMergedDictionaryUnlessSkipped()
    {
        var host = new LocatorHost();
        _fixture.Invoke(application =>
        {
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { ["Custom"] = host });

            Assert.Same(host, LocatorHost.GetInstance(application));
            Assert.Same(host, LocatorHost.GetInstance());
            var exception = Assert.Throws<InvalidOperationException>(() => LocatorHost.GetInstance(application, skipMergedDictionaries: true));
            Assert.Contains("is not present in ResourceDictionary", exception.Message);
            Assert.Throws<InvalidOperationException>(() => LocatorHost.GetInstance(skipMergedDictionaries: true));
        });
    }

    [Fact]
    public void MissingHostIsReported()
    {
        _fixture.Invoke(application =>
        {
            application.Resources.Add("Locator", new OtherLocatorHost());

            var exception = Assert.Throws<InvalidOperationException>(() => LocatorHost.GetInstance(application));
            Assert.Contains("<locator:ViewModelLocatorHost x:Key=\"Locator\"/>", exception.Message);
            Assert.Contains(nameof(Application), exception.Message);
        });
    }

    [Fact]
    public void FindsHostByKey()
    {
        var host = new LocatorHost();
        _fixture.Invoke(application =>
        {
            application.Resources.Add("Custom", host);

            Assert.Same(host, LocatorHost.GetInstance(application, "Custom"));
            Assert.Same(host, LocatorHost.GetInstance("Custom"));
        });
    }

    [Fact]
    public void MissingOrWrongKeyIsReported()
    {
        _fixture.Invoke(application =>
        {
            application.Resources.Add("Other", new OtherLocatorHost());

            var missing = Assert.Throws<InvalidOperationException>(() => LocatorHost.GetInstance(application, "Missing"));
            Assert.Contains("Key 'Missing' is not found", missing.Message);
            Assert.Throws<InvalidOperationException>(() => LocatorHost.GetInstance("Missing"));

            var wrongType = Assert.Throws<InvalidOperationException>(() => LocatorHost.GetInstance(application, "Other"));
            Assert.Contains($"Type '{typeof(OtherLocatorHost)}' doesn't implement", wrongType.Message);
        });
    }

    [Fact]
    public void NullApplicationIsRejected()
    {
        Assert.Throws<ArgumentNullException>("applicationInstance", () => LocatorHost.GetInstance<Application>(null!));
        Assert.Throws<ArgumentNullException>("applicationInstance", () => LocatorHost.GetInstance<Application>(null!, "Locator"));
    }

    [Fact]
    public void ViewModelLocatorMustBeSetBeforeUse()
    {
        var host = new LocatorHost();

        var exception = Assert.Throws<InvalidOperationException>(() => host.ViewModelLocator);
        Assert.Contains($"IViewModelLocatorInitialization<{nameof(TestLocator)}>", exception.Message);

        var locator = new TestLocator();
        host.SetViewModelLocator(locator);
        Assert.Same(locator, host.ViewModelLocator);
    }

    public sealed class TestLocator;

    public sealed class LocatorHost : AbstractViewModelLocatorHost<TestLocator>;

    public sealed class OtherLocatorHost : AbstractViewModelLocatorHost<string>;
}
