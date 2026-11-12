using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MacKey
{
    internal sealed class MainForm : Form
    {
        private readonly AppController _app;
        private MacToggle _toggle;
        private ChoiceCard _appleCard;
        private ChoiceCard _pcCard;
        private ToggleRow _extraRow;
        private ToggleRow _capsRow;
        private ToggleRow _startupRow;
        private Label _status;
        private TextBox _testBox;
        private Panel _header;
        private bool _syncing;

        public MainForm(AppController app)
        {
            _app = app;
            Theme.Apply(Theme.SystemPrefersDark());

            Text = "MacKey";
            Icon = AppIcon.Shared;
            ClientSize = new Size(520, 640);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            DoubleBuffered = true;

            BuildUi();
            ApplyThemeColors();
            SyncFromSettings();
            Theme.Changed += OnThemeChanged;
        }

        private void BuildUi()
        {
            Panel header = new Panel();
            _header = header;
            header.Bounds = new Rectangle(0, 0, ClientSize.Width, 84);
            header.BackColor = Theme.Header;
            header.Paint += HeaderPaint;
            Controls.Add(header);

            Label title = new Label();
            title.Text = "MacKey";
            title.Font = new Font("Segoe UI Semibold", 17f);
            title.ForeColor = Theme.Text;
            title.AutoSize = true;
            title.BackColor = Theme.Header;
            title.Tag = Ink.Text;
            title.Location = new Point(80, 14);
            header.Controls.Add(title);

            Label sub = new Label();
            sub.Text = "Mac shortcuts on Windows";
            sub.Font = new Font("Segoe UI", 9f);
            sub.ForeColor = Theme.Muted;
            sub.AutoSize = true;
            sub.BackColor = Theme.Header;
            sub.Tag = Ink.Muted;
            sub.Location = new Point(82, 45);
            header.Controls.Add(sub);

            // ---- Mac Mode card -------------------------------------------------
            CardPanel power = new CardPanel();
            power.Bounds = new Rectangle(24, 100, 472, 96);
            Controls.Add(power);

            Label powerTitle = new Label();
            powerTitle.Text = "Mac Mode";
            powerTitle.Font = new Font("Segoe UI Semibold", 13f);
            powerTitle.ForeColor = Theme.Text;
            powerTitle.AutoSize = true;
            powerTitle.BackColor = Theme.Card;
            powerTitle.Tag = Ink.Text;
            powerTitle.Location = new Point(18, 13);
            power.Controls.Add(powerTitle);

            _status = new Label();
            _status.Text = "Off \u2014 Windows shortcuts unchanged";
            _status.Font = new Font("Segoe UI", 9.5f);
            _status.ForeColor = Theme.Muted;
            _status.AutoSize = true;
            _status.BackColor = Theme.Card;
            _status.Location = new Point(18, 41);
            power.Controls.Add(_status);

            Label hotkey = new Label();
            hotkey.Text = "Toggle with Ctrl + Alt + M";
            hotkey.Font = new Font("Segoe UI", 8.5f);
            hotkey.ForeColor = Theme.Faint;
            hotkey.AutoSize = true;
            hotkey.BackColor = Theme.Card;
            hotkey.Tag = Ink.Faint;
            hotkey.Location = new Point(18, 63);
            power.Controls.Add(hotkey);

            _toggle = new MacToggle();
            _toggle.Size = new Size(60, 34);
            _toggle.Location = new Point(power.Width - 18 - 60, 31);
            _toggle.BackColor = Theme.Card;
            _toggle.CheckedChanged += delegate
            {
                if (!_syncing) _app.SetMacMode(_toggle.Checked);
            };
            power.Controls.Add(_toggle);

            EventHandler powerClick = delegate { _toggle.Checked = !_toggle.Checked; };
            power.Click += powerClick;
            powerTitle.Click += powerClick;
            _status.Click += powerClick;
            hotkey.Click += powerClick;
            power.Cursor = Cursors.Hand;

            // ---- Keyboard choice ----------------------------------------------
            Controls.Add(SectionLabel("Keyboard", 26, 214));

            _appleCard = new ChoiceCard("Apple keyboard", "\u2318 sits next to Space");
            _appleCard.Bounds = new Rectangle(24, 236, 228, 68);
            _appleCard.SelectedChanged += delegate
            {
                if (!_syncing && _appleCard.Selected) _app.SetLayout(false);
            };
            Controls.Add(_appleCard);

            _pcCard = new ChoiceCard("PC keyboard", "Swaps Alt and \u2318");
            _pcCard.Bounds = new Rectangle(268, 236, 228, 68);
            _pcCard.SelectedChanged += delegate
            {
                if (!_syncing && _pcCard.Selected) _app.SetLayout(true);
            };
            Controls.Add(_pcCard);

            // ---- Behavior -----------------------------------------------------
            Controls.Add(SectionLabel("Behavior", 26, 320));

            CardPanel group = new CardPanel();
            group.Bounds = new Rectangle(24, 342, 472, 162);
            group.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawLine(pen, 16, 53, group.Width - 16, 53);
                    e.Graphics.DrawLine(pen, 16, 107, group.Width - 16, 107);
                }
            };
            Controls.Add(group);

            _extraRow = new ToggleRow(
                "Extra Mac shortcuts",
                "\u2318Tab, \u2318Q, \u2318Space, screenshots");
            _extraRow.Bounds = new Rectangle(0, 0, 472, 54);
            _extraRow.CheckedChanged += delegate
            {
                if (!_syncing) _app.SetExtraShortcuts(_extraRow.Checked);
            };
            group.Controls.Add(_extraRow);

            _capsRow = new ToggleRow(
                "Caps Lock = Windows key",
                "Keep real Win+ shortcuts within reach");
            _capsRow.Bounds = new Rectangle(0, 54, 472, 54);
            _capsRow.CheckedChanged += delegate
            {
                if (!_syncing) _app.SetCapsLockAsWin(_capsRow.Checked);
            };
            group.Controls.Add(_capsRow);

            _startupRow = new ToggleRow(
                "Start with Windows",
                "Launch quietly in the tray");
            _startupRow.Bounds = new Rectangle(0, 108, 472, 54);
            _startupRow.CheckedChanged += delegate
            {
                if (!_syncing) _app.SetStartWithWindows(_startupRow.Checked);
            };
            group.Controls.Add(_startupRow);

            // ---- Try it -------------------------------------------------------
            CardPanel testCard = new CardPanel();
            testCard.Bounds = new Rectangle(24, 516, 472, 88);
            Controls.Add(testCard);

            Label tryLabel = new Label();
            tryLabel.Text = "Try it";
            tryLabel.Font = new Font("Segoe UI Semibold", 9.5f);
            tryLabel.ForeColor = Theme.Text;
            tryLabel.AutoSize = true;
            tryLabel.BackColor = Theme.Card;
            tryLabel.Tag = Ink.Text;
            tryLabel.Location = new Point(16, 10);
            testCard.Controls.Add(tryLabel);

            Label tryHint = new Label();
            tryHint.Text = "Click the box, then press \u2318C, \u2318V, \u2318Z, \u2318A\u2026";
            tryHint.Font = new Font("Segoe UI", 8.5f);
            tryHint.ForeColor = Theme.Muted;
            tryHint.AutoSize = true;
            tryHint.BackColor = Theme.Card;
            tryHint.Tag = Ink.Muted;
            tryHint.Location = new Point(16, 29);
            testCard.Controls.Add(tryHint);

            _testBox = new TextBox();
            _testBox.BorderStyle = BorderStyle.FixedSingle;
            _testBox.Font = new Font("Segoe UI", 10.5f);
            _testBox.Location = new Point(16, 51);
            _testBox.Size = new Size(440, 26);
            _testBox.BackColor = Theme.Field;
            _testBox.ForeColor = Theme.Text;
            testCard.Controls.Add(_testBox);

            // ---- Footer -------------------------------------------------------
            Label footer = new Label();
            footer.Text = "\u2303\u2325M toggles Mac Mode";
            footer.Font = new Font("Segoe UI", 8.5f);
            footer.ForeColor = Theme.Muted;
            footer.AutoSize = true;
            footer.BackColor = Color.Transparent;
            footer.Tag = Ink.Muted;
            footer.Location = new Point(26, 620);
            Controls.Add(footer);

            LinkLabel all = new LinkLabel();
            all.Text = "All shortcuts";
            all.Font = new Font("Segoe UI", 8.5f);
            all.LinkColor = Theme.Accent;
            all.ActiveLinkColor = Theme.Blend(Theme.Accent, Color.Black, 0.25f);
            all.LinkBehavior = LinkBehavior.HoverUnderline;
            all.AutoSize = true;
            all.BackColor = Color.Transparent;
            all.Location = new Point(ClientSize.Width - 26 - all.PreferredWidth, 620);
            all.LinkClicked += delegate
            {
                using (ShortcutsForm f = new ShortcutsForm())
                    f.ShowDialog(this);
            };
            Controls.Add(all);
        }

        private static Label SectionLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Segoe UI Semibold", 9f);
            l.ForeColor = Theme.Muted;
            l.AutoSize = true;
            l.BackColor = Color.Transparent;
            l.Tag = Ink.Muted;
            l.Location = new Point(x, y);
            return l;
        }

        private void HeaderPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float s = DeviceDpi / 96f;
            Rectangle r = new Rectangle(
                (int)(24 * s), (int)(20 * s), (int)(44 * s), (int)(44 * s));
            using (GraphicsPath path = Theme.RoundedRect(r, (int)(12 * s)))
            using (LinearGradientBrush brush = new LinearGradientBrush(
                r, Color.FromArgb(10, 132, 255), Color.FromArgb(64, 90, 255), 45f))
            {
                g.FillPath(brush, path);
            }

            using (Font font = new Font("Segoe UI Symbol", 21f))
            using (StringFormat fmt = new StringFormat())
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                fmt.Alignment = StringAlignment.Center;
                fmt.LineAlignment = StringAlignment.Center;
                g.DrawString(
                    "\u2318", font, brush,
                    new RectangleF(r.X, r.Y - s, r.Width, r.Height), fmt);
            }

            using (Pen pen = new Pen(Theme.Border))
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }

        /// <summary>Re-colours every child control from the current palette.</summary>
        private void ApplyThemeColors()
        {
            BackColor = Theme.Bg;
            if (_header != null) _header.BackColor = Theme.Header;
            ApplyThemeColors(this);
        }

        private static void ApplyThemeColors(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is LinkLabel)
                {
                    LinkLabel link = (LinkLabel)c;
                    link.LinkColor = Theme.Accent;
                    link.ActiveLinkColor = Theme.Blend(Theme.Accent, Color.Black, 0.25f);
                }
                else if (c is Label)
                {
                    Label label = (Label)c;
                    if (label.Tag is Ink) label.ForeColor = Theme.ForInk((Ink)label.Tag);
                    if (label.BackColor != Color.Transparent)
                        label.BackColor = parent.BackColor;
                }
                else if (c is TextBox)
                {
                    c.BackColor = Theme.Field;
                    c.ForeColor = Theme.Text;
                }
                else if (c is CardPanel || c is MacToggle ||
                         c is ChoiceCard || c is ToggleRow)
                {
                    c.BackColor = Theme.Card;
                }
                else if (c is Panel)
                {
                    c.BackColor = Theme.Header;
                }

                ApplyThemeColors(c);
            }
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;

            ApplyThemeColors();
            Theme.ApplyTitleBar(Handle);
            SyncFromSettings();
            Invalidate(true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(Handle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }

        public void SyncFromSettings()
        {
            _syncing = true;
            try
            {
                _toggle.Checked = _app.Settings.MacMode;
                _appleCard.Selected = !_app.Settings.PcLayout;
                _pcCard.Selected = _app.Settings.PcLayout;
                _extraRow.Checked = _app.Settings.ExtraShortcuts;
                _capsRow.Checked = _app.Settings.CapsLockAsWin;
                _startupRow.Checked = _app.Settings.StartWithWindows;

                if (_app.Settings.MacMode)
                {
                    _status.Text = _app.Settings.PcLayout
                        ? "On \u2014 Alt next to Space acts like \u2318"
                        : "On \u2014 \u2318 now works like Ctrl";
                    _status.ForeColor = Theme.Green;
                }
                else
                {
                    _status.Text = "Off \u2014 Windows shortcuts unchanged";
                    _status.ForeColor = Theme.Muted;
                }
            }
            finally
            {
                _syncing = false;
            }
        }

        public void ShowFromTray()
        {
            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Normal;

            Activate();
            BringToFront();
            TopMost = true;
            TopMost = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState != FormWindowState.Minimized || _app == null || _app.Exiting)
                return;

            try
            {
                BeginInvoke(new Action(delegate
                {
                    Hide();
                    _app.NotifyMinimized();
                }));
            }
            catch
            {
                // The window is already going away.
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_app.Exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _app.NotifyClosed();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_APP_SHOW || (uint)m.Msg == AppMessages.Show)
            {
                ShowFromTray();
                return;
            }
            if (m.Msg == Native.WM_SETTINGCHANGE)
            {
                // Windows broadcasts this when the app theme is switched.
                bool dark = Theme.SystemPrefersDark();
                if (dark != Theme.IsDark) Theme.Apply(dark);
            }
            base.WndProc(ref m);
        }
    }
}
