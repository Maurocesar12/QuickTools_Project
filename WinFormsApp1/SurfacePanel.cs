using System.Drawing.Drawing2D;

namespace WinFormsApp1;

/// <summary>A buffered surface for overview cards; child controls remain native and accessible.</summary>
internal sealed class SurfacePanel : Panel
{
    public bool Highlight { get; init; }

    public SurfacePanel() => DoubleBuffered = true;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        if (Width < 2 || Height < 2) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        const int diameter = 24;
        using var shape = new GraphicsPath();
        shape.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        shape.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        shape.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        shape.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        shape.CloseFigure();
        using var fill = new LinearGradientBrush(bounds, BackColor,
            Highlight ? Color.FromArgb(27, 65, 66) : Color.FromArgb(27, 37, 54), 15f);
        e.Graphics.FillPath(fill, shape);
        using var border = new Pen(Highlight ? Color.FromArgb(49, 105, 95) : Color.FromArgb(43, 57, 77));
        e.Graphics.DrawPath(border, shape);
    }
}
