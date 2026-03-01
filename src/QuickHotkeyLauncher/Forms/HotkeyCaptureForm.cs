using QuickHotkeyLauncher.Localization;
using QuickHotkeyLauncher.Models;
using QuickHotkeyLauncher.UI;

namespace QuickHotkeyLauncher.Forms;

public sealed class HotkeyCaptureForm : Form
{
    private readonly Label _previewLabel;
    private readonly Button _confirmButton;
    private HotkeyDefinition? _captured;

    public HotkeyDefinition? Result => _captured;

    public HotkeyCaptureForm(HotkeyDefinition? initial = null)
    {
        Text = L.T("Set Hotkey", "设置快捷键");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(420, 188);
        KeyPreview = true;
        UiTheme.StyleForm(this);

        var panel = new RoundedPanel
        {
            Left = 12,
            Top = 12,
            Width = 396,
            Height = 164,
            BackColor = UiTheme.Card,
            CornerRadius = 8,
            BorderColor = UiTheme.Border,
            BorderThickness = 1
        };

        var instruction = new Label
        {
            Text = L.T("Press any key combination (for example Ctrl + Alt + V)", "请直接按下组合键（例如 Ctrl + Alt + V）"),
            AutoSize = false,
            Width = 360,
            Height = 38,
            Top = 16,
            Left = 16,
            ForeColor = UiTheme.MutedText
        };

        _previewLabel = new Label
        {
            Text = initial?.ToString() ?? L.T("Not set", "未设置"),
            AutoSize = false,
            Width = 360,
            Height = 30,
            Top = 62,
            Left = 16,
            Font = UiTheme.FontSemibold(10f),
            ForeColor = UiTheme.Text
        };

        _confirmButton = new Button
        {
            Text = L.T("OK", "确定"),
            Width = 80,
            Height = 30,
            Top = 114,
            Left = 208,
            Enabled = initial is not null
        };
        UiTheme.StylePrimaryButton(_confirmButton);
        _confirmButton.Enabled = initial is not null;
        _confirmButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Text = L.T("Cancel", "取消"),
            Width = 80,
            Height = 30,
            Top = 114,
            Left = 296
        };
        UiTheme.StyleGhostButton(cancelButton);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        panel.Controls.Add(instruction);
        panel.Controls.Add(_previewLabel);
        panel.Controls.Add(_confirmButton);
        panel.Controls.Add(cancelButton);
        Controls.Add(panel);

        if (initial is not null)
        {
            _captured = initial;
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        var keyCode = keyData & Keys.KeyCode;
        if (IsModifierOnly(keyCode))
        {
            return true;
        }

        _captured = new HotkeyDefinition
        {
            Ctrl = keyData.HasFlag(Keys.Control),
            Alt = keyData.HasFlag(Keys.Alt),
            Shift = keyData.HasFlag(Keys.Shift),
            Win = keyData.HasFlag(Keys.LWin) || keyData.HasFlag(Keys.RWin),
            Key = keyCode
        };

        _previewLabel.Text = _captured.ToString();
        _confirmButton.Enabled = true;
        return true;
    }

    private static bool IsModifierOnly(Keys key)
    {
        return key is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin;
    }
}
