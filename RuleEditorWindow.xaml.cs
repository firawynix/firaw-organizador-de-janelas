using System.Windows;
using Forms = System.Windows.Forms;

namespace FirawynixWindowManager;

public partial class RuleEditorWindow : Window
{
    private readonly WindowInfo? sample;
    private string selectedMonitor;
    private NormalizedArea area;
    internal WindowRule Rule { get; }

    internal RuleEditorWindow(WindowInfo? sample, WindowRule? existing, bool isFirstRule)
    {
        this.sample = sample;
        Rule = existing is null ? new WindowRule() : new WindowRule
        {
            Id = existing.Id, Name = existing.Name, Match = existing.Match,
            ClassName = existing.ClassName, TitleContains = existing.TitleContains,
            Placement = existing.Placement, Monitor = existing.Monitor,
            Area = existing.Area, Enabled = existing.Enabled
        };
        selectedMonitor = existing?.Monitor ??
            Forms.Screen.FromRectangle(sample?.Bounds ?? Forms.Screen.PrimaryScreen!.Bounds).DeviceName;
        area = existing?.Area ?? (sample is null ? new NormalizedArea() :
            NormalizedArea.FromRectangle(sample.Bounds,
                Forms.Screen.FromRectangle(sample.Bounds).WorkingArea));

        InitializeComponent();
        UiBranding.Apply(this);
        HeadingText.Text = existing is null ? "Nova regra" : "Editar regra";
        RuleNameBox.Text = existing?.Name ?? (isFirstRule ? "Janela principal" : "Janela especial");
        TitleBox.Text = existing?.TitleContains ?? sample?.Title ?? "";
        var sampleTitle = sample?.Title ?? "";
        if (sampleTitle.Length > 70) sampleTitle = sampleTitle[..67] + "…";
        SampleText.Text = sample is null
            ? "Você pode preencher um trecho do título manualmente."
            : $"Janela capturada: {(sampleTitle.Length == 0 ? "sem título" : sampleTitle)}";
        TypeRadio.IsEnabled = !string.IsNullOrEmpty(sample?.ClassName ?? Rule.ClassName);
        switch (existing?.Match ?? (isFirstRule ? MatchMode.All : MatchMode.TitleContains))
        {
            case MatchMode.All: AllRadio.IsChecked = true; break;
            case MatchMode.Class: TypeRadio.IsChecked = true; break;
            default: TitleRadio.IsChecked = true; break;
        }
        if ((existing?.Placement ?? PlacementMode.Area) == PlacementMode.FullMonitor)
            FullRadio.IsChecked = true;
        else AreaRadio.IsChecked = true;
        UpdateOptions();
    }

    private void UpdateOptions()
    {
        if (TitleSection is null || AreaSection is null || MonitorText is null) return;
        TitleSection.Visibility = TitleRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        AreaSection.Visibility = AreaRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        MonitorText.Text = MonitorNames.Description(selectedMonitor);
        AreaText.Text = $"{area.X:P0} da esquerda · {area.Y:P0} do topo · {area.Width:P0} × {area.Height:P0}";
    }

    private void Options_Changed(object sender, RoutedEventArgs e) => UpdateOptions();

    private void ChooseMonitor_Click(object sender, RoutedEventArgs e)
    {
        var picker = new MonitorPickerWindow(selectedMonitor) { Owner = this };
        if (picker.ShowDialog() == true && picker.SelectedMonitor is { } screen)
            selectedMonitor = screen.DeviceName;
        UpdateOptions();
    }

    private void DrawArea_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        try
        {
            using var selector = new AreaSelectorForm();
            if (selector.ShowDialog() != Forms.DialogResult.OK) return;
            var selected = selector.SelectedArea;
            var screen = Forms.Screen.FromRectangle(selected);
            var inside = System.Drawing.Rectangle.Intersect(selected, screen.WorkingArea);
            if (inside.Width < 100 || inside.Height < 80)
            {
                System.Windows.MessageBox.Show("Selecione uma área dentro de um monitor.", "Firaw",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            selectedMonitor = screen.DeviceName;
            area = NormalizedArea.FromRectangle(inside, screen.WorkingArea);
            AreaRadio.IsChecked = true;
            UpdateOptions();
        }
        finally { Show(); Activate(); }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RuleNameBox.Text))
        {
            System.Windows.MessageBox.Show(this, "Informe um nome para a regra.", "Firaw",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (TitleRadio.IsChecked == true && string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            System.Windows.MessageBox.Show(this, "Informe um trecho do título da janela.", "Firaw",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (TypeRadio.IsChecked == true && string.IsNullOrEmpty(sample?.ClassName ?? Rule.ClassName))
        {
            System.Windows.MessageBox.Show(this, "Capture uma janela para reconhecer esse tipo.", "Firaw",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        Rule.Name = RuleNameBox.Text.Trim();
        Rule.Match = AllRadio.IsChecked == true ? MatchMode.All :
            TypeRadio.IsChecked == true ? MatchMode.Class : MatchMode.TitleContains;
        Rule.ClassName = sample?.ClassName ?? Rule.ClassName;
        Rule.TitleContains = TitleBox.Text.Trim();
        Rule.Placement = FullRadio.IsChecked == true ? PlacementMode.FullMonitor : PlacementMode.Area;
        Rule.Monitor = selectedMonitor;
        Rule.Area = area;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
