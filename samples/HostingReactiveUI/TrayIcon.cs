using System;
using System.ComponentModel;
using System.Windows.Forms;
using HostingReactiveUI.Properties;
using Microsoft.Extensions.Hosting.Wpf.Core;
using Microsoft.Extensions.Hosting.Wpf.TrayIcon;

namespace HostingReactiveUI
{
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
            // Close the form, which closes the application.
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
}
