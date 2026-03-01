using System.Globalization;
using System.Drawing.Drawing2D;

namespace QuickHotkeyLauncher.UI;

internal static class UiTheme
{
    private static readonly bool IsZh = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    public static readonly Color Surface = Color.FromArgb(247, 249, 252);
    public static readonly Color Card = Color.White;
    public static readonly Color Border = Color.FromArgb(224, 229, 237);
    public static readonly Color Primary = Color.FromArgb(41, 98, 255);
    public static readonly Color PrimaryHover = Color.FromArgb(30, 84, 235);
    public static readonly Color Text = Color.FromArgb(43, 52, 69);
    public static readonly Color MutedText = Color.FromArgb(106, 116, 133);
    public static readonly Color Success = Color.FromArgb(39, 174, 96);
    public static readonly Color Warning = Color.FromArgb(243, 156, 18);
    public static readonly Color Danger = Color.FromArgb(231, 76, 60);
    public static readonly Color Neutral = Color.FromArgb(127, 140, 141);

    public static Font FontRegular(float size = 9.5f)
    {
        return new Font(IsZh ? "Microsoft YaHei UI" : "Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point);
    }

    public static Font FontSemibold(float size = 9.5f)
    {
        return new Font(IsZh ? "Microsoft YaHei UI" : "Segoe UI Semibold", size, FontStyle.Regular, GraphicsUnit.Point);
    }

    public static void StyleForm(Form form)
    {
        form.BackColor = Surface;
        form.Font = FontRegular();
        form.ForeColor = Text;
    }

    public static void StylePrimaryButton(Button button)
    {
        StyleButtonShape(button, 8);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(23, 72, 210);
        button.FlatAppearance.MouseOverBackColor = PrimaryHover;
        button.BackColor = Primary;
        button.ForeColor = Color.White;
        button.Font = FontSemibold(9.5f);
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(10, 3, 10, 3);
        button.MouseEnter += (_, _) => button.BackColor = PrimaryHover;
        button.MouseLeave += (_, _) => button.BackColor = Primary;
    }

    public static void StyleGhostButton(Button button)
    {
        StyleButtonShape(button, 8);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(236, 241, 251);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 248, 255);
        button.BackColor = Card;
        button.ForeColor = Text;
        button.Font = FontRegular(9f);
        button.Cursor = Cursors.Hand;
        button.Padding = new Padding(8, 2, 8, 2);
        button.MouseEnter += (_, _) => button.BackColor = Color.FromArgb(245, 248, 255);
        button.MouseLeave += (_, _) => button.BackColor = Card;
    }

    public static void StyleInput(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Color.White;
        textBox.ForeColor = Text;
        textBox.Font = FontRegular(9.5f);
    }

    public static void StyleOption(RadioButton radioButton)
    {
        radioButton.AutoSize = true;
        radioButton.ForeColor = Text;
        radioButton.Font = FontRegular(9.25f);
    }

    public static void StyleList(ListBox listBox)
    {
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.BackColor = Card;
        listBox.ForeColor = Text;
        listBox.Font = FontRegular(9.5f);
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = Card;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.GridColor = Border;
        grid.RowTemplate.Height = 38;
        grid.RowTemplate.DefaultCellStyle.Padding = new Padding(0, 6, 0, 6);
        grid.DefaultCellStyle.BackColor = Card;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 239, 255);
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Font = FontRegular(9.5f);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 246, 250);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = MutedText;
        grid.ColumnHeadersDefaultCellStyle.Font = FontSemibold(9f);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(244, 246, 250);
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = MutedText;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeight = 40;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
    }

    public static Color StatusColor(string status, string ok, string conflict, string invalid, string none)
    {
        if (status == ok) return Success;
        if (status == conflict) return Danger;
        if (status == invalid) return Warning;
        if (status == none) return Neutral;
        return MutedText;
    }

    private static void StyleButtonShape(Button button, int radius)
    {
        void ApplyRoundRegion()
        {
            if (button.Width <= 0 || button.Height <= 0)
            {
                return;
            }

            using var path = CreateRoundRectPath(new Rectangle(0, 0, button.Width, button.Height), radius);
            var oldRegion = button.Region;
            button.Region = new Region(path);
            oldRegion?.Dispose();
        }

        button.SizeChanged += (_, _) => ApplyRoundRegion();
        button.HandleCreated += (_, _) => ApplyRoundRegion();
        ApplyRoundRegion();
    }

    private static GraphicsPath CreateRoundRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(rect);
            path.CloseFigure();
            return path;
        }

        var diameter = radius * 2;
        var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);
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
