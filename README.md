# CatsLock

A tiny Windows app that blocks all keyboard input so a cat walking on the keyboard can't type anything. The screen and mouse stay fully usable.

## Usage

1. Run `CatLock.exe`. A small always-on-top window opens.
2. **Lock:** click the button, or press `Ctrl+Alt+K`. The button turns red and every keypress is swallowed.
3. **Unlock:** click the button with the mouse. There is intentionally no keyboard unlock, so the cat can't undo it.
4. Closing the window quits the app and removes the keyboard block.

## Build

Needs only the .NET Framework that ships with Windows 10 (no SDK install):

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize /out:CatLock.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll CatLock.cs
```

## How it works

Installs a global low-level keyboard hook (`WH_KEYBOARD_LL`). While locked, the hook returns without passing key events on, so no application sees them. Modifier state is tracked inside the hook so the `Ctrl+Alt+K` lock hotkey works.

## Limits

- `Ctrl+Alt+Del` cannot be blocked by any app.
- Keys physically held at the moment of locking may keep repeating until released.
- The hook can't intercept keys in windows running as administrator unless CatLock is also run as administrator.
