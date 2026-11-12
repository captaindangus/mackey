using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacKey
{
    internal sealed class ShortcutsForm : Form
    {
        private readonly ListView _list;

        public ShortcutsForm()
        {
            Text = "MacKey \u2014 Shortcuts";
            ClientSize = new Size(580, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.Bg;
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);

            Label intro = new Label();
            intro.Text = "Cmd is the key next to Space. These are the mappings MacKey adds.";
            intro.Font = new Font("Segoe UI", 9f);
            intro.ForeColor = Theme.Muted;
            intro.AutoSize = true;
            intro.Tag = Ink.Muted;
            intro.Location = new Point(16, 14);
            Controls.Add(intro);

            _list = new ListView();
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.BackColor = Theme.Card;
            _list.ForeColor = Theme.Text;
            _list.Font = new Font("Segoe UI", 9.5f);
            _list.Bounds = new Rectangle(16, 42, 548, 516);
            _list.Columns.Add("On your Mac", 210);
            _list.Columns.Add("On Windows", 310);

            _list.OwnerDraw = true;
            _list.DrawColumnHeader += DrawColumnHeader;
            _list.DrawItem += delegate(object s, DrawListViewItemEventArgs e)
            {
                e.DrawDefault = true;
            };
            _list.DrawSubItem += delegate(object s, DrawListViewSubItemEventArgs e)
            {
                e.DrawDefault = true;
            };

            Add(_list, "\u2318 C / V / X", "Ctrl C / V / X");
            Add(_list, "\u2318 Z / \u21e7\u2318 Z", "Ctrl Z / Ctrl Shift Z");
            Add(_list, "\u2318 A / S / O / P / N / W", "Ctrl A / S / O / P / N / W");
            Add(_list, "\u2318 F", "Ctrl F");
            Add(_list, "\u2318 Tab", "Alt Tab (hold \u2318 to cycle)");
            Add(_list, "\u2318 `", "Alt Tab");
            Add(_list, "\u2318 Q", "Alt F4 (quit app)");
            Add(_list, "\u2318 H / \u2318 M", "Win Down (minimize)");
            Add(_list, "\u2318 Space", "Win S (search)");
            Add(_list, "\u2318 \u21e7 3 / 4 / 5", "Win Shift S (screenshot)");
            Add(_list, "\u2318 [ / \u2318 ]", "Alt Left / Alt Right (back / forward)");
            Add(_list, "\u2318 \u21e7 [ / \u2318 \u21e7 ]", "Ctrl Shift Tab / Ctrl Tab");
            Add(_list, "\u2318 \u2190 / \u2318 \u2192", "Home / End (line start / end)");
            Add(_list, "\u2318 \u2191 / \u2318 \u2193", "Ctrl Home / Ctrl End (document)");
            Add(_list, "\u2318 \u21e7 \u2190 / \u2318 \u21e7 \u2192", "Shift Home / Shift End");
            Add(_list, "\u2318 Backspace / \u2318 Delete", "Delete (move to Trash)");
            Add(_list, "\u2318 \u2325 Esc", "Ctrl Shift Esc (Task Manager)");
            Add(_list, "\u2318 \u2303 Q", "Lock Windows");
            Add(_list, "Caps Lock (optional)", "Windows key");
            Add(_list, "Right Win (PC keyboard)", "Real Windows key");

            Controls.Add(_list);

            Label note = new Label();
            note.Text =
                "Secure screens (Ctrl+Alt+Del, UAC, the lock screen) are handled by Windows " +
                "before any app can remap them. For shortcuts inside administrator apps, " +
                "use Restart as administrator from the tray menu.";
            note.Font = new Font("Segoe UI", 8.5f);
            note.ForeColor = Theme.Muted;
            note.AutoSize = true;
            note.MaximumSize = new Size(430, 0);
            note.Tag = Ink.Muted;
            note.Location = new Point(16, 566);
            Controls.Add(note);

            Button close = new Button();
            close.Text = "Done";
            close.Font = new Font("Segoe UI", 9.5f);
            close.Size = new Size(88, 30);
            close.Location = new Point(ClientSize.Width - 16 - 88, 574);
            close.DialogResult = DialogResult.OK;
            close.FlatStyle = FlatStyle.Flat;
            close.FlatAppearance.BorderColor = Theme.Border;
            close.BackColor = Theme.Card;
            close.ForeColor = Theme.Text;
            close.UseVisualStyleBackColor = false;
            Controls.Add(close);

            AcceptButton = close;
            CancelButton = close;

            Theme.Changed += OnThemeChanged;
        }

        private static void Add(ListView list, string mac, string win)
        {
            list.Items.Add(new ListViewItem(new string[] { mac, win }));
        }

        /// <summary>Paints the column header so it follows the dark palette.</summary>
        private static void DrawColumnHeader(
            object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Theme.Card))
                e.Graphics.FillRectangle(brush, e.Bounds);
            using (Pen pen = new Pen(Theme.Border))
                e.Graphics.DrawLine(
                    pen, e.Bounds.Left, e.Bounds.Bottom - 1,
                    e.Bounds.Right, e.Bounds.Bottom - 1);

            Rectangle text = new Rectangle(
                e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics, e.Header.Text, e.Font, text, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;

            BackColor = Theme.Bg;
            _list.BackColor = Theme.Card;
            _list.ForeColor = Theme.Text;
            _list.Invalidate(true);

            foreach (Control c in Controls)
            {
                Label label = c as Label;
                if (label != null && label.Tag is Ink)
                    label.ForeColor = Theme.ForInk((Ink)label.Tag);

                Button button = c as Button;
                if (button != null)
                {
                    button.BackColor = Theme.Card;
                    button.ForeColor = Theme.Text;
                    button.FlatAppearance.BorderColor = Theme.Border;
                }
            }

            Theme.ApplyTitleBar(Handle);
            ApplyListViewTheme();
            Invalidate(true);
        }

        private void ApplyListViewTheme()
        {
            if (!_list.IsHandleCreated) return;
            try
            {
                Native.SetWindowTheme(
                    _list.Handle, Theme.IsDark ? "DarkMode_Explorer" : "Explorer", null);

                IntPtr header = Native.SendMessage(
                    _list.Handle, Native.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
                if (header != IntPtr.Zero)
                {
                    Native.SetWindowTheme(
                        header, Theme.IsDark ? "DarkMode_Explorer" : "Explorer", null);
                }
            }
            catch
            {
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.ApplyTitleBar(Handle);
            ApplyListViewTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
