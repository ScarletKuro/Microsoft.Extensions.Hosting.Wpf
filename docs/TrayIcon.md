# Microsoft.Extensions.Hosting.Wpf.TrayIcon
Extension library for `Microsoft.Extensions.Hosting.Wpf`. This adds support to use tray icon with `Microsoft.Extensions.Hosting.Wpf`.

## Getting Started

### Install nuget
```Install-Package Extensions.Hosting.Wpf.TrayIcon```

### Register service
```CSharp
private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
{
    services.AddWpf<App>();
    services.AddWpfTrayIcon<TrayIcon>(); //<--- new line
}
```

## Example TrayIcon implementation

```CSharp
public class TrayIcon : ITrayIcon
{
	private NotifyIcon? _notifyIcon;
	private ContextMenuStrip? _contextMenu;
	private ToolStripMenuItem? _exitItem;
	private ToolStripMenuItem? _versionItem;
	private IContainer? _components;

	public IWpfThread WpfThread { get; }

	public TrayIcon(IWpfThread wpfThread)
	{
		WpfThread = wpfThread;
	}

	public void InitializeComponent()
	{
		_components = new Container();
		_contextMenu = new ContextMenuStrip();
		_exitItem = new ToolStripMenuItem();
		_versionItem = new ToolStripMenuItem();

		//Initialize exitItem
		_exitItem.Text = "E&xit";
		_exitItem.Click += ExitItemOnClick;

		//Initialize versionItem
		_versionItem.Enabled = false;
		_versionItem.Text = Application.ProductVersion;

		// Initialize contextMenu
		_contextMenu.Items.AddRange(new ToolStripItem[] { _versionItem, _exitItem });

		// Create the NotifyIcon.
		_notifyIcon = new NotifyIcon(_components);

		// The Icon property sets the icon that will appear
		// in the systray for this application.
		_notifyIcon.Icon = Resources.Dog;

		// The ContextMenu property sets the menu that will
		// appear when the systray icon is right clicked.
		_notifyIcon.ContextMenuStrip = _contextMenu;

		// The Text property sets the text that will be displayed,
		// in a tooltip, when the mouse hovers over the systray icon.
		_notifyIcon.Text = $@"{Application.ProductVersion}";
		_notifyIcon.Visible = true;
	}

	private void ExitItemOnClick(object? sender, EventArgs e)
	{
		//Shutdown entire application in a proper way.
		WpfThread.HandleApplicationExit();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_notifyIcon?.Dispose();

			_versionItem?.Dispose();
			_contextMenu?.Dispose();
			_exitItem?.Dispose();
			_components?.Dispose();
		}
	}
}
```
