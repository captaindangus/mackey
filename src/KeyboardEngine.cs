using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace MacKey
{
    /// <summary>
    /// Owns the global low-level keyboard hook and translates Mac-style chords
    /// into their Windows equivalents while Mac Mode is on.
    /// </summary>
    internal sealed class KeyboardEngine : IDisposable
    {
        public bool MacMode;
        public bool PcLayout;
        public bool ExtraShortcuts = true;
        public bool CapsLockAsWin;

        /// <summary>Raised (on the UI thread) when Ctrl+Alt+M is pressed.</summary>
        public event EventHandler ToggleHotkey;

        private static readonly int[] NoKeys = new int[0];
        private static readonly IntPtr Marker = (IntPtr)0x4D41434B; // "MACK"

        private Native.LowLevelKeyboardProc _proc;
        private IntPtr _hook = IntPtr.Zero;
        private bool _disposed;
        private StreamWriter _log;

        // Physical modifier state, independent of remapping.
        private readonly HashSet<int> _ctrlKeys = new HashSet<int>();
        private readonly HashSet<int> _shiftKeys = new HashSet<int>();
        private readonly HashSet<int> _rawAltKeys = new HashSet<int>();

        // Logical roles after layout selection.
        private readonly HashSet<int> _cmdKeys = new HashSet<int>();
        private readonly HashSet<int> _optKeys = new HashSet<int>();

        // Keys currently being held with a synthetic Ctrl wrapped around them.
        private readonly HashSet<int> _wrapped = new HashSet<int>();

        // Special (Mac-only) chords that are currently held down, so key-up and
        // auto-repeat are handled without firing the chord a second time.
        private readonly Dictionary<int, int[]> _specialCombos = new Dictionary<int, int[]>();

        private bool _ctrlSynthetic;
        private bool _switcher;
        private bool _switcherAlt;
        private bool _hotkeyBlocked;
        private bool _capsWin;

        public bool Install()
        {
            if (_hook != IntPtr.Zero) return true;

            if (Environment.GetEnvironmentVariable("MACK_DEBUG") == "1")
            {
                try
                {
                    string dir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MacKey");
                    Directory.CreateDirectory(dir);
                    _log = new StreamWriter(Path.Combine(dir, "debug.log"), false);
                    _log.AutoFlush = true;
                    Log("--- MacKey debug log ---");
                }
                catch
                {
                    _log = null;
                }
            }

            _proc = HookProc;
            _hook = Native.SetWindowsHookEx(
                Native.WH_KEYBOARD_LL, _proc, Native.GetModuleHandle(null), 0);
            Log("hook installed=" + (_hook != IntPtr.Zero));
            return _hook != IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ResetState();
            if (_hook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
            if (_log != null)
            {
                try { _log.Dispose(); } catch { }
                _log = null;
            }
            GC.SuppressFinalize(this);
        }

        private void Log(string message)
        {
            if (_log == null) return;
            try
            {
                _log.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + message);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Releases anything this engine may be holding down. Safe to call at any time.
        /// </summary>
        public void ResetState()
        {
            EndSwitcher();

            if (_ctrlSynthetic)
            {
                SendKey(Native.VK_LCONTROL, true);
                _ctrlSynthetic = false;
            }

            // On a PC layout the Option key never injects Alt, so there is
            // nothing to release here.

            if (_capsWin)
            {
                SendKey(Native.VK_LWIN, true);
                _capsWin = false;
            }

            _wrapped.Clear();
            _specialCombos.Clear();
            _cmdKeys.Clear();
            _optKeys.Clear();
            _ctrlKeys.Clear();
            _shiftKeys.Clear();
            _rawAltKeys.Clear();
            _hotkeyBlocked = false;
        }

        private bool OsAltDown
        {
            get
            {
                if (!PcLayout) return _rawAltKeys.Count > 0;
                // On a PC layout the Option key is logical only, so only a real
                // right Alt counts as a Windows-level Alt being held.
                return _rawAltKeys.Contains(Native.VK_RMENU);
            }
        }

        private bool IsCmdKey(int vk)
        {
            if (PcLayout) return vk == Native.VK_LMENU;
            return vk == Native.VK_LWIN || vk == Native.VK_RWIN;
        }

        private bool IsOptKey(int vk)
        {
            if (PcLayout) return vk == Native.VK_LWIN;
            return vk == Native.VK_LMENU || vk == Native.VK_RMENU;
        }

        private static bool IsModifier(int vk)
        {
            switch (vk)
            {
                case Native.VK_LWIN:
                case Native.VK_RWIN:
                case Native.VK_LMENU:
                case Native.VK_RMENU:
                case Native.VK_LCONTROL:
                case Native.VK_RCONTROL:
                case Native.VK_LSHIFT:
                case Native.VK_RSHIFT:
                case Native.VK_CAPITAL:
                    return true;
            }
            return false;
        }

        private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
                return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

            Native.KBDLLHOOKSTRUCT k = (Native.KBDLLHOOKSTRUCT)
                Marshal.PtrToStructure(lParam, typeof(Native.KBDLLHOOKSTRUCT));

            // Never touch our own injected events (or another tool's).
            if ((k.flags & Native.LLKHF_INJECTED) != 0)
                return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

            int vk = (int)k.vkCode;
            bool down = wParam == (IntPtr)Native.WM_KEYDOWN || wParam == (IntPtr)Native.WM_SYSKEYDOWN;
            bool up = wParam == (IntPtr)Native.WM_KEYUP || wParam == (IntPtr)Native.WM_SYSKEYUP;
            if (!down && !up)
                return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

            TrackPhysical(vk, down, up);

            Log(string.Format("key vk=0x{0:X2} {1} cmd={2} opt={3} ctrl={4} shift={5} rawAlt={6}",
                vk, down ? "down" : "up", _cmdKeys.Count, _optKeys.Count,
                _ctrlKeys.Count, _shiftKeys.Count, _rawAltKeys.Count));

            // Ctrl + Alt + M is deliberately handled in the hook so it works no
            // matter which physical key is currently acting as Command.
            if (vk == 'M')
            {
                if (down && _ctrlKeys.Count > 0 && _rawAltKeys.Count > 0)
                {
                    _hotkeyBlocked = true;
                    RaiseToggle();
                    return (IntPtr)1;
                }
                if (up && _hotkeyBlocked)
                {
                    _hotkeyBlocked = false;
                    return (IntPtr)1;
                }
            }

            if (!MacMode)
                return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

            // Optional escape hatch: Caps Lock becomes a real Windows key.
            if (CapsLockAsWin && vk == Native.VK_CAPITAL)
            {
                if (down)
                {
                    if (!_capsWin)
                    {
                        SendKey(Native.VK_LWIN, false);
                        _capsWin = true;
                    }
                }
                else if (_capsWin)
                {
                    SendKey(Native.VK_LWIN, true);
                    _capsWin = false;
                }
                return (IntPtr)1;
            }

            if (IsCmdKey(vk))
            {
                if (down)
                {
                    _cmdKeys.Add(vk);
                }
                else
                {
                    _cmdKeys.Remove(vk);
                    if (_cmdKeys.Count == 0)
                    {
                        EndSwitcher();
                        _specialCombos.Clear();

                        // Safety net: never leave the synthetic Ctrl behind.
                        if (_wrapped.Count == 0 && _ctrlKeys.Count == 0 && _ctrlSynthetic)
                        {
                            SendKey(Native.VK_LCONTROL, true);
                            _ctrlSynthetic = false;
                        }
                    }
                }
                return (IntPtr)1;
            }

            if (IsOptKey(vk))
            {
                if (PcLayout)
                {
                    // Physical Win sits in the Option spot. Track it as a logical
                    // modifier only: injecting Alt here would make a lone tap
                    // open menus and turn Option + Tab into the Windows switcher.
                    if (down) _optKeys.Add(vk);
                    else _optKeys.Remove(vk);
                    return (IntPtr)1;
                }

                // Apple keyboard: Alt already sits in the Option spot.
                if (down) _optKeys.Add(vk);
                else _optKeys.Remove(vk);
                return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
            }

            bool shift = _shiftKeys.Count > 0;
            bool option = _optKeys.Count > 0;
            bool ctrl = _ctrlKeys.Count > 0;

            // A key that was wrapped in a synthetic Ctrl must always be released,
            // even when Command was let go first. Otherwise the synthetic Ctrl
            // stays logically held and every later key becomes Ctrl + key.
            if (up && _wrapped.Remove(vk))
            {
                SendKey(vk, true);
                if (_wrapped.Count == 0 && _ctrlKeys.Count == 0 && _ctrlSynthetic)
                {
                    SendKey(Native.VK_LCONTROL, true);
                    _ctrlSynthetic = false;
                }
                return (IntPtr)1;
            }

            // While the app switcher is held open, keep it open like macOS does.
            if (_switcher)
            {
                if (vk == Native.VK_TAB)
                {
                    SendKey(Native.VK_TAB, up);
                    return (IntPtr)1;
                }
                if (vk == Native.VK_ESCAPE && down)
                {
                    SendKey(Native.VK_ESCAPE, false);
                    SendKey(Native.VK_ESCAPE, true);
                    EndSwitcher();
                    return (IntPtr)1;
                }
                if (vk == Native.VK_LSHIFT || vk == Native.VK_RSHIFT)
                    return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

                return (IntPtr)1;
            }

            if (_cmdKeys.Count > 0 && !IsModifier(vk))
            {
                if (ExtraShortcuts)
                {
                    if (down && (vk == Native.VK_TAB || vk == Native.VK_OEM_3))
                    {
                        StartSwitcher();
                        SendKey(Native.VK_TAB, false);
                        SendKey(Native.VK_TAB, true);
                        return (IntPtr)1;
                    }

                    if (down)
                    {
                        int[] combo;
                        if (_specialCombos.TryGetValue(vk, out combo))
                        {
                            // Auto-repeat: only keys that should repeat get re-sent.
                            if (Repeats(vk) && combo.Length > 0) SendCombo(combo);
                            return (IntPtr)1;
                        }

                        combo = SpecialCombo(vk, shift, option, ctrl);
                        if (combo != null)
                        {
                            Log("  -> special (" + combo.Length + " keys)");
                            if (combo.Length > 0) SendCombo(combo);
                            _specialCombos[vk] = combo;
                            return (IntPtr)1;
                        }
                        Log("  -> wrap as Ctrl+" + vk);
                    }
                    else if (_specialCombos.Remove(vk))
                    {
                        return (IntPtr)1;
                    }
                }

                if (down)
                {
                    if (_wrapped.Add(vk))
                    {
                        if (_ctrlKeys.Count == 0 && !_ctrlSynthetic)
                        {
                            SendKey(Native.VK_LCONTROL, false);
                            _ctrlSynthetic = true;
                        }
                        SendKey(vk, false);
                    }
                    else
                    {
                        SendKey(vk, false); // auto-repeat
                    }
                    return (IntPtr)1;
                }

                if (up)
                {
                    // Wrapped releases are handled before the Command check.
                    return (IntPtr)1;
                }
            }

            return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        /// <summary>
        /// Keys whose special chord may safely repeat while the key is held.
        /// </summary>
        private static bool Repeats(int vk)
        {
            switch (vk)
            {
                case Native.VK_LEFT:
                case Native.VK_RIGHT:
                case Native.VK_UP:
                case Native.VK_DOWN:
                case Native.VK_BACK:
                case Native.VK_DELETE:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the Windows chord for a Mac-only shortcut, an empty array when
        /// the key was handled directly, or null when the normal Cmd -> Ctrl
        /// wrapping should be used.
        /// </summary>
        private int[] SpecialCombo(int vk, bool shift, bool option, bool ctrl)
        {
            // Cmd + Option + Esc = Force Quit -> Task Manager.
            if (option && vk == Native.VK_ESCAPE)
            {
                if (OsAltDown)
                {
                    SendKey(Native.VK_LMENU, true);
                    if (PcLayout) _optKeys.Clear();
                    else _rawAltKeys.Clear();
                }
                SendCombo(new int[] { Native.VK_LCONTROL, Native.VK_LSHIFT, Native.VK_ESCAPE });
                return NoKeys;
            }

            // Cmd + Ctrl + Q (and Cmd + Shift + Q) = lock the PC.
            if (ctrl && vk == 'Q')
            {
                Native.LockWorkStation();
                return NoKeys;
            }
            if (shift && vk == 'Q')
            {
                Native.LockWorkStation();
                return NoKeys;
            }

            // Screenshots: Cmd + Shift + 3/4/5 -> Win + Shift + S.
            if (shift && (vk == '3' || vk == '4' || vk == '5'))
                return new int[] { Native.VK_LWIN, Native.VK_LSHIFT, 'S' };

            // Browser back / forward and tab switching.
            if (vk == Native.VK_OEM_4)
            {
                if (shift) return new int[] { Native.VK_LCONTROL, Native.VK_LSHIFT, Native.VK_TAB };
                return new int[] { Native.VK_LMENU, Native.VK_LEFT };
            }
            if (vk == Native.VK_OEM_6)
            {
                if (shift) return new int[] { Native.VK_LCONTROL, Native.VK_TAB };
                return new int[] { Native.VK_LMENU, Native.VK_RIGHT };
            }

            // Cmd + Q = quit the app.
            if (vk == 'Q') return new int[] { Native.VK_LMENU, Native.VK_F4 };

            // Cmd + H / Cmd + M = minimize.
            if (vk == 'H' || vk == 'M') return new int[] { Native.VK_LWIN, Native.VK_DOWN };

            // Cmd + Space = Spotlight -> Windows search.
            if (vk == Native.VK_SPACE) return new int[] { Native.VK_LWIN, 'S' };

            // Mac text navigation.
            if (vk == Native.VK_LEFT)
                return shift
                    ? new int[] { Native.VK_LSHIFT, Native.VK_HOME }
                    : new int[] { Native.VK_HOME };
            if (vk == Native.VK_RIGHT)
                return shift
                    ? new int[] { Native.VK_LSHIFT, Native.VK_END }
                    : new int[] { Native.VK_END };
            if (vk == Native.VK_UP)
                return shift
                    ? new int[] { Native.VK_LCONTROL, Native.VK_LSHIFT, Native.VK_HOME }
                    : new int[] { Native.VK_LCONTROL, Native.VK_HOME };
            if (vk == Native.VK_DOWN)
                return shift
                    ? new int[] { Native.VK_LCONTROL, Native.VK_LSHIFT, Native.VK_END }
                    : new int[] { Native.VK_LCONTROL, Native.VK_END };

            // Cmd + Delete / Cmd + Backspace = move to Trash.
            if (vk == Native.VK_BACK || vk == Native.VK_DELETE)
                return new int[] { Native.VK_DELETE };

            return null;
        }

        private void StartSwitcher()
        {
            if (_switcher) return;
            Log("  -> switcher start");
            _switcher = true;
            if (!OsAltDown)
            {
                SendKey(Native.VK_LMENU, false);
                _switcherAlt = true;
            }
        }

        private void EndSwitcher()
        {
            if (!_switcher) return;
            Log("  -> switcher end");
            _switcher = false;
            if (_switcherAlt)
            {
                SendKey(Native.VK_LMENU, true);
                _switcherAlt = false;
            }
        }

        private void SendCombo(int[] keys)
        {
            List<int> pressed = new List<int>(keys.Length);
            foreach (int key in keys)
            {
                if (key == Native.VK_LCONTROL && _ctrlKeys.Count > 0) continue;
                if (key == Native.VK_LSHIFT && _shiftKeys.Count > 0) continue;
                if (key == Native.VK_LMENU && OsAltDown) continue;

                SendKey(key, false);
                pressed.Add(key);
            }
            for (int i = pressed.Count - 1; i >= 0; i--)
                SendKey(pressed[i], true);
        }

        private void SendKey(int vk, bool up)
        {
            Log(string.Format("     send vk=0x{0:X2} {1}", vk, up ? "up" : "down"));
            Native.INPUT input = new Native.INPUT();
            input.type = Native.INPUT_KEYBOARD;
            input.U.ki.wVk = (ushort)vk;
            input.U.ki.wScan = 0;
            input.U.ki.dwFlags =
                (up ? Native.KEYEVENTF_KEYUP : 0) |
                (IsExtended(vk) ? Native.KEYEVENTF_EXTENDEDKEY : 0);
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = Marker;

            Native.INPUT[] inputs = new Native.INPUT[] { input };
            Native.SendInput(1, inputs, Marshal.SizeOf(typeof(Native.INPUT)));
        }

        private static bool IsExtended(int vk)
        {
            switch (vk)
            {
                case Native.VK_LEFT:
                case Native.VK_RIGHT:
                case Native.VK_UP:
                case Native.VK_DOWN:
                case Native.VK_HOME:
                case Native.VK_END:
                case Native.VK_PRIOR:
                case Native.VK_NEXT:
                case Native.VK_INSERT:
                case Native.VK_DELETE:
                case Native.VK_RCONTROL:
                case Native.VK_RMENU:
                case Native.VK_DIVIDE:
                case Native.VK_SNAPSHOT:
                    return true;
            }
            return false;
        }

        private void TrackPhysical(int vk, bool down, bool up)
        {
            if (vk == Native.VK_LCONTROL || vk == Native.VK_RCONTROL)
                Update(_ctrlKeys, vk, down, up);
            else if (vk == Native.VK_LSHIFT || vk == Native.VK_RSHIFT)
                Update(_shiftKeys, vk, down, up);
            else if (vk == Native.VK_LMENU || vk == Native.VK_RMENU)
                Update(_rawAltKeys, vk, down, up);
        }

        private static void Update(HashSet<int> set, int vk, bool down, bool up)
        {
            if (down) set.Add(vk);
            else if (up) set.Remove(vk);
        }

        private void RaiseToggle()
        {
            EventHandler handler = ToggleHotkey;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
