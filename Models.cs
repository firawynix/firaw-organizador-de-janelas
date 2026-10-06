using System.Text.Json;

namespace FirawynixWindowManager;

internal enum MatchMode { All, Class, TitleContains }
internal enum PlacementMode { Area, FullMonitor }

internal sealed class WindowRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Nova regra";
    public MatchMode Match { get; set; }
    public string ClassName { get; set; } = "";
    public string TitleContains { get; set; } = "";
    public PlacementMode Placement { get; set; }
    public string Monitor { get; set; } = "";
    public NormalizedArea Area { get; set; } = new();
    public bool Enabled { get; set; } = true;

    public bool Matches(WindowInfo window) => Enabled && Match switch
    {
        MatchMode.All => true,
        MatchMode.Class => ClassName.Length > 0 &&
            string.Equals(window.ClassName, ClassName, StringComparison.OrdinalIgnoreCase),
        MatchMode.TitleContains => TitleContains.Length > 0 &&
            window.Title.Contains(TitleContains, StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    public string MatchDescription => Match switch
    {
        MatchMode.All => "Todas as janelas",
        MatchMode.Class => "Janelas do mesmo tipo",
        _ => $"Título contém: {TitleContains}"
    };
}

internal sealed class AppProfile
{
    public string Name { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public List<string> AssociatedExecutables { get; set; } = [];
    public List<WindowRule> Rules { get; set; } = [];

    public bool ContainsExecutable(string path) =>
        string.Equals(ExecutablePath, path, StringComparison.OrdinalIgnoreCase) ||
        AssociatedExecutables.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));

    public bool ContainsWindow(WindowInfo window)
    {
        if (string.Equals(ExecutablePath, window.ExecutablePath,
            StringComparison.OrdinalIgnoreCase)) return true;
        if (!AssociatedExecutables.Any(x => string.Equals(x, window.ExecutablePath,
            StringComparison.OrdinalIgnoreCase))) return false;
        var fileName = Path.GetFileName(window.ExecutablePath);
        if (fileName.Equals("msedgewebview2.exe", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("msedge.exe", StringComparison.OrdinalIgnoreCase) ||
            fileName.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase))
            return NativeWindows.HasAncestorExecutable(window.ProcessId, ExecutablePath);
        return true;
    }
}

internal sealed class NormalizedArea
{
    public double X { get; set; } = 0.1;
    public double Y { get; set; } = 0.1;
    public double Width { get; set; } = 0.8;
    public double Height { get; set; } = 0.8;

    public static NormalizedArea FromRectangle(Rectangle bounds, Rectangle work) => new()
    {
        X = (double)(bounds.Left - work.Left) / work.Width,
        Y = (double)(bounds.Top - work.Top) / work.Height,
        Width = (double)bounds.Width / work.Width,
        Height = (double)bounds.Height / work.Height
    };

    public Rectangle ToRectangle(Rectangle work)
    {
        var width = Math.Clamp((int)Math.Round(Width * work.Width), 100, work.Width);
        var height = Math.Clamp((int)Math.Round(Height * work.Height), 80, work.Height);
        var x = Math.Clamp(work.Left + (int)Math.Round(X * work.Width), work.Left, work.Right - width);
        var y = Math.Clamp(work.Top + (int)Math.Round(Y * work.Height), work.Top, work.Bottom - height);
        return new Rectangle(x, y, width, height);
    }
}

internal static class ProfileStore
{
    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Firawynix", "WindowManager", "profiles.json");

    public static List<AppProfile> Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<List<AppProfile>>(File.ReadAllText(FilePath)) ?? [];
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível ler as regras salvas:\n{ex.Message}",
                "Firawynix", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        return [];
    }

    public static void Save(List<AppProfile> profiles)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(profiles,
            new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FilePath, true);
    }
}
