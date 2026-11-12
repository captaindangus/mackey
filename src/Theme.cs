using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace MacKey
{
    /// <summary>Semantic text roles so labels can be re-coloured on theme change.</summary>
    internal enum Ink
    {
        Text,
        Muted,
        Faint,
        Green
    }

    internal static class Theme
    {
        /// <summary>Raised after the palette switches between light and dark.</summary>
        public static event System.EventHandler Changed;

        public static bool IsDark { get; private set; }

        private static bool _ready;

        public static Color Bg;
        public static Color Card;
        public static Color Header;
        public static Color Field;
        public static Color Text;
        public static Color Muted;
        public static Color Faint;
        public static Color Accent;
        public static Color AccentSoft;
        public static Color Border;
        public static Color Track;
        public static Color Knob;
        public static Color Green;

        static Theme()
        {
            Apply(false);
            _ready = true;
        }

        /// <summary>True when Windows is set to dark mode for apps.</summary>
        public static bool SystemPrefersDark()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key == null) return false;
                    object value = key.GetValue("AppsUseLightTheme");
                    if (value is int) return (int)value == 0;
                }
            }
            catch
            {
            }
            return false;
        }

        public static void Apply(bool dark)
        {
            bool changed = _ready && dark != IsDark;
            IsDark = dark;

            if (dark)
            {
                Bg = Color.FromArgb(28, 28, 30);
                Card = Color.FromArgb(44, 44, 46);
                Header = Color.FromArgb(36, 36, 38);
                Field = Color.FromArgb(26, 26, 28);
                Text = Color.FromArgb(245, 245, 247);
                Muted = Color.FromArgb(161, 161, 166);
                Faint = Color.FromArgb(124, 124, 129);
                Accent = Color.FromArgb(10, 132, 255);
                AccentSoft = Color.FromArgb(22, 46, 74);
                Border = Color.FromArgb(58, 58, 60);
                Track = Color.FromArgb(72, 72, 74);
                Knob = Color.FromArgb(242, 242, 247);
                Green = Color.FromArgb(48, 209, 88);
            }
            else
            {
                Bg = Color.FromArgb(242, 242, 247);
                Card = Color.White;
                Header = Color.White;
                Field = Color.White;
                Text = Color.FromArgb(29, 29, 31);
                Muted = Color.FromArgb(110, 110, 115);
                Faint = Color.FromArgb(150, 150, 155);
                Accent = Color.FromArgb(10, 132, 255);
                AccentSoft = Color.FromArgb(234, 244, 255);
                Border = Color.FromArgb(226, 226, 230);
                Track = Color.FromArgb(209, 209, 214);
                Knob = Color.White;
                Green = Color.FromArgb(40, 175, 90);
            }

            if (changed && Changed != null) Changed(null, System.EventArgs.Empty);
        }

        /// <summary>Asks the DWM to paint the window frame dark or light.</summary>
        public static void ApplyTitleBar(System.IntPtr handle)
        {
            if (handle == System.IntPtr.Zero) return;
            int value = IsDark ? 1 : 0;
            try { Native.DwmSetWindowAttribute(handle, 20, ref value, sizeof(int)); }
            catch { }
            try { Native.DwmSetWindowAttribute(handle, 19, ref value, sizeof(int)); }
            catch { }
        }

        public static Color ForInk(Ink ink)
        {
            switch (ink)
            {
                case Ink.Muted: return Muted;
                case Ink.Faint: return Faint;
                case Ink.Green: return Green;
            }
            return Text;
        }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Blend(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}
