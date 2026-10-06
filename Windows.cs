using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FirawynixWindowManager;

internal sealed record WindowInfo(nint Handle, uint ProcessId, string Title, string ClassName,
    string ExecutablePath, Rectangle Bounds)
{
    public string Display => $"{(Title.Length == 0 ? "(sem título)" : Title)}  ·  {Path.GetFileName(ExecutablePath)}  ·  {ClassName}";
}

internal static class NativeWindows
{
    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpFrameChanged = 0x0020;
    internal const int SwRestore = 9;
    internal const int SwMaximize = 3;
    internal const int GwlStyle = -16;
    internal const int WsCaption = 0x00C00000;
    internal const int WsThickFrame = 0x00040000;
    internal const int WsBorder = 0x00800000;
    internal const int WsDlgFrame = 0x00400000;
    internal const int WsPopup = unchecked((int)0x80000000);

    internal delegate bool EnumWindowsCallback(nint handle, nint parameter);

    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowsCallback callback, nint parameter);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint handle);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint handle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowTextW(nint handle, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassNameW(nint handle, StringBuilder text, int max);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint handle, out uint processId);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint handle, out Rect rect);
    [DllImport("user32.dll")] internal static extern bool ShowWindow(nint handle, int command);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool SetWindowPos(nint handle, nint after,
        int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)] internal static extern int GetWindowStyle(nint handle, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)] internal static extern int SetWindowStyle(nint handle, int index, int value);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint OpenProcess(uint access, bool inherit, uint processId);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryFullProcessImageNameW(
        nint process, int flags, StringBuilder path, ref int size);
    [DllImport("ntdll.dll")] private static extern int NtQueryInformationProcess(nint process,
        int informationClass, out ProcessBasicInformation information, int length, out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public nint Reserved1;
        public nint PebBaseAddress;
        public nint Reserved2A;
        public nint Reserved2B;
        public nint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left, Top, Right, Bottom;
        public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    internal static string GetExecutablePath(uint processId)
    {
        var process = OpenProcess(0x1000, false, processId);
        if (process == 0) return "";
        try
        {
            var path = new StringBuilder(32768);
            var length = path.Capacity;
            return QueryFullProcessImageNameW(process, 0, path, ref length) ? path.ToString() : "";
        }
        finally { CloseHandle(process); }
    }

    internal static bool HasAncestorExecutable(uint processId, string executablePath)
    {
        var visited = new HashSet<uint> { processId };
        for (var depth = 0; depth < 10; depth++)
        {
            var process = OpenProcess(0x1000, false, processId);
            if (process == 0) return false;
            uint parentId;
            try
            {
                if (NtQueryInformationProcess(process, 0, out var information,
                    Marshal.SizeOf<ProcessBasicInformation>(), out _) != 0) return false;
                parentId = unchecked((uint)information.InheritedFromUniqueProcessId.ToInt64());
            }
            finally { CloseHandle(process); }
            if (parentId == 0 || !visited.Add(parentId)) return false;
            if (string.Equals(GetExecutablePath(parentId), executablePath,
                StringComparison.OrdinalIgnoreCase)) return true;
            processId = parentId;
        }
        return false;
    }
}

internal static class WindowCatalog
{
    public static List<WindowInfo> Enumerate()
    {
        var result = new List<WindowInfo>();
        NativeWindows.EnumWindows((handle, _) =>
        {
            if (!NativeWindows.IsWindowVisible(handle) ||
                !NativeWindows.GetWindowRect(handle, out var rect) ||
                rect.Right - rect.Left < 80 || rect.Bottom - rect.Top < 60)
                return true;

            NativeWindows.GetWindowThreadProcessId(handle, out var processId);
            if (processId == Environment.ProcessId || processId == 0) return true;
            var path = NativeWindows.GetExecutablePath(processId);
            if (path.Length == 0) return true;

            var title = new StringBuilder(512);
            var className = new StringBuilder(256);
            NativeWindows.GetWindowTextW(handle, title, title.Capacity);
            NativeWindows.GetClassNameW(handle, className, className.Capacity);
            if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or
                "Shell_SecondaryTrayWnd" or "NotifyIconOverflowWindow") return true;
            result.Add(new WindowInfo(handle, processId, title.ToString(), className.ToString(),
                path, rect.ToRectangle()));
            return true;
        }, 0);
        return result;
    }
}

internal sealed class WindowPlacer
{
    private readonly Dictionary<nint, (int Style, Rectangle Bounds)> borderless = [];

    public bool Apply(WindowInfo window, WindowRule rule)
    {
        if (!NativeWindows.IsWindow(window.Handle)) return false;
        var screen = Screen.AllScreens.FirstOrDefault(s =>
            string.Equals(s.DeviceName, rule.Monitor, StringComparison.OrdinalIgnoreCase))
            ?? Screen.FromRectangle(window.Bounds);

        NativeWindows.ShowWindow(window.Handle, NativeWindows.SwRestore);
        if (rule.Placement == PlacementMode.FullMonitor)
        {
            if (!borderless.ContainsKey(window.Handle))
            {
                var original = NativeWindows.GetWindowStyle(window.Handle, NativeWindows.GwlStyle);
                borderless[window.Handle] = (original, window.Bounds);
            }
            var style = borderless[window.Handle].Style;
            var fullStyle = (style & ~(NativeWindows.WsCaption | NativeWindows.WsThickFrame |
                NativeWindows.WsBorder | NativeWindows.WsDlgFrame)) | NativeWindows.WsPopup;
            NativeWindows.SetWindowStyle(window.Handle, NativeWindows.GwlStyle, fullStyle);
            var bounds = screen.Bounds;
            return NativeWindows.SetWindowPos(window.Handle, 0, bounds.X, bounds.Y,
                bounds.Width, bounds.Height, NativeWindows.SwpNoZOrder |
                NativeWindows.SwpNoActivate | NativeWindows.SwpFrameChanged);
        }

        RestoreStyle(window.Handle);
        var target = rule.Area.ToRectangle(screen.WorkingArea);
        return NativeWindows.SetWindowPos(window.Handle, 0, target.X, target.Y,
            target.Width, target.Height, NativeWindows.SwpNoZOrder | NativeWindows.SwpNoActivate |
            NativeWindows.SwpFrameChanged);
    }

    public void Prune()
    {
        foreach (var handle in borderless.Keys.Where(h => !NativeWindows.IsWindow(h)).ToArray())
            borderless.Remove(handle);
    }

    public void RestoreAll()
    {
        foreach (var handle in borderless.Keys.ToArray()) RestoreStyle(handle);
    }

    private void RestoreStyle(nint handle)
    {
        if (!borderless.Remove(handle, out var previous) || !NativeWindows.IsWindow(handle)) return;
        NativeWindows.SetWindowStyle(handle, NativeWindows.GwlStyle, previous.Style);
        var bounds = previous.Bounds;
        NativeWindows.SetWindowPos(handle, 0, bounds.X, bounds.Y, bounds.Width, bounds.Height,
            NativeWindows.SwpNoZOrder | NativeWindows.SwpNoActivate | NativeWindows.SwpFrameChanged);
    }
}
