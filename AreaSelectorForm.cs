namespace FirawynixWindowManager;

internal sealed class AreaSelectorForm : Form
{
    private static readonly Color Cyan = Color.FromArgb(25, 211, 230);
    private Point? start;
    private Point current;
    public Rectangle SelectedArea { get; private set; }

    public AreaSelectorForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        Bounds = SystemInformation.VirtualScreen;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(12, 17, 23);
        Opacity = 0.82;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel; };
        MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            start = e.Location;
            current = e.Location;
            Invalidate();
        };
        MouseMove += (_, e) =>
        {
            if (!start.HasValue) return;
            current = e.Location;
            Invalidate();
        };
        MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left || !start.HasValue) return;
            current = e.Location;
            var local = Selection();
            if (local.Width < 100 || local.Height < 80)
            {
                start = null;
                Invalidate();
                return;
            }
            SelectedArea = new Rectangle(local.X + Bounds.X, local.Y + Bounds.Y,
                local.Width, local.Height);
            DialogResult = DialogResult.OK;
        };
    }

    private Rectangle Selection() => !start.HasValue ? Rectangle.Empty : Rectangle.FromLTRB(
        Math.Min(start.Value.X, current.X), Math.Min(start.Value.Y, current.Y),
        Math.Max(start.Value.X, current.X), Math.Max(start.Value.Y, current.Y));

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var titleFont = new Font("Segoe UI", 18, FontStyle.Bold);
        using var infoFont = new Font("Segoe UI", 10);
        using var numberFont = new Font("Segoe UI", 15, FontStyle.Bold);
        using var monitorPen = new Pen(Color.FromArgb(120, 155, 170, 181), 2);
        using var cyanBrush = new SolidBrush(Cyan);
        foreach (var screen in Screen.AllScreens)
        {
            var rect = screen.Bounds;
            rect.Offset(-Bounds.X, -Bounds.Y);
            rect.Inflate(-2, -2);
            e.Graphics.DrawRectangle(monitorPen, rect);
            e.Graphics.DrawString($"{MonitorNames.Name(screen)}  ·  {screen.Bounds.Width} × {screen.Bounds.Height}",
                numberFont, cyanBrush, rect.X + 18, rect.Y + 16);
        }

        var primary = Screen.PrimaryScreen?.Bounds ?? Screen.AllScreens[0].Bounds;
        var banner = new Rectangle(primary.X - Bounds.X + 24, primary.Y - Bounds.Y + 72, 440, 94);
        using var bannerBrush = new SolidBrush(Color.FromArgb(220, 21, 29, 38));
        e.Graphics.FillRectangle(bannerBrush, banner);
        e.Graphics.DrawString("Desenhe a área da janela", titleFont, Brushes.White,
            banner.X + 16, banner.Y + 13);
        e.Graphics.DrawString("Arraste para selecionar  ·  Esc para cancelar", infoFont,
            Brushes.White, banner.X + 18, banner.Y + 55);

        if (!start.HasValue) return;
        var selection = Selection();
        using var fill = new SolidBrush(Color.FromArgb(90, 25, 211, 230));
        using var outline = new Pen(Cyan, 3);
        e.Graphics.FillRectangle(fill, selection);
        e.Graphics.DrawRectangle(outline, selection);
        e.Graphics.DrawString($"{selection.Width} × {selection.Height}", infoFont,
            Brushes.White, selection.Right + 10, selection.Bottom + 6);
    }
}
