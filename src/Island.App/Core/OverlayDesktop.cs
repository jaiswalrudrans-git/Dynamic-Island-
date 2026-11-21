using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Island.App.Core;

internal static class OverlayDesktop
{
    private const int ExtendedStyle = -20;
    private const long ToolWindow = 0x80, AppWindow = 0x40000, NoActivate = 0x08000000, Transparent = 0x20;
    private const uint NoSize = 0x1, NoMove = 0x2, NoZOrder = 0x4, KeepFocus = 0x10, FrameChanged = 0x20;

    internal static void Configure(nint hwnd, bool rendered = false, bool typing = false)
    {
        var original = GetWindowLongPtrW(hwnd, ExtendedStyle);
        // Before WPF submits its first alpha surface, the entire layered rectangle can receive input.
        long style = ((long)original | ToolWindow) & ~AppWindow;
        if (typing) style &= ~NoActivate;
        else style |= NoActivate;
        style = rendered ? style & ~Transparent : style | Transparent;
        if ((long)original == style) return;
        Marshal.SetLastPInvokeError(0);
        var previous = SetWindowLongPtrW(hwnd, ExtendedStyle, (nint)style);
        if (previous == 0 && Marshal.GetLastPInvokeError() != 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        uint flags = NoSize | NoMove | NoZOrder | FrameChanged;
        if (!typing) flags |= KeepFocus;
        if (!SetWindowPos(hwnd, 0, 0, 0, 0, 0, flags))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    internal static void Place(nint hwnd, string? deviceName)
    {
        MonitorInfo? primary = null, selected = null;
        if (!EnumDisplayMonitors(0, 0, (monitor, _, _, _) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfoW(monitor, ref info))
            {
                if ((info.Flags & 1) != 0) primary = info;
                if (string.Equals(deviceName, info.Device, StringComparison.OrdinalIgnoreCase)) selected = info;
            }
            return true;
        }, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        if ((selected ?? primary) is not { } display) return;
        if (!GetWindowRect(hwnd, out var window)) throw new Win32Exception(Marshal.GetLastPInvokeError());

        // Both rectangles are in physical virtual-desktop pixels. WPF owns the
        // DIP size and rescales it on WM_DPICHANGED; center again after that resize.
        int left = display.Bounds.Left + (display.Bounds.Width - window.Width) / 2;
        if (window.Left == left && window.Top == display.Bounds.Top) return;
        if (!SetWindowPos(hwnd, 0, left, display.Bounds.Top, 0, 0, NoSize | NoZOrder | KeepFocus))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
        public readonly int Width => Right - Left;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Bounds, WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Device;
    }

    private delegate bool MonitorCallback(nint monitor, nint dc, nint bounds, nint data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetWindowLongPtrW(nint hwnd, int index);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtrW(nint hwnd, int index, nint style);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(nint dc, nint clip, MonitorCallback callback, nint data);
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern bool GetMonitorInfoW(nint monitor, ref MonitorInfo info);
}
