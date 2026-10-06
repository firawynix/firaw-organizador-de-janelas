namespace FirawynixWindowManager;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--self-check", StringComparer.OrdinalIgnoreCase))
        {
            SelfCheck();
            return;
        }

        var preview = args.FirstOrDefault(arg => arg is "--preview" or "--preview-window" or
            "--preview-monitor" or "--preview-rule");
        if (preview is not null)
        {
            Preview(preview);
            return;
        }

        using var mutex = new Mutex(true, @"Local\FirawynixWindowManager", out var first);
        if (!first)
        {
            MessageBox.Show("O Organizador de Janelas Firaw já está aberto.",
                "Firaw", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var app = new System.Windows.Application();
        app.Run(new OrganizerWindow());
    }

    private static void Preview(string mode)
    {
        var app = new System.Windows.Application
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown
        };
        System.Windows.Window window = mode switch
        {
            "--preview-window" => new WindowPickerWindow(),
            "--preview-monitor" => new MonitorPickerWindow(),
            "--preview-rule" => new RuleEditorWindow(null, null, true),
            _ => new OrganizerWindow()
        };
        window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
        window.Left = SystemInformation.VirtualScreen.Right + 100;
        window.Top = 100;
        window.ShowInTaskbar = false;
        window.Show();
        window.UpdateLayout();

        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window);
        var image = new System.Windows.Media.Imaging.RenderTargetBitmap(
            (int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY),
            96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY,
            System.Windows.Media.PixelFormats.Pbgra32);
        image.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
        var file = mode switch
        {
            "--preview-window" => "preview-window.png",
            "--preview-monitor" => "preview-monitor.png",
            "--preview-rule" => "preview-rule.png",
            _ => "preview.png"
        };
        using (var output = File.Create(Path.Combine(AppContext.BaseDirectory, file)))
            encoder.Save(output);
        if (window is OrganizerWindow organizer) organizer.CloseForPreview();
        else window.Close();
        app.Shutdown();
    }

    private static void SelfCheck()
    {
        var screen = Screen.PrimaryScreen!;
        using var window = new Form
        {
            Opacity = 0, ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(SystemInformation.VirtualScreen.Right + 100, 100, 360, 260)
        };
        window.Show();
        Application.DoEvents();
        var original = window.Bounds;
        var info = new WindowInfo(window.Handle, (uint)Environment.ProcessId,
            "Firaw self-check", "TestWindow", Application.ExecutablePath, original);
        var placer = new WindowPlacer();
        var areaRule = new WindowRule
        {
            Monitor = screen.DeviceName,
            Area = new NormalizedArea { X = 0.1, Y = 0.1, Width = 0.4, Height = 0.4 }
        };
        var areaApplied = placer.Apply(info, areaRule);
        NativeWindows.GetWindowRect(window.Handle, out var areaRect);
        var expectedArea = areaRule.Area.ToRectangle(screen.WorkingArea);
        var areaCorrect = areaApplied &&
            Math.Abs(areaRect.Left - expectedArea.Left) <= 10 &&
            Math.Abs(areaRect.Top - expectedArea.Top) <= 10;
        var fullRule = new WindowRule
        {
            Monitor = screen.DeviceName, Placement = PlacementMode.FullMonitor
        };
        var fullApplied = placer.Apply(info, fullRule);
        NativeWindows.GetWindowRect(window.Handle, out var fullRect);
        var fullCorrect = fullApplied &&
            Math.Abs(fullRect.Left - screen.Bounds.Left) <= 10 &&
            Math.Abs(fullRect.Top - screen.Bounds.Top) <= 10 &&
            Math.Abs(fullRect.Right - screen.Bounds.Right) <= 10 &&
            Math.Abs(fullRect.Bottom - screen.Bounds.Bottom) <= 10;
        placer.RestoreAll();
        var report = System.Text.Json.JsonSerializer.Serialize(new
        {
            AreaCorrect = areaCorrect, FullMonitorCorrect = fullCorrect,
            ExpectedArea = expectedArea.ToString(), ActualArea = areaRect.ToRectangle().ToString(),
            ExpectedFull = screen.Bounds.ToString(), ActualFull = fullRect.ToRectangle().ToString()
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-check.json"), report);
        if (!areaCorrect || !fullCorrect) Environment.ExitCode = 1;
    }
}
