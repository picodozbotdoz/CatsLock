using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class CatLock : Form
{
    const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
    delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int id, HookProc fn, IntPtr mod, uint tid);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr h, int code, IntPtr w, IntPtr l);
    [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string name);

    IntPtr hook;
    HookProc proc;           // keep a reference so the GC doesn't collect it
    bool locked, ctrl, alt;
    Button btn = new Button();
    Label info = new Label();

    CatLock()
    {
        Text = "CatLock";
        ClientSize = new Size(260, 120);
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        btn.Dock = DockStyle.Fill;
        btn.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        btn.FlatStyle = FlatStyle.Flat;
        btn.Click += (s, e) => SetLocked(!locked);
        info.Dock = DockStyle.Bottom;
        info.Height = 28;
        info.TextAlign = ContentAlignment.MiddleCenter;
        info.Text = "Hotkey to lock: Ctrl+Alt+K";
        Controls.Add(btn);
        Controls.Add(info);

        proc = Hook;
        hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(null), 0);
        SetLocked(false);
    }

    void SetLocked(bool v)
    {
        locked = v;
        btn.Text = v ? "LOCKED\r\nclick to unlock" : "Click to LOCK";
        btn.BackColor = v ? Color.IndianRed : Color.MediumSeaGreen;
        btn.ForeColor = Color.White;
        info.Text = v ? "Keyboard blocked. Use the mouse to unlock." : "Hotkey to lock: Ctrl+Alt+K";
    }

    IntPtr Hook(int code, IntPtr w, IntPtr l)
    {
        if (code >= 0)
        {
            int msg = (int)w;
            int vk = Marshal.ReadInt32(l);   // KBDLLHOOKSTRUCT.vkCode
            bool down = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool up = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            // track modifiers ourselves, even for swallowed keys
            if (vk == 0xA2 || vk == 0xA3 || vk == 0x11) ctrl = down;
            else if (vk == 0xA4 || vk == 0xA5 || vk == 0x12) alt = down;
            else if (down && vk == 0x4B && ctrl && alt && !locked)
            {
                BeginInvoke((Action)(() => SetLocked(true)));
                return (IntPtr)1;
            }

            if (locked && (down || up)) return (IntPtr)1;
        }
        return CallNextHookEx(hook, code, w, l);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnhookWindowsHookEx(hook);
        base.OnFormClosed(e);
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new CatLock());
    }
}
