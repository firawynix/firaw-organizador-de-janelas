using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace FirawynixWindowManager;

public partial class MonitorPickerWindow : Window
{
    private sealed record Entry(Forms.Screen Screen, int Number, string Name, string Details);
    private readonly string? selectedDevice;
    public Forms.Screen? SelectedMonitor => (MonitorsList.SelectedItem as Entry)?.Screen;

    public MonitorPickerWindow(string? selectedDevice = null)
    {
        this.selectedDevice = selectedDevice;
        InitializeComponent();
        UiBranding.Apply(this);
        RefreshMonitors();
    }

    private void RefreshMonitors()
    {
        var current = SelectedMonitor?.DeviceName ?? selectedDevice;
        var entries = Forms.Screen.AllScreens.Select((screen, index) => new Entry(screen, index + 1,
            MonitorNames.Name(screen),
            $"{screen.Bounds.Width} × {screen.Bounds.Height}  ·  {screen.DeviceName}" +
            (screen.Primary ? "  ·  Principal" : ""))).ToList();
        MonitorsList.ItemsSource = entries;
        MonitorsList.SelectedItem = entries.FirstOrDefault(e => string.Equals(e.Screen.DeviceName,
            current, StringComparison.OrdinalIgnoreCase)) ?? entries.FirstOrDefault();
    }

    private async void Identify_Click(object sender, RoutedEventArgs e)
    {
        var overlays = new List<Forms.Form>();
        Hide();
        try
        {
            for (var index = 0; index < Forms.Screen.AllScreens.Length; index++)
            {
                var screen = Forms.Screen.AllScreens[index];
                var overlay = new Forms.Form
                {
                    FormBorderStyle = Forms.FormBorderStyle.None,
                    StartPosition = Forms.FormStartPosition.Manual,
                    ShowInTaskbar = false,
                    TopMost = true,
                    BackColor = System.Drawing.Color.FromArgb(25, 211, 230),
                    Opacity = 0.95,
                    Size = new System.Drawing.Size(360, 220),
                    Location = new System.Drawing.Point(
                        screen.Bounds.Left + (screen.Bounds.Width - 360) / 2,
                        screen.Bounds.Top + (screen.Bounds.Height - 220) / 2)
                };
                var number = new Forms.Label
                {
                    Text = (index + 1).ToString(), Dock = Forms.DockStyle.Top,
                    Height = 125, TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Font = new System.Drawing.Font("Segoe UI", 70, System.Drawing.FontStyle.Bold),
                    ForeColor = System.Drawing.Color.FromArgb(12, 17, 23)
                };
                var name = new Forms.Label
                {
                    Text = MonitorNames.Name(screen), Dock = Forms.DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.TopCenter,
                    Font = new System.Drawing.Font("Segoe UI", 16, System.Drawing.FontStyle.Bold),
                    ForeColor = System.Drawing.Color.FromArgb(12, 17, 23)
                };
                overlay.Controls.Add(name);
                overlay.Controls.Add(number);
                overlays.Add(overlay);
                overlay.Show();
            }
            await Task.Delay(2600);
        }
        finally
        {
            foreach (var overlay in overlays) { overlay.Close(); overlay.Dispose(); }
            Show();
            Activate();
        }
    }

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMonitor is not { } screen) return;
        var prompt = new NamePromptWindow(MonitorNames.Name(screen)) { Owner = this };
        if (prompt.ShowDialog() != true) return;
        try { MonitorNames.Rename(screen, prompt.Value); RefreshMonitors(); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"Não foi possível salvar o nome:\n{ex.Message}",
                "Firaw", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMonitor is not null) DialogResult = true;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Monitors_DoubleClick(object sender, MouseButtonEventArgs e) => Confirm_Click(sender,
        new RoutedEventArgs());
}
