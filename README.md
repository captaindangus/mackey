# MacKey

**Mac keyboard shortcuts on Windows — as a tiny tray app.**

MacKey makes the key next to the space bar behave like macOS `⌘` (Command). Hold it and press `C`, `V`, `Z`, `Tab`, `Q`, `Space`, and the rest of the shortcuts your fingers already know. It also adds the Mac-only chords Windows doesn't have, like `⌘Tab` for the app switcher and `⌘Space` for search.

It is a single self-contained `.exe`. No installer, no background service, no account, no network access.

---

## Quick start

1. Double-click **`dist\MacKey.exe`**.
2. Pick **Apple keyboard** or **PC keyboard** (see below).
3. Flip **Mac Mode** on. Or press **Ctrl + Alt + M** at any time.
4. The app keeps running in the notification area. Close the window and Mac Mode stays on; use the tray icon to reopen it.

To build it yourself:

```powershell
.\build.ps1
.\run.ps1
```

`build.ps1` uses the C# compiler that ships with Windows (.NET Framework 4.8). Nothing needs to be installed.

---

## Which keyboard do I have?

| Choice | Use it when | What MacKey does |
| --- | --- | --- |
| **Apple keyboard** | You plugged a real Apple keyboard into the PC. | `⌘` is already next to Space, so only the `⌘` key is remapped. |
| **PC keyboard** | You are on a normal Windows laptop or keyboard. | Swaps the left `Alt` and left `Win` keys so the key next to Space becomes `⌘`. |

The physical `Ctrl` key stays `Ctrl` in both modes, just like the `Control` key on a Mac. The physical `Alt` key stays `Alt` (Option).

---

## What gets remapped

Mac Mode is intentionally conservative: the core rule is **`⌘` + key becomes `Ctrl` + key**. That alone covers almost every app, because Windows uses `Ctrl` where macOS uses `⌘`.

| On your Mac | On Windows |
| --- | --- |
| `⌘C` / `⌘V` / `⌘X` / `⌘A` | `Ctrl+C` / `Ctrl+V` / `Ctrl+X` / `Ctrl+A` |
| `⌘Z` / `⇧⌘Z` | `Ctrl+Z` / `Ctrl+Shift+Z` |
| `⌘S` / `⌘O` / `⌘P` / `⌘N` / `⌘W` / `⌘F` | The matching `Ctrl` shortcut |
| `⌘Tab` | `Alt+Tab` (hold `⌘` to keep the switcher open, `⇧` to go back) |
| ``⌘` `` | `Alt+Tab` |
| `⌘Q` | `Alt+F4` (quit the app) |
| `⌘H` / `⌘M` | `Win+Down` (minimize) |
| `⌘Space` | `Win+S` (search) |
| `⌘⇧3` / `⌘⇧4` / `⌘⇧5` | `Win+Shift+S` (screenshot) |
| `⌘[` / `⌘]` | `Alt+Left` / `Alt+Right` (back / forward) |
| `⇧⌘[` / `⇧⌘]` | `Ctrl+Shift+Tab` / `Ctrl+Tab` |
| `⌘←` / `⌘→` | `Home` / `End` |
| `⌘↑` / `⌘↓` | `Ctrl+Home` / `Ctrl+End` |
| `⇧⌘←` / `⇧⌘→` | `Shift+Home` / `Shift+End` |
| `⌘Backspace` / `⌘Delete` | `Delete` (move to Trash) |
| `⌘⌥Esc` | `Ctrl+Shift+Esc` (Task Manager) |
| `⌘⌃Q` | Lock Windows |

The Mac-only extras (the rows below `⌘Tab`) can be turned off with **Extra Mac shortcuts** if you want a pure `⌘ → Ctrl` remap.

---

## Keeping the real Windows key

When Mac Mode is on, the key in the `⌘` position is no longer the Windows key. Two escape hatches are available:

- **Caps Lock = Windows key** (optional, off by default). Tap Caps Lock, then `E` to open Explorer, `D` for the desktop, `L` to lock, and so on.
- **Right Win key** stays a real Windows key when you choose **PC keyboard**. (The left Win key is the one moved into the Option position.)

You can also toggle Mac Mode off with **Ctrl + Alt + M** whenever you need stock Windows behavior.

---

## Notes and limitations

- **Secure screens are off-limits.** `Ctrl+Alt+Del`, UAC prompts, the login screen, and the lock screen are handled by Windows before any app sees them. No user-mode remapper can change those.
- **Administrator apps.** Windows blocks a normal app from sending input to an elevated window. If shortcuts stop working inside an admin app, choose **Restart as administrator** from the tray menu.
- **Games using raw input** may ignore synthetic keystrokes. Toggle Mac Mode off for those.
- **Antivirus / SmartScreen.** The `.exe` is unsigned, and keyboard hooks are the same mechanism used by automation tools, so SmartScreen may warn on first launch. The full source is in `src/` if you want to build it yourself.
- **Settings** live in `%APPDATA%\MacKey\settings.ini`. Delete that folder to reset.
- **Uninstall:** choose **Exit** from the tray menu, turn off **Start with Windows**, then delete the `MacKey` folder.

---

## How it works

MacKey installs a `WH_KEYBOARD_LL` low-level keyboard hook while it is running. When Mac Mode is on:

1. The physical key in the `⌘` position is swallowed and treated as a modifier.
2. A normal key pressed while `⌘` is held is re-sent as `Ctrl` + that key through `SendInput`.
3. A small table of Mac-only chords is translated first (for example `⌘Tab` → a held `Alt+Tab`).
4. Everything else passes through untouched.

When Mac Mode is off, the hook forwards every event without changing it.

---

## Project layout

```
MacKey/
  build.ps1            # compiles dist\MacKey.exe with the in-box C# compiler
  run.ps1              # builds if needed, then launches
  src/
    Program.cs         # single-instance entry point
    AppController.cs   # settings, tray, autostart, lifecycle
    KeyboardEngine.cs  # low-level hook and remapping rules
    MainForm.cs        # the window
    ShortcutsForm.cs   # the mapping reference dialog
    Controls.cs        # custom switch / card / row controls
    Theme.cs           # colors and rounded-rect helpers
    Settings.cs        # %APPDATA%\MacKey\settings.ini
    Native.cs          # P/Invoke declarations
    app.manifest       # DPI awareness + Windows 10/11 compatibility
  assets/app.ico       # generated by tools\make-icon.ps1
  dist/MacKey.exe      # build output
```

No external NuGet packages, no runtime to install, no telemetry.
