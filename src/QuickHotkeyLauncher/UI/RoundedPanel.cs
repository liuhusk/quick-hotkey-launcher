using System.Drawing.Drawing2D;

namespace QuickHotkeyLauncher.UI;

public sealed class RoundedPanel : Panel
{
    private int _cornerRadius = 8;
    private int _borderThickness = 1;
    private Color _borderColor = UiTheme.Border;

    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Math.Max(0, value);
            Invalidate();
            UpdateRegion();
        }
    }

    public int BorderThickness
    {
        get => _borderThickness;
        set
        {
            _borderThickness = Math.Max(0, value);
            Invalidate();
            UpdateRegion();
        }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    public RoundedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = UiTheme.Card;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var borderInset = _borderThickness > 0 ? _borderThickness / 2f : 0f;
        var rect = new RectangleF(
            borderInset,
            borderInset,
            Math.Max(1, Width - _borderThickness),
            Math.Max(1, Height - _borderThickness));

        using var path = CreateRoundRectPath(rect, Math.Max(0, _cornerRadius - borderInset));
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);

        if (_borderThickness > 0)
        {
            using var pen = new Pen(_borderColor, _borderThickness);
            e.Graphics.DrawPath(pen, path);
        }
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = CreateRoundRectPath(new RectangleF(0, 0, Width, Height), _cornerRadius);
        Region?.Dispose();
        Region = new Region(path);
    }

    private static GraphicsPath CreateRoundRectPath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            path.CloseFigure();
            return path;
        }

        var diameter = radius * 2f;
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
