using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Wpf;
using Microsoft.Extensions.Hosting.Wpf.Core;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWpf<App>();
builder.Services.AddWpfLifetime();

using var host = builder.Build();
host.Run();

public sealed class App : Application, IApplicationInitializeComponent
{
    public void InitializeComponent()
    {
        Startup += (_, _) => new Window
        {
            Title = "WPF with HostApplicationBuilder",
            Width = 480,
            Height = 240,
            Content = new TextBlock
            {
                Text = "Hello from a hosted WPF application!",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        }.Show();
    }
}
