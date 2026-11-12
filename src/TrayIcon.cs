using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacKey
{
    internal sealed class TrayIcon : IDisposable
    {
        private readonly AppController _app;
        private readonly NotifyIcon _icon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _macItem;
        private readonly ToolStripMenuItem _appleItem;
        private readonly ToolStripMenuItem _pcItem;
        private readonly ToolStripMenuItem _extraItem;
        private readonly ToolStripMenuItem _capsItem;
        private readonly ToolStripMenuItem _startupItem;

        public TrayIcon(AppController app)
        {
            _app = app;

            _icon = new NotifyIcon();
            _icon.Icon = AppIcon.Shared;
            _icon.Text = "MacKey";
            _icon.Visible = true;
            _icon.DoubleClick += delegate { _app.ShowForm(); };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = new Font("Segoe UI", 9f);
            menu.Renderer = new ThemeMenuRenderer();
            menu.BackColor = Theme.Card;
            menu.ForeColor = Theme.Text;

            ToolStripMenuItem open = new ToolStripMenuItem("Open MacKey");
            open.Font = new Font("Segoe UI Semibold", 9f);
            open.Click += delegate { _app.ShowForm(); };
            menu.Items.Add(open);
            menu.Items.Add(new ToolStripSeparator());

            _macItem = new ToolStripMenuItem("Mac Mode");
            _macItem.Click += delegate { _app.SetMacMode(!_app.Settings.MacMode); };
            menu.Items.Add(_macItem);

            ToolStripMenuItem keyboard = new ToolStripMenuItem("Keyboard layout");
            _appleItem = new ToolStripMenuItem("Apple keyboard");
            _appleItem.Click += delegate { _app.SetLayout(false); };
            _pcItem = new ToolStripMenuItem("PC keyboard (swap Alt / \u2318)");
            _pcItem.Click += delegate { _app.SetLayout(true); };
            keyboard.DropDownItems.Add(_appleItem);
            keyboard.DropDownItems.Add(_pcItem);
            menu.Items.Add(keyboard);

            menu.Items.Add(new ToolStripSeparator());

            _extraItem = new ToolStripMenuItem("Extra Mac shortcuts");
            _extraItem.Click += delegate
            {
                _app.SetExtraShortcuts(!_app.Settings.ExtraShortcuts);
            };
            menu.Items.Add(_extraItem);

            _capsItem = new ToolStripMenuItem("Caps Lock = Windows key");
            _capsItem.Click += delegate
            {
                _app.SetCapsLockAsWin(!_app.Settings.CapsLockAsWin);
            };
            menu.Items.Add(_capsItem);

            _startupItem = new ToolStripMenuItem("Start with Windows");
            _startupItem.Click += delegate
            {
                _app.SetStartWithWindows(!_app.Settings.StartWithWindows);
            };
            menu.Items.Add(_startupItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem admin = new ToolStripMenuItem("Restart as administrator\u2026");
            admin.Click += delegate { _app.RestartAsAdmin(); };
            menu.Items.Add(admin);

            ToolStripMenuItem exit = new ToolStripMenuItem("Exit");
            exit.Click += delegate { _app.ExitApp(); };
            menu.Items.Add(exit);

            _icon.ContextMenuStrip = menu;
            _menu = menu;
            Theme.Changed += OnThemeChanged;
            Sync();
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            _menu.BackColor = Theme.Card;
            _menu.ForeColor = Theme.Text;
            _menu.Refresh();
        }

        public void Sync()
        {
            _macItem.Checked = _app.Settings.MacMode;
            _appleItem.Checked = !_app.Settings.PcLayout;
            _pcItem.Checked = _app.Settings.PcLayout;
            _extraItem.Checked = _app.Settings.ExtraShortcuts;
            _capsItem.Checked = _app.Settings.CapsLockAsWin;
            _startupItem.Checked = _app.Settings.StartWithWindows;

            string text = _app.Settings.MacMode
                ? "MacKey \u2014 Mac Mode on"
                : "MacKey \u2014 Mac Mode off";
            if (text.Length > 63) text = text.Substring(0, 63);
            _icon.Text = text;
        }

        public void ShowBalloon(string title, string text)
        {
            try
            {
                _icon.ShowBalloonTip(4000, title, text, ToolTipIcon.Info);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            Theme.Changed -= OnThemeChanged;
            _icon.Visible = false;
            _icon.Dispose();
            _menu.Dispose();
        }
    }
}
