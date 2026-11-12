using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacKey
{
    internal static class AppIcon
    {
        private static Icon _icon;

        public static Icon Shared
        {
            get
            {
                if (_icon == null)
                {
                    try
                    {
                        _icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    }
                    catch
                    {
                    }
                    if (_icon == null) _icon = SystemIcons.Application;
                }
                return _icon;
            }
        }
    }
}
