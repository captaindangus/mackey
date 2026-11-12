using System;
using System.IO;
using System.Text;

namespace MacKey
{
    internal sealed class Settings
    {
        public bool MacMode;
        public bool PcLayout;
        public bool ExtraShortcuts = true;
        public bool CapsLockAsWin;
        public bool StartWithWindows;
        public bool SeenWelcome;

        private static string Folder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MacKey");
            }
        }

        private static string FilePath
        {
            get { return Path.Combine(Folder, "settings.ini"); }
        }

        public static Settings Load()
        {
            Settings s = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return s;

                foreach (string raw in File.ReadAllLines(FilePath))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;

                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    bool b;
                    if (!bool.TryParse(val, out b)) b = val == "1";

                    switch (key)
                    {
                        case "MacMode": s.MacMode = b; break;
                        case "PcLayout": s.PcLayout = b; break;
                        case "ExtraShortcuts": s.ExtraShortcuts = b; break;
                        case "CapsLockAsWin": s.CapsLockAsWin = b; break;
                        case "StartWithWindows": s.StartWithWindows = b; break;
                        case "SeenWelcome": s.SeenWelcome = b; break;
                    }
                }
            }
            catch
            {
                // A broken settings file should never stop the app from starting.
            }
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# MacKey settings");
                sb.AppendLine("MacMode=" + MacMode);
                sb.AppendLine("PcLayout=" + PcLayout);
                sb.AppendLine("ExtraShortcuts=" + ExtraShortcuts);
                sb.AppendLine("CapsLockAsWin=" + CapsLockAsWin);
                sb.AppendLine("StartWithWindows=" + StartWithWindows);
                sb.AppendLine("SeenWelcome=" + SeenWelcome);
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Read-only profile: run with defaults rather than crashing.
            }
        }
    }
}
