using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using Microsoft.Win32;
using Island.Core.Clipboard;

namespace Island.App.Features.Clipboard.Services;

public static class ClipboardNative
{
    public const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AddClipboardFormatListener(nint hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RemoveClipboardFormatListener(nint hwnd);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool OpenClipboard(nint hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    public static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("user32.dll")]
    public static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll")]
    public static extern nint GetClipboardOwner();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll")]
    public static extern nint GlobalLock(nint hMem);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalUnlock(nint hMem);

    [DllImport("kernel32.dll")]
    public static extern nuint GlobalSize(nint hMem);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint RegisterWindowMessage(string lpString);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern uint DragQueryFile(nint hDrop, uint iFile, [Out] StringBuilder? lpszFile, uint cch);

    public static string? GetProcessNameForWindow(nint hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0) return null;
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    public static bool HasExcludedClipboardFormat()
    {
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try
        {
            uint exFormat = RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
            if (exFormat != 0 && IsClipboardFormatAvailable(exFormat))
            {
                return true;
            }

            uint canHistFormat = RegisterClipboardFormat("CanIncludeInClipboardHistory");
            if (canHistFormat != 0 && IsClipboardFormatAvailable(canHistFormat))
            {
                nint hData = GetClipboardData(canHistFormat);
                if (hData != IntPtr.Zero)
                {
                    nint ptr = GlobalLock(hData);
                    if (ptr != IntPtr.Zero)
                    {
                        try
                        {
                            uint val = unchecked((uint)Marshal.ReadInt32(ptr));
                            if (val == 0) return true;
                        }
                        finally
                        {
                            GlobalUnlock(hData);
                        }
                    }
                }
            }

            uint canCloudFormat = RegisterClipboardFormat("CanUploadToCloudClipboard");
            if (canCloudFormat != 0 && IsClipboardFormatAvailable(canCloudFormat))
            {
                nint hData = GetClipboardData(canCloudFormat);
                if (hData != IntPtr.Zero)
                {
                    nint ptr = GlobalLock(hData);
                    if (ptr != IntPtr.Zero)
                    {
                        try
                        {
                            uint val = unchecked((uint)Marshal.ReadInt32(ptr));
                            if (val == 0) return true;
                        }
                        finally
                        {
                            GlobalUnlock(hData);
                        }
                    }
                }
            }

            return false;
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static void SendCtrlV()
    {
        const ushort VK_CONTROL = 0x11;
        const ushort VK_V = 0x56;
        const uint KEYEVENTF_KEYUP = 0x0002;

        INPUT[] inputs = new INPUT[4];

        // Ctrl DOWN
        inputs[0].type = 1; // INPUT_KEYBOARD
        inputs[0].u.ki.wVk = VK_CONTROL;

        // V DOWN
        inputs[1].type = 1;
        inputs[1].u.ki.wVk = VK_V;

        // V UP
        inputs[2].type = 1;
        inputs[2].u.ki.wVk = VK_V;
        inputs[2].u.ki.dwFlags = KEYEVENTF_KEYUP;

        // Ctrl UP
        inputs[3].type = 1;
        inputs[3].u.ki.wVk = VK_CONTROL;
        inputs[3].u.ki.dwFlags = KEYEVENTF_KEYUP;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}

public sealed class ClipboardListener : IDisposable
{
    private const uint CF_HDROP = 15;
    private const uint CF_DIB = 8;
    private const uint CF_DIBV5 = 17;
    private const uint CF_UNICODETEXT = 13;

    private static readonly string[] WhitelistedBrowsers = ["brave", "chrome", "msedge", "firefox", "opera", "vivaldi"];
    private static readonly string[] IgnoredPasswordManagers = ["1password", "bitwarden", "keepass", "keepassxc", "lastpass", "dashlane"];

    private readonly ClipboardStore store;
    private readonly Func<bool>? isPaused;
    private readonly Action<ClipboardItem>? onItemCaptured;
    private readonly uint taskbarCreatedMessage;

    private HwndSource? hwndSource;
    private CancellationTokenSource? debounceCts;
    private bool disposed;

    private static uint ignoredSequence;
    private static DateTime ignoredSequenceExpiry = DateTime.MinValue;

    public ClipboardListener(
        ClipboardStore store,
        Func<bool>? isPaused = null,
        Action<ClipboardItem>? onItemCaptured = null)
    {
        this.store = store;
        this.isPaused = isPaused;
        this.onItemCaptured = onItemCaptured;
        this.taskbarCreatedMessage = ClipboardNative.RegisterWindowMessage("TaskbarCreated");

        CreateHwndAndListen();

        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public static void IgnoreNextSequence(uint seq)
    {
        ignoredSequence = seq;
        ignoredSequenceExpiry = DateTime.UtcNow.AddSeconds(1);
    }

    public static void IgnoreCurrentOrNextSequence()
    {
        ignoredSequence = ClipboardNative.GetClipboardSequenceNumber() + 1;
        ignoredSequenceExpiry = DateTime.UtcNow.AddSeconds(1);
    }

    private void CreateHwndAndListen()
    {
        try
        {
            var parameters = new HwndSourceParameters("IslandClipboardListenerHwnd")
            {
                WindowStyle = 0,
                ExtendedWindowStyle = 0,
                Height = 0,
                Width = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE
            };
            hwndSource = new HwndSource(parameters);
            hwndSource.AddHook(HwndHook);
            RegisterListener();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[ClipboardListener] Failed to create HWND source: {ex.Message}");
        }
    }

    private void RegisterListener()
    {
        if (hwndSource != null && hwndSource.Handle != IntPtr.Zero)
        {
            ClipboardNative.RemoveClipboardFormatListener(hwndSource.Handle);
            ClipboardNative.AddClipboardFormatListener(hwndSource.Handle);
            Trace.WriteLine($"[ClipboardListener] Registered AddClipboardFormatListener on {hwndSource.Handle:X}");
        }
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            Trace.WriteLine("[ClipboardListener] Session unlock detected, re-registering listener");
            RegisterListener();
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Trace.WriteLine("[ClipboardListener] Power resume detected, re-registering listener");
            RegisterListener();
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Trace.WriteLine("[ClipboardListener] Display settings changed, re-registering listener");
        RegisterListener();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((uint)msg == taskbarCreatedMessage && taskbarCreatedMessage != 0)
        {
            Trace.WriteLine("[ClipboardListener] TaskbarCreated received, re-registering listener");
            RegisterListener();
            return IntPtr.Zero;
        }

        if (msg == ClipboardNative.WM_CLIPBOARDUPDATE)
        {
            Trace.WriteLine("[ClipboardListener] clipboard update received");
            Console.WriteLine("[ClipboardListener] clipboard update received");

            debounceCts?.Cancel();
            var cts = new CancellationTokenSource();
            debounceCts = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(50, cts.Token);
                    if (!cts.Token.IsCancellationRequested)
                    {
                        ProcessClipboardUpdate();
                    }
                }
                catch (OperationCanceledException) { }
            }, cts.Token);

            handled = true;
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    private void ProcessClipboardUpdate()
    {
        if (disposed) return;

        if (isPaused?.Invoke() == true || store.IsPaused)
        {
            Trace.WriteLine("[ClipboardListener] Skipped: paused");
            return;
        }

        uint currentSeq = ClipboardNative.GetClipboardSequenceNumber();
        if (currentSeq == ignoredSequence && DateTime.UtcNow < ignoredSequenceExpiry)
        {
            Trace.WriteLine("[ClipboardListener] Skipped: our own paste");
            return;
        }

        if (ClipboardNative.HasExcludedClipboardFormat())
        {
            Trace.WriteLine("[ClipboardListener] Skipped: excluded format");
            return;
        }

        IntPtr ownerHwnd = ClipboardNative.GetClipboardOwner();
        string? appName = ClipboardNative.GetProcessNameForWindow(ownerHwnd);
        if (string.IsNullOrEmpty(appName))
        {
            IntPtr fgHwnd = ClipboardNative.GetForegroundWindow();
            appName = ClipboardNative.GetProcessNameForWindow(fgHwnd);
        }

        if (!string.IsNullOrEmpty(appName))
        {
            string appLower = appName.ToLowerInvariant();
            bool isBrowser = WhitelistedBrowsers.Any(b => appLower.Contains(b));
            if (!isBrowser && IgnoredPasswordManagers.Any(p => appLower.Contains(p)))
            {
                Trace.WriteLine($"[ClipboardListener] Skipped: ignored app ({appName})");
                return;
            }
        }

        bool opened = false;
        for (int attempt = 1; attempt <= 6; attempt++)
        {
            if (ClipboardNative.OpenClipboard(IntPtr.Zero))
            {
                opened = true;
                break;
            }
            Thread.Sleep(30 + attempt * 5);
        }

        if (!opened)
        {
            Trace.WriteLine("[ClipboardListener] Failed to open clipboard after 6 retries (CLIPBRD_E_CANT_OPEN)");
            return;
        }

        string? text = null;
        string? html = null;
        string[]? files = null;
        byte[]? imageBytes = null;

        try
        {
            if (ClipboardNative.IsClipboardFormatAvailable(CF_HDROP))
            {
                IntPtr hDrop = ClipboardNative.GetClipboardData(CF_HDROP);
                if (hDrop != IntPtr.Zero)
                {
                    uint fileCount = ClipboardNative.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                    if (fileCount > 0)
                    {
                        var fileList = new string[fileCount];
                        var sb = new StringBuilder(520);
                        for (uint i = 0; i < fileCount; i++)
                        {
                            sb.Clear();
                            ClipboardNative.DragQueryFile(hDrop, i, sb, (uint)sb.Capacity);
                            fileList[i] = sb.ToString();
                        }
                        files = fileList;
                    }
                }
            }

            uint cfPng = ClipboardNative.RegisterClipboardFormat("PNG");
            if (cfPng != 0 && ClipboardNative.IsClipboardFormatAvailable(cfPng))
            {
                IntPtr hData = ClipboardNative.GetClipboardData(cfPng);
                if (hData != IntPtr.Zero)
                {
                    nuint size = ClipboardNative.GlobalSize(hData);
                    if (size > 0)
                    {
                        IntPtr ptr = ClipboardNative.GlobalLock(hData);
                        if (ptr != IntPtr.Zero)
                        {
                            try
                            {
                                imageBytes = new byte[size];
                                Marshal.Copy(ptr, imageBytes, 0, (int)size);
                            }
                            finally
                            {
                                ClipboardNative.GlobalUnlock(hData);
                            }
                        }
                    }
                }
            }
            else if (ClipboardNative.IsClipboardFormatAvailable(CF_DIBV5) || ClipboardNative.IsClipboardFormatAvailable(CF_DIB))
            {
                uint dibFormat = ClipboardNative.IsClipboardFormatAvailable(CF_DIBV5) ? CF_DIBV5 : CF_DIB;
                IntPtr hData = ClipboardNative.GetClipboardData(dibFormat);
                if (hData != IntPtr.Zero)
                {
                    nuint dibSize = ClipboardNative.GlobalSize(hData);
                    if (dibSize > 0)
                    {
                        IntPtr ptr = ClipboardNative.GlobalLock(hData);
                        if (ptr != IntPtr.Zero)
                        {
                            try
                            {
                                int headerSize = Marshal.ReadInt32(ptr);
                                int bitCount = Marshal.ReadInt16(ptr + 14);
                                int clrUsed = Marshal.ReadInt32(ptr + 32);

                                int paletteColors = clrUsed;
                                if (paletteColors == 0 && bitCount <= 8)
                                {
                                    paletteColors = 1 << bitCount;
                                }
                                int paletteSize = paletteColors * 4;
                                int offBits = 14 + headerSize + paletteSize;

                                int totalBmpSize = 14 + (int)dibSize;
                                imageBytes = new byte[totalBmpSize];

                                imageBytes[0] = 0x42;
                                imageBytes[1] = 0x4D;
                                BitConverter.GetBytes(totalBmpSize).CopyTo(imageBytes, 2);
                                BitConverter.GetBytes(offBits).CopyTo(imageBytes, 10);

                                Marshal.Copy(ptr, imageBytes, 14, (int)dibSize);
                            }
                            finally
                            {
                                ClipboardNative.GlobalUnlock(hData);
                            }
                        }
                    }
                }
            }

            if (ClipboardNative.IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                IntPtr hMem = ClipboardNative.GetClipboardData(CF_UNICODETEXT);
                if (hMem != IntPtr.Zero)
                {
                    IntPtr ptr = ClipboardNative.GlobalLock(hMem);
                    if (ptr != IntPtr.Zero)
                    {
                        try
                        {
                            text = Marshal.PtrToStringUni(ptr);
                        }
                        finally
                        {
                            ClipboardNative.GlobalUnlock(hMem);
                        }
                    }
                }
            }

            uint cfHtml = ClipboardNative.RegisterClipboardFormat("HTML Format");
            if (cfHtml != 0 && ClipboardNative.IsClipboardFormatAvailable(cfHtml))
            {
                IntPtr hHtml = ClipboardNative.GetClipboardData(cfHtml);
                if (hHtml != IntPtr.Zero)
                {
                    IntPtr ptr = ClipboardNative.GlobalLock(hHtml);
                    if (ptr != IntPtr.Zero)
                    {
                        try
                        {
                            html = Marshal.PtrToStringUTF8(ptr);
                        }
                        finally
                        {
                            ClipboardNative.GlobalUnlock(hHtml);
                        }
                    }
                }
            }
        }
        finally
        {
            ClipboardNative.CloseClipboard();
        }

        if (files != null && files.Length > 0)
        {
            store.AddFiles(files, appName);
            onItemCaptured?.Invoke(store.GetItems().FirstOrDefault()!);
            return;
        }

        if (imageBytes != null && imageBytes.Length > 0)
        {
            if (imageBytes.Length > 25 * 1024 * 1024)
            {
                Trace.WriteLine("[ClipboardListener] Skipped: too large");
                return;
            }
            store.AddImage(imageBytes, appName);
            onItemCaptured?.Invoke(store.GetItems().FirstOrDefault()!);
            return;
        }

        if (!string.IsNullOrEmpty(text))
        {
            if (text.Length > 2 * 1024 * 1024)
            {
                Trace.WriteLine("[ClipboardListener] Skipped: too large");
                return;
            }

            var lastItem = store.GetItems().FirstOrDefault();
            if (lastItem != null && lastItem.Text == text)
            {
                Trace.WriteLine("[ClipboardListener] Skipped: duplicate");
                return;
            }

            if (html != null)
            {
                store.AddRichText(text, html, appName);
            }
            else
            {
                store.AddText(text, appName);
            }
            onItemCaptured?.Invoke(store.GetItems().FirstOrDefault()!);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        debounceCts?.Cancel();
        debounceCts?.Dispose();

        if (hwndSource != null)
        {
            try
            {
                if (hwndSource.Handle != IntPtr.Zero)
                {
                    ClipboardNative.RemoveClipboardFormatListener(hwndSource.Handle);
                }
                hwndSource.RemoveHook(HwndHook);
                hwndSource.Dispose();
            }
            catch { }
            hwndSource = null;
        }
    }
}
