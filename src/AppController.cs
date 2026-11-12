using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MacKey
{
    internal sealed class AppController : ApplicationContext
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValue = "MacKey";

        public Settings Settings { get; private set; }
        public bool Exiting { get; private set; }

        private KeyboardEngine _engine;
        private MainForm _form;
        private TrayIcon _tray;
        private bool _minimizeNotified;
        private bool _closeNotified;

        public AppController(bool startHidden)
        {
            Settings = Settings.Load();

            _engine = new KeyboardEngine();
            _engine.MacMode = Settings.MacMode;
            _engine.PcLayout = Settings.PcLayout;
            _engine.ExtraShortcuts = Settings.ExtraShortcuts;
            _engine.CapsLockAsWin = Settings.CapsLockAsWin;
            _engine.ToggleHotkey += delegate
            {
                try
                {
                    if (_form != null && _form.IsHandleCreated)
                        _form.BeginInvoke(new Action(ToggleMacMode));
                }
                catch
                {
                }
            };

            bool installed = _engine.Install();

            _form = new MainForm(this);
            IntPtr handle = _form.Handle; // create the window handle early

            _tray = new TrayIcon(this);

            if (!Settings.StartWithWindows)
            {
                // Make sure a stale Run entry never survives a manual opt-out.
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                    {
                        if (key != null) key.DeleteValue(RunValue, false);
                    }
                }
                catch
                {
                }
            }
            else
            {
                ApplyAutostart(true);
            }

            if (!startHidden || !Settings.SeenWelcome)
            {
                _form.Show();
                _form.Activate();
            }

            if (!Settings.SeenWelcome)
            {
                Settings.SeenWelcome = true;
                Settings.Save();
                _tray.ShowBalloon(
                    "MacKey is ready",
                    "Turn on Mac Mode with the switch, or press Ctrl+Alt+M.");
            }

            if (!installed)
            {
                MessageBox.Show(
                    "MacKey could not install its keyboard hook.\n\n" +
                    "Try restarting the app, or run it as administrator.",
                    "MacKey",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        public void ToggleMacMode()
        {
            SetMacMode(!Settings.MacMode);
        }

        public void SetMacMode(bool on)
        {
            Settings.MacMode = on;
            _engine.MacMode = on;
            if (!on) _engine.ResetState();
            Settings.Save();

            _form.SyncFromSettings();
            _tray.Sync();

            if (on && !_form.Visible)
            {
                _tray.ShowBalloon(
                    "Mac Mode on",
                    "\u2318 now works like Ctrl. Press Ctrl+Alt+M to turn it off.");
            }
        }

        public void SetLayout(bool pc)
        {
            if (Settings.PcLayout == pc) return;

            Settings.PcLayout = pc;
            _engine.PcLayout = pc;
            _engine.ResetState();
            Settings.Save();

            _form.SyncFromSettings();
            _tray.Sync();
        }

        public void SetExtraShortcuts(bool on)
        {
            Settings.ExtraShortcuts = on;
            _engine.ExtraShortcuts = on;
            _engine.ResetState();
            Settings.Save();

            _form.SyncFromSettings();
            _tray.Sync();
        }

        public void SetCapsLockAsWin(bool on)
        {
            Settings.CapsLockAsWin = on;
            _engine.CapsLockAsWin = on;
            _engine.ResetState();
            Settings.Save();

            _form.SyncFromSettings();
            _tray.Sync();
        }

        public void SetStartWithWindows(bool on)
        {
            Settings.StartWithWindows = on;
            ApplyAutostart(on);
            Settings.Save();

            _form.SyncFromSettings();
            _tray.Sync();
        }

        private static void ApplyAutostart(bool on)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null) return;
                    if (on)
                    {
                        key.SetValue(
                            RunValue,
                            "\"" + Application.ExecutablePath + "\" --tray");
                    }
                    else
                    {
                        key.DeleteValue(RunValue, false);
                    }
                }
            }
            catch
            {
                // Group policy or a locked-down profile can block this.
            }
        }

        public void ShowForm()
        {
            if (_form == null) return;
            _form.ShowFromTray();
        }

        public void NotifyMinimized()
        {
            if (_minimizeNotified || _tray == null) return;
            _minimizeNotified = true;
            _tray.ShowBalloon(
                "MacKey is still running",
                "Click the tray icon to reopen it.");
        }

        public void NotifyClosed()
        {
            if (_closeNotified || _tray == null) return;
            _closeNotified = true;
            _tray.ShowBalloon(
                "MacKey is still running",
                "Mac Mode stays active. Use the tray icon to reopen it.");
        }

        public void RestartAsAdmin()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(
                    Application.ExecutablePath, "--tray --replace");
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
            }
            catch
            {
                // The user declined the UAC prompt.
                return;
            }
            ExitApp();
        }

        public void ExitApp()
        {
            if (Exiting) return;
            Exiting = true;

            if (_engine != null)
            {
                _engine.ResetState();
                _engine.Dispose();
                _engine = null;
            }
            if (_tray != null)
            {
                _tray.Dispose();
                _tray = null;
            }
            if (_form != null)
            {
                _form.Dispose();
                _form = null;
            }
            ExitThread();
        }
    }
}
