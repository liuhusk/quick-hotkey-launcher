using QuickHotkeyLauncher.Localization;

namespace QuickHotkeyLauncher.UI;

internal static class AppDialog
{
    public static DialogResult Confirm(IWin32Window owner, string message)
    {
        return ShowCustomDialog(
            owner,
            message,
            L.T("Confirm", "确认"),
            isWarning: false,
            hasCancel: true,
            okText: L.T("Yes", "是"),
            cancelText: L.T("No", "否"),
            defaultResult: DialogResult.No);
    }

    public static void Warn(IWin32Window owner, string message)
    {
        ShowCustomDialog(
            owner,
            message,
            L.T("Validation", "提示"),
            isWarning: true,
            hasCancel: false,
            okText: L.T("OK", "确定"),
            cancelText: string.Empty,
            defaultResult: DialogResult.OK);
    }

    private static DialogResult ShowCustomDialog(
        IWin32Window owner,
        string message,
        string title,
        bool isWarning,
        bool hasCancel,
        string okText,
        string cancelText,
        DialogResult defaultResult)
    {
        using var form = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(420, 186)
        };
        UiTheme.StyleForm(form);

        var card = new RoundedPanel
        {
            Left = 12,
            Top = 12,
            Width = 396,
            Height = 162,
            BackColor = UiTheme.Card,
            CornerRadius = 8,
            BorderColor = UiTheme.Border,
            BorderThickness = 1
        };

        var titleLabel = new Label
        {
            Left = 16,
            Top = 14,
            Width = 360,
            Height = 24,
            Text = title,
            ForeColor = isWarning ? UiTheme.Warning : UiTheme.Text,
            Font = UiTheme.FontSemibold(10f)
        };

        var messageLabel = new Label
        {
            Left = 16,
            Top = 44,
            Width = 360,
            Height = 52,
            Text = message,
            ForeColor = UiTheme.Text,
            Font = UiTheme.FontRegular(9.5f)
        };

        var okButton = new Button
        {
            Text = okText,
            Width = 86,
            Height = 32,
            Top = 112,
            Left = hasCancel ? 196 : 290,
            DialogResult = hasCancel ? DialogResult.Yes : DialogResult.OK
        };
        UiTheme.StylePrimaryButton(okButton);

        card.Controls.Add(titleLabel);
        card.Controls.Add(messageLabel);
        card.Controls.Add(okButton);

        if (hasCancel)
        {
            var cancelButton = new Button
            {
                Text = cancelText,
                Width = 86,
                Height = 32,
                Top = 112,
                Left = 290,
                DialogResult = DialogResult.No
            };
            UiTheme.StyleGhostButton(cancelButton);
            card.Controls.Add(cancelButton);
            form.CancelButton = cancelButton;
        }

        form.AcceptButton = okButton;
        form.Controls.Add(card);

        form.Shown += (_, _) =>
        {
            if (defaultResult == DialogResult.No && hasCancel)
            {
                form.ActiveControl = card.Controls.OfType<Button>().FirstOrDefault(x => x.DialogResult == DialogResult.No);
            }
            else
            {
                form.ActiveControl = okButton;
            }
        };

        return form.ShowDialog(owner);
    }
}
