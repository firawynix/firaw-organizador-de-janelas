using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace FirawynixWindowManager;

internal static class UiBranding
{
    public static void Apply(Window window)
    {
        window.SourceInitialized += (_, _) => ApplyCaption(window);
        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        if (icon is null) return;
        var image = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
        image.Freeze();
        window.Icon = image;
    }

    private static void ApplyCaption(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == 0) return;
        var caption = 0x00E6D319;
        var text = 0x00141007;
        _ = DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint windowHandle, int attribute,
        ref int value, int size);
}
