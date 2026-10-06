using System.Text.Json;

namespace FirawynixWindowManager;

internal static class MonitorNames
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Firawynix", "WindowManager", "monitors.json");
    private static readonly Dictionary<string, string> names = Load();

    private static Dictionary<string, string> Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath))
                    ?? new(StringComparer.OrdinalIgnoreCase)
                : new(StringComparer.OrdinalIgnoreCase);
        }
        catch { return new(StringComparer.OrdinalIgnoreCase); }
    }

    public static string Name(Screen screen)
    {
        var index = Array.FindIndex(Screen.AllScreens, s =>
            string.Equals(s.DeviceName, screen.DeviceName, StringComparison.OrdinalIgnoreCase)) + 1;
        var fallback = $"Monitor {Math.Max(index, 1)}";
        return names.TryGetValue(screen.DeviceName, out var name) &&
               !string.IsNullOrWhiteSpace(name) ? name : fallback;
    }

    public static string Description(Screen screen)
    {
        var name = Name(screen);
        var resolution = $"{screen.Bounds.Width} × {screen.Bounds.Height}";
        return $"{name} · {resolution}{(screen.Primary ? " · Principal" : "")}";
    }

    public static string Description(string deviceName)
    {
        var screen = Screen.AllScreens.FirstOrDefault(s =>
            string.Equals(s.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        return screen is null ? $"Monitor desconectado ({deviceName})" : Description(screen);
    }

    public static void Rename(Screen screen, string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0) names.Remove(screen.DeviceName);
        else names[screen.DeviceName] = trimmed;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(names,
            new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, FilePath, true);
    }
}
