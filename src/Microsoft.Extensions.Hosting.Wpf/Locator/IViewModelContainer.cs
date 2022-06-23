namespace Microsoft.Extensions.Hosting.Wpf.Locator;

public interface IViewModelContainer
{
    T GetService<T>() where T : class;
}