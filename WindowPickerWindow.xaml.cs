using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FirawynixWindowManager;

public partial class WindowPickerWindow : Window
{
    private sealed record Entry(WindowInfo Info, string Name, string App, string Size);
    private readonly string? executableFilter;
    private List<Entry> allWindows = [];
    internal WindowInfo? SelectedWindow => (WindowsList.SelectedItem as Entry)?.Info;

    public WindowPickerWindow(string? executableFilter = null)
    {
        this.executableFilter = executableFilter;
        InitializeComponent();
        UiBranding.Apply(this);
        Loaded += (_, _) => Refresh();
    }

    private void Refresh()
    {
        allWindows = WindowCatalog.Enumerate()
            .Where(window => executableFilter is null ||
                string.Equals(window.ExecutablePath, executableFilter,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(window => Path.GetFileName(window.ExecutablePath))
            .ThenBy(window => window.Title)
            .Select(window => new Entry(window,
                window.Title.Length == 0 ? "Janela sem título" : window.Title,
                Path.GetFileNameWithoutExtension(window.ExecutablePath),
                $"{window.Bounds.Width} × {window.Bounds.Height}"))
            .ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text.Trim();
        var filtered = allWindows.Where(entry => query.Length == 0 ||
            entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            entry.App.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        WindowsList.ItemsSource = filtered;
        WindowsList.SelectedIndex = filtered.Count > 0 ? 0 : -1;
    }

    private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWindow is not null) DialogResult = true;
    }
    private void Windows_DoubleClick(object sender, MouseButtonEventArgs e) => Confirm_Click(sender,
        new RoutedEventArgs());
}
