using System.Windows;

namespace FirawynixWindowManager;

public partial class NamePromptWindow : Window
{
    public string Value => NameBox.Text.Trim();

    public NamePromptWindow(string current)
    {
        InitializeComponent();
        UiBranding.Apply(this);
        NameBox.Text = current;
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }

    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
