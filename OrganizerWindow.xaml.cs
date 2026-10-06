using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace FirawynixWindowManager;

public partial class OrganizerWindow : Window
{
    private sealed record ProfileEntry(AppProfile Profile, string Name, string Subtitle);
    private sealed record RuleEntry(WindowRule Rule, string Name, string Match,
        string Destination, string State, string ToggleLabel);

    private readonly List<AppProfile> profiles = ProfileStore.Load();
    private readonly WindowPlacer placer = new();
    private readonly Dictionary<nint, (DateTime FirstSeen, int Applies)> seen = [];
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly Forms.NotifyIcon tray;
    private bool quitting;
    private bool active = true;

    private AppProfile? SelectedProfile => (ProgramsList.SelectedItem as ProfileEntry)?.Profile;

    public OrganizerWindow()
    {
        InitializeComponent();
        UiBranding.Apply(this);
        RefreshPrograms();
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir organizador", null, (_, _) => Dispatcher.Invoke(ShowWindow));
        menu.Items.Add("Sair", null, (_, _) => Dispatcher.Invoke(Exit));
        tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ??
                System.Drawing.SystemIcons.Application,
            Text = "Firaw Organizador de Janelas",
            Visible = true,
            ContextMenuStrip = menu
        };
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWindow);
        timer.Tick += (_, _) => Scan();
        timer.Start();
        Closing += OnClosing;
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Exit()
    {
        quitting = true;
        Close();
    }

    internal void CloseForPreview() => Exit();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!quitting)
        {
            e.Cancel = true;
            Hide();
            tray.ShowBalloonTip(2000, "Firaw",
                "As regras continuam ativas na área de notificação.", Forms.ToolTipIcon.Info);
            return;
        }
        timer.Stop();
        placer.RestoreAll();
        tray.Visible = false;
        tray.Dispose();
    }

    private void RefreshPrograms(AppProfile? select = null)
    {
        select ??= SelectedProfile;
        var entries = profiles.OrderBy(p => p.Name).Select(p => new ProfileEntry(p,
            p.Name, $"{p.Rules.Count} {(p.Rules.Count == 1 ? "regra" : "regras")} · {Path.GetFileName(p.ExecutablePath)}"))
            .ToList();
        ProgramsList.ItemsSource = entries;
        ProgramsList.SelectedItem = entries.FirstOrDefault(e => ReferenceEquals(e.Profile, select)) ??
            entries.FirstOrDefault();
        RefreshRules();
    }

    private void RefreshRules()
    {
        var profile = SelectedProfile;
        ProfileNameText.Text = profile?.Name ?? "Escolha um programa";
        ProfilePathText.Text = profile?.ExecutablePath ??
            "Capture uma janela ou selecione um arquivo para começar.";
        NewRuleButton.IsEnabled = profile is not null;
        RemoveProgramButton.IsEnabled = profile is not null;
        var entries = profile?.Rules.Select(rule => new RuleEntry(rule, rule.Name,
            rule.MatchDescription,
            $"{(rule.Placement == PlacementMode.FullMonitor ? "Tela cheia" : "Área selecionada")} · {MonitorNames.Description(rule.Monitor)}",
            rule.Enabled ? "● Ativa" : "○ Pausada",
            rule.Enabled ? "Pausar" : "Ativar")).ToList() ?? [];
        RulesList.ItemsSource = entries;
        EmptyRules.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Programs_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshRules();

    private void CaptureProgram_Click(object sender, RoutedEventArgs e)
    {
        var picker = new WindowPickerWindow { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedWindow is null) return;
        var window = picker.SelectedWindow;
        var profile = profiles.FirstOrDefault(p => p.ContainsWindow(window));
        if (profile is null)
        {
            profile = new AppProfile
            {
                Name = Path.GetFileNameWithoutExtension(window.ExecutablePath),
                ExecutablePath = window.ExecutablePath
            };
            profiles.Add(profile);
            SaveProfiles();
        }
        RefreshPrograms(profile);
        if (profile.Rules.Count == 0) CreateRule(profile, window);
    }

    private void BrowseProgram_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFileDialog { Filter = "Programas (*.exe)|*.exe", Title = "Escolher programa" };
        if (picker.ShowDialog(this) != true) return;
        var profile = profiles.FirstOrDefault(p => p.ContainsExecutable(picker.FileName));
        if (profile is null)
        {
            profile = new AppProfile
            {
                Name = Path.GetFileNameWithoutExtension(picker.FileName),
                ExecutablePath = picker.FileName
            };
            profiles.Add(profile);
            SaveProfiles();
        }
        RefreshPrograms(profile);
        if (profile.Rules.Count == 0) CreateRule(profile, null);
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        var profile = SelectedProfile;
        if (profile is null) return;
        WindowInfo? sample = null;
        var picker = new WindowPickerWindow { Owner = this };
        if (picker.ShowDialog() == true) sample = picker.SelectedWindow;
        if (sample is not null && !profile.ContainsExecutable(sample.ExecutablePath))
        {
            var answer = System.Windows.MessageBox.Show(this,
                $"Esta janela pertence a {Path.GetFileName(sample.ExecutablePath)}. Associar ao programa {profile.Name}?",
                "Associar janela", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;
        }
        CreateRule(profile, sample);
    }

    private void CreateRule(AppProfile profile, WindowInfo? sample)
    {
        var editor = new RuleEditorWindow(sample, null, profile.Rules.Count == 0) { Owner = this };
        if (editor.ShowDialog() != true) return;
        if (sample is not null && !profile.ContainsExecutable(sample.ExecutablePath))
            profile.AssociatedExecutables.Add(sample.ExecutablePath);
        profile.Rules.Insert(0, editor.Rule);
        SaveProfiles();
        RefreshPrograms(profile);
        Reapply();
    }

    private void EditRule_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not RuleEntry entry || SelectedProfile is not { } profile) return;
        var editor = new RuleEditorWindow(null, entry.Rule, false) { Owner = this };
        if (editor.ShowDialog() != true) return;
        var index = profile.Rules.FindIndex(rule => rule.Id == entry.Rule.Id);
        if (index < 0) return;
        profile.Rules[index] = editor.Rule;
        SaveProfiles();
        RefreshPrograms(profile);
        Reapply();
    }

    private void ToggleRule_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not RuleEntry entry) return;
        entry.Rule.Enabled = !entry.Rule.Enabled;
        SaveProfiles();
        RefreshPrograms(SelectedProfile);
        Reapply();
    }

    private void RemoveRule_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not RuleEntry entry || SelectedProfile is not { } profile) return;
        if (System.Windows.MessageBox.Show(this, $"Excluir a regra '{entry.Name}'?", "Firaw",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        profile.Rules.Remove(entry.Rule);
        SaveProfiles();
        RefreshPrograms(profile);
        Reapply();
    }

    private void RemoveProgram_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProfile is not { } profile) return;
        if (System.Windows.MessageBox.Show(this, $"Remover todas as regras de {profile.Name}?", "Firaw",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        profiles.Remove(profile);
        SaveProfiles();
        RefreshPrograms();
        Reapply();
    }

    private void Monitors_Click(object sender, RoutedEventArgs e)
    {
        var picker = new MonitorPickerWindow { Owner = this };
        picker.ShowDialog();
        RefreshRules();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        active = !active;
        ActivityText.Text = active ? "●  Ativo" : "○  Pausado";
        PauseButton.Content = active ? "Pausar" : "Retomar";
        StatusText.Text = active ? "Pronto para organizar janelas" : "Monitoramento pausado";
        if (active) seen.Clear();
    }

    private void SaveProfiles()
    {
        try { ProfileStore.Save(profiles); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"Não foi possível salvar as regras:\n{ex.Message}",
                "Firaw", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Reapply()
    {
        placer.RestoreAll();
        seen.Clear();
    }

    private void Scan()
    {
        if (!active) return;
        try
        {
            var now = DateTime.UtcNow;
            var windows = WindowCatalog.Enumerate();
            var handles = windows.Select(w => w.Handle).ToHashSet();
            foreach (var handle in seen.Keys.Where(h => !handles.Contains(h)).ToArray()) seen.Remove(handle);
            placer.Prune();
            foreach (var window in windows)
            {
                if (!seen.TryGetValue(window.Handle, out var state))
                {
                    seen[window.Handle] = (now, 0);
                    continue;
                }
                var elapsed = (now - state.FirstSeen).TotalMilliseconds;
                if (state.Applies >= 2 || elapsed < (state.Applies == 0 ? 450 : 1500)) continue;
                var profile = profiles.FirstOrDefault(p => p.ContainsWindow(window));
                var rule = profile?.Rules.Where(r => r.Matches(window))
                    .OrderBy(r => r.Match == MatchMode.All ? 1 : 0).FirstOrDefault();
                if (rule is null) { seen[window.Handle] = (state.FirstSeen, 2); continue; }
                var applied = placer.Apply(window, rule);
                seen[window.Handle] = (state.FirstSeen, state.Applies + 1);
                if (!applied) StatusText.Text = $"Não foi possível mover {profile!.Name}: {rule.Name}";
            }
        }
        catch (Exception ex) { StatusText.Text = $"Falha ao verificar janelas: {ex.Message}"; }
    }
}
