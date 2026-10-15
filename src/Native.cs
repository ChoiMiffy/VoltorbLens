using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Imaging;

namespace VoltorbLens;

public sealed record WindowItem(nint Handle, string Title, string Process)
{ public override string ToString() => $"{Process} — {Title}"; }
public static class Native
{
    public delegate bool EnumProc(nint h,nint p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb,nint p);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(nint h);
    [DllImport("user32.dll")] public static extern bool IsWindow(nint h);
    [DllImport("user32.dll")] public static extern bool IsIconic(nint h);
    [DllImport("user32.dll")] public static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(nint h);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(nint h,StringBuilder b,int n);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(nint h,out uint pid);
    [DllImport("user32.dll")] static extern bool GetClientRect(nint h,out Rect r);
    [DllImport("user32.dll")] static extern bool ClientToScreen(nint h,ref Point p);
    [DllImport("user32.dll",SetLastError=true)] public static extern bool SetWindowDisplayAffinity(nint h,uint affinity);
    [DllImport("dwmapi.dll")] public static extern int DwmFlush();
    [DllImport("user32.dll")] public static extern nint WindowFromPoint(Point p);
    [DllImport("user32.dll")] public static extern nint GetAncestor(nint h,uint flags);
    [DllImport("user32.dll")] static extern nint GetDC(nint h);
    [DllImport("user32.dll")] static extern int ReleaseDC(nint h,nint dc);
    [DllImport("gdi32.dll")] static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] static extern nint SelectObject(nint dc,nint obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(nint dc);
    [DllImport("user32.dll")] static extern bool UpdateLayeredWindow(nint h,nint screen,ref Point dst,ref Size size,nint src,ref Point origin,uint key,ref Blend blend,uint flags);
    [StructLayout(LayoutKind.Sequential)] struct Rect { public int L,T,R,B; }
    [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend { public byte Op,Flags,Alpha,Format; }
    public static Rectangle ClientBounds(nint h)
    {
        if(!GetClientRect(h,out var r))return Rectangle.Empty;
        var p=new Point(); ClientToScreen(h,ref p); return new Rectangle(p.X,p.Y,r.R-r.L,r.B-r.T);
    }
    public static List<WindowItem> Windows()
    {
        var windows=new List<WindowItem>();
        EnumWindows((h,p)=> { if(!IsWindowVisible(h))return true; var title=new StringBuilder(512); GetWindowText(h,title,512);
            GetWindowThreadProcessId(h,out uint pid);
            if(title.Length==0 || pid==Environment.ProcessId)return true;
            try { windows.Add(new(h,title.ToString(),System.Diagnostics.Process.GetProcessById((int)pid).ProcessName)); } catch { }
            return true; },0);
        return windows.OrderByDescending(w=>w.Process.Contains("melon",StringComparison.OrdinalIgnoreCase)).ThenBy(w=>w.Title).ToList();
    }
    // Desktop BitBlt omits the hardware cursor. Capture exclusion is checked on each overlay.
    public static Bitmap Capture(Rectangle area)
    {
        var bitmap=new Bitmap(area.Width,area.Height,PixelFormat.Format32bppArgb);
        try { using var g=Graphics.FromImage(bitmap); g.CopyFromScreen(area.Location,Point.Empty,area.Size,CopyPixelOperation.SourceCopy); return bitmap; }
        catch { bitmap.Dispose(); throw; }
    }
    public static void Layer(nint handle, Bitmap bitmap, Point location)
    {
        var screen=GetDC(0); var mem=CreateCompatibleDC(screen); var bmp=bitmap.GetHbitmap(Color.FromArgb(0)); var old=SelectObject(mem,bmp);
        try { var size=bitmap.Size; var origin=Point.Empty; var blend=new Blend {Alpha=255,Format=1};
            if(!UpdateLayeredWindow(handle,screen,ref location,ref size,mem,ref origin,0,ref blend,2)) throw new System.ComponentModel.Win32Exception(); }
        finally { SelectObject(mem,old); DeleteObject(bmp); DeleteDC(mem); ReleaseDC(0,screen); }
    }
}
