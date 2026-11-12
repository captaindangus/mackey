using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MacKey
{
    /// <summary>A white, softly rounded card that clips its children.</summary>
    internal sealed class CardPanel : Panel
    {
        public CardPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }

        private int Radius
        {
            get { return (int)(10 * DeviceDpi / 96f); }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width <= 0 || Height <= 0) return;

            using (GraphicsPath path = Theme.RoundedRect(
                new Rectangle(0, 0, Width, Height), Radius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Theme.RoundedRect(
                new Rectangle(1, 1, Width - 3, Height - 3), Radius))
            using (Pen pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    /// <summary>An iOS-style switch with a short animation.</summary>
    internal sealed class MacToggle : Control
    {
        private bool _checked;
        private float _pos;
        private readonly Timer _timer;

        public event EventHandler CheckedChanged;

        public MacToggle()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            BackColor = Theme.Card;
            Cursor = Cursors.Hand;
            TabStop = true;
            Size = new Size(60, 34);

            _timer = new Timer();
            _timer.Interval = 15;
            _timer.Tick += delegate { Animate(); };
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                _timer.Start();
                EventHandler handler = CheckedChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void Animate()
        {
            float target = _checked ? 1f : 0f;
            float diff = target - _pos;
            if (Math.Abs(diff) < 0.04f)
            {
                _pos = target;
                _timer.Stop();
            }
            else
            {
                _pos += diff * 0.35f;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            int w = Width;
            int h = Height;
            int pad = Math.Max(2, h / 12);

            using (GraphicsPath path = Theme.RoundedRect(
                new Rectangle(0, 0, w - 1, h - 1), h / 2))
            using (SolidBrush brush = new SolidBrush(
                Theme.Blend(Theme.Track, Theme.Accent, _pos)))
            {
                g.FillPath(brush, path);
            }

            int knob = h - pad * 2;
            int travel = w - knob - pad * 2;
            int x = pad + (int)(travel * _pos);

            using (SolidBrush brush = new SolidBrush(Theme.Knob))
                g.FillEllipse(brush, x, pad, knob, knob);
            using (Pen pen = new Pen(Theme.IsDark
                ? Color.FromArgb(40, 255, 255, 255)
                : Color.FromArgb(28, 0, 0, 0)))
                g.DrawEllipse(pen, x, pad, knob, knob);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>A selectable card used for the keyboard layout choice.</summary>
    internal sealed class ChoiceCard : Control
    {
        private bool _selected;
        private readonly string _title;
        private readonly string _subtitle;
        private readonly Font _titleFont;
        private readonly Font _subFont;

        public event EventHandler SelectedChanged;

        public ChoiceCard(string title, string subtitle)
        {
            _title = title;
            _subtitle = subtitle;
            _titleFont = new Font("Segoe UI Semibold", 9.5f);
            _subFont = new Font("Segoe UI", 8.5f);

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            BackColor = Theme.Card;
            Cursor = Cursors.Hand;
            TabStop = true;
            Size = new Size(228, 68);
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value) return;
                _selected = value;
                Invalidate();
                EventHandler handler = SelectedChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!_selected) Selected = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                if (!_selected) Selected = true;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            float s = DeviceDpi / 96f;
            int radius = (int)(9 * s);

            using (GraphicsPath path = Theme.RoundedRect(
                new Rectangle(0, 0, Width - 1, Height - 1), radius))
            {
                using (SolidBrush fill = new SolidBrush(
                    _selected ? Theme.AccentSoft : Theme.Card))
                    g.FillPath(fill, path);
                using (Pen pen = new Pen(
                    _selected ? Theme.Accent : Theme.Border, _selected ? 2f : 1f))
                    g.DrawPath(pen, path);
            }

            int dotSize = (int)(18 * s);
            int cy = Height / 2;
            Rectangle dot = new Rectangle((int)(16 * s), cy - dotSize / 2, dotSize, dotSize);
            using (Pen pen = new Pen(
                _selected ? Theme.Accent : Theme.Faint, 1.5f))
                g.DrawEllipse(pen, dot);
            if (_selected)
            {
                int inner = (int)(8 * s);
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                    g.FillEllipse(
                        b, dot.X + (dot.Width - inner) / 2,
                        dot.Y + (dot.Height - inner) / 2,
                        inner, inner);
            }

            float textX = 44 * s;
            using (SolidBrush b = new SolidBrush(Theme.Text))
                g.DrawString(_title, _titleFont, b, new PointF(textX, 13 * s));
            using (SolidBrush b = new SolidBrush(Theme.Muted))
                g.DrawString(_subtitle, _subFont, b, new PointF(textX, 33 * s));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _titleFont.Dispose();
                _subFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>A settings row with a title, subtitle and small switch.</summary>
    internal sealed class ToggleRow : Control
    {
        private bool _checked;
        private float _pos;
        private readonly Timer _timer;
        private readonly string _title;
        private readonly string _subtitle;
        private readonly Font _titleFont;
        private readonly Font _subFont;

        public event EventHandler CheckedChanged;

        public ToggleRow(string title, string subtitle)
        {
            _title = title;
            _subtitle = subtitle;
            _titleFont = new Font("Segoe UI", 10f);
            _subFont = new Font("Segoe UI", 8.5f);

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            BackColor = Theme.Card;
            Cursor = Cursors.Hand;
            TabStop = true;
            Size = new Size(472, 54);

            _timer = new Timer();
            _timer.Interval = 15;
            _timer.Tick += delegate { Animate(); };
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                _timer.Start();
                EventHandler handler = CheckedChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void Animate()
        {
            float target = _checked ? 1f : 0f;
            float diff = target - _pos;
            if (Math.Abs(diff) < 0.04f)
            {
                _pos = target;
                _timer.Stop();
            }
            else
            {
                _pos += diff * 0.35f;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            float s = DeviceDpi / 96f;
            using (SolidBrush b = new SolidBrush(Theme.Text))
                g.DrawString(_title, _titleFont, b, new PointF(16 * s, 8 * s));
            using (SolidBrush b = new SolidBrush(Theme.Muted))
                g.DrawString(_subtitle, _subFont, b, new PointF(16 * s, 28 * s));

            int sw = (int)(44 * s);
            int sh = (int)(24 * s);
            int x = Width - (int)(16 * s) - sw;
            int y = (Height - sh) / 2;

            using (GraphicsPath path = Theme.RoundedRect(
                new Rectangle(x, y, sw - 1, sh - 1), sh / 2))
            using (SolidBrush brush = new SolidBrush(
                Theme.Blend(Theme.Track, Theme.Accent, _pos)))
            {
                g.FillPath(brush, path);
            }

            int knob = sh - (int)(4 * s);
            int kx = x + (int)(2 * s) + (int)((sw - knob - 4 * s) * _pos);
            using (SolidBrush brush = new SolidBrush(Theme.Knob))
                g.FillEllipse(brush, kx, y + 2, knob, knob);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _titleFont.Dispose();
                _subFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
