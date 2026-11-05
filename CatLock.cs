using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
    NotifyIcon tray = new NotifyIcon();

    CatLock()
    {
        Text = "CatLock";
        ClientSize = new Size(260, 120);
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Icon = tray.Icon = MakeIcon();
        tray.Text = "CatLock";
        tray.Visible = true;
        tray.DoubleClick += (s, e) => ShowWindow();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show", null, (s, e) => ShowWindow());
        menu.Items.Add("Exit", null, (s, e) => Close());
        tray.ContextMenuStrip = menu;

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
        if (v) ShowWindow();   // the mouse unlock button must be reachable
    }

    void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    // minimizing hides to the tray, but only while unlocked
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState != FormWindowState.Minimized) return;
        if (locked) WindowState = FormWindowState.Normal;
        else Hide();
    }

    static Icon MakeIcon() { return MakeIcon(32); }

    static Icon MakeIcon(int n)
    {
        using (var bmp = new Bitmap(n, n))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float u = n / 32f;
            var fur = new SolidBrush(Color.FromArgb(240, 150, 60));
            var dark = new SolidBrush(Color.FromArgb(40, 40, 40));
            g.FillPolygon(fur, new[] { new PointF(3 * u, 14 * u), new PointF(5 * u, 2 * u), new PointF(14 * u, 8 * u) });
            g.FillPolygon(fur, new[] { new PointF(29 * u, 14 * u), new PointF(27 * u, 2 * u), new PointF(18 * u, 8 * u) });
            g.FillPolygon(Brushes.Pink, new[] { new PointF(6 * u, 11 * u), new PointF(6.5f * u, 6 * u), new PointF(11 * u, 9 * u) });
            g.FillPolygon(Brushes.Pink, new[] { new PointF(26 * u, 11 * u), new PointF(25.5f * u, 6 * u), new PointF(21 * u, 9 * u) });
            g.FillEllipse(fur, 2 * u, 7 * u, 28 * u, 23 * u);
            g.FillEllipse(Brushes.White, 7 * u, 13 * u, 7 * u, 8 * u);
            g.FillEllipse(Brushes.White, 18 * u, 13 * u, 7 * u, 8 * u);
            g.FillEllipse(dark, 9.5f * u, 14 * u, 3 * u, 6 * u);
            g.FillEllipse(dark, 20.5f * u, 14 * u, 3 * u, 6 * u);
            g.FillPolygon(Brushes.HotPink, new[] { new PointF(14 * u, 22 * u), new PointF(18 * u, 22 * u), new PointF(16 * u, 25 * u) });
            using (var pen = new Pen(dark, Math.Max(1f, u)))
            {
                g.DrawLine(pen, 16 * u, 25 * u, 13 * u, 27 * u);
                g.DrawLine(pen, 16 * u, 25 * u, 19 * u, 27 * u);
                g.DrawLine(pen, 2 * u, 21 * u, 9 * u, 23 * u);
                g.DrawLine(pen, 2 * u, 26 * u, 9 * u, 25 * u);
                g.DrawLine(pen, 30 * u, 21 * u, 23 * u, 23 * u);
                g.DrawLine(pen, 30 * u, 26 * u, 23 * u, 25 * u);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    // writes a 256px PNG-in-ICO file, used to embed the icon in the exe at build time
    static void WriteIco(string path)
    {
        var png = new MemoryStream();
        using (var bmp = new Bitmap(MakeIcon(256).ToBitmap())) bmp.Save(png, System.Drawing.Imaging.ImageFormat.Png);
        using (var w = new BinaryWriter(File.Create(path)))
        {
            w.Write((short)0); w.Write((short)1); w.Write((short)1);
            w.Write((byte)0); w.Write((byte)0); w.Write((byte)0); w.Write((byte)0);
            w.Write((short)1); w.Write((short)32);
            w.Write((int)png.Length); w.Write(22);
            w.Write(png.ToArray());
        }
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
        tray.Visible = false;
        tray.Dispose();
        base.OnFormClosed(e);
    }

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--make-ico") { WriteIco(args[1]); return; }
        Application.EnableVisualStyles();
        Application.Run(new CatLock());
    }
}
