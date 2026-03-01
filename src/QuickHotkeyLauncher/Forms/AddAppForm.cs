using QuickHotkeyLauncher.Localization;
using QuickHotkeyLauncher.Models;
using QuickHotkeyLauncher.UI;

namespace QuickHotkeyLauncher.Forms;

public sealed class AddAppForm : Form
{
    private readonly RadioButton _installedRadio;
    private readonly RadioButton _customRadio;
    private readonly TextBox _searchTextBox;
    private readonly ListBox _appListBox;
    private readonly TextBox _appNameTextBox;
    private readonly TextBox _pathTextBox;
    private readonly Button _browseButton;
    private readonly Label _hotkeyLabel;
    private readonly List<AppCatalogItem> _allApps;
    private HotkeyDefinition? _hotkey;

    public AppBinding? Result { get; private set; }

    public AddAppForm(List<AppCatalogItem> apps)
    {
        _allApps = apps;
        Text = L.T("Add Application", "添加应用");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(760, 520);
        UiTheme.StyleForm(this);

        var header = new RoundedPanel
        {
            Left = 12,
            Top = 10,
            Width = 736,
            Height = 58,
            BackColor = UiTheme.Card,
            CornerRadius = 8,
            BorderColor = UiTheme.Border,
            BorderThickness = 1
        };
        var title = new Label
        {
            Left = 16,
            Top = 10,
            Width = 300,
            Height = 22,
            Text = L.T("Create Application Shortcut", "创建应用快捷方式"),
            Font = UiTheme.FontSemibold(10.5f),
            ForeColor = UiTheme.Text
        };
        var desc = new Label
        {
            Left = 16,
            Top = 32,
            Width = 420,
            Height = 18,
            Text = L.T("Pick from installed apps or choose an executable path.", "可从已安装应用中选择，或自定义 exe 路径。"),
            Font = UiTheme.FontRegular(9f),
            ForeColor = UiTheme.MutedText
        };
        header.Controls.Add(title);
        header.Controls.Add(desc);

        _installedRadio = new RadioButton
        {
            Text = L.T("Select from installed apps", "从已安装应用选择"),
            Left = 18,
            Top = 82,
            Checked = true
        };
        _customRadio = new RadioButton
        {
            Text = L.T("Choose custom executable", "自定义可执行文件"),
            Left = 220,
            Top = 82
        };
        UiTheme.StyleOption(_installedRadio);
        UiTheme.StyleOption(_customRadio);
        _installedRadio.CheckedChanged += (_, _) => RefreshMode();
        _customRadio.CheckedChanged += (_, _) => RefreshMode();

        _searchTextBox = new TextBox
        {
            Left = 18,
            Top = 108,
            Width = 730
        };
        UiTheme.StyleInput(_searchTextBox);
        _searchTextBox.PlaceholderText = L.T("Search installed applications...", "搜索已安装应用...");
        _searchTextBox.TextChanged += (_, _) => ReloadList();

        _appListBox = new ListBox
        {
            Left = 18,
            Top = 136,
            Width = 730,
            Height = 210
        };
        UiTheme.StyleList(_appListBox);

        var appNameLabel = new Label
        {
            Left = 18,
            Top = 358,
            Width = 80,
            Text = L.T("App Name", "应用名称"),
            ForeColor = UiTheme.MutedText
        };
        _appNameTextBox = new TextBox { Left = 98, Top = 354, Width = 650 };
        UiTheme.StyleInput(_appNameTextBox);

        var pathLabel = new Label
        {
            Left = 18,
            Top = 392,
            Width = 80,
            Text = L.T("Exe Path", "执行路径"),
            ForeColor = UiTheme.MutedText
        };
        _pathTextBox = new TextBox { Left = 98, Top = 388, Width = 560 };
        UiTheme.StyleInput(_pathTextBox);

        _browseButton = new Button
        {
            Left = 668,
            Top = 387,
            Width = 80,
            Height = 28,
            Text = L.T("Browse", "浏览")
        };
        UiTheme.StyleGhostButton(_browseButton);
        _browseButton.Click += (_, _) => BrowseExe();

        var hotkeyTitleLabel = new Label
        {
            Left = 18,
            Top = 428,
            Width = 80,
            Text = L.T("Hotkey", "快捷键"),
            ForeColor = UiTheme.MutedText
        };
        _hotkeyLabel = new Label
        {
            Left = 98,
            Top = 426,
            Width = 470,
            Height = 30,
            Text = L.T("Not set", "未设置"),
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = UiTheme.Card,
            ForeColor = UiTheme.Text
        };
        var setHotkeyButton = new Button
        {
            Left = 578,
            Top = 426,
            Width = 80,
            Height = 30,
            Text = L.T("Set", "设置")
        };
        UiTheme.StyleGhostButton(setHotkeyButton);
        setHotkeyButton.Click += (_, _) => CaptureHotkey();

        var okButton = new Button
        {
            Left = 578,
            Top = 474,
            Width = 80,
            Height = 32,
            Text = L.T("Add", "添加")
        };
        UiTheme.StylePrimaryButton(okButton);
        okButton.Click += (_, _) => Confirm();

        var cancelButton = new Button
        {
            Left = 668,
            Top = 474,
            Width = 80,
            Height = 32,
            Text = L.T("Cancel", "取消")
        };
        UiTheme.StyleGhostButton(cancelButton);
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _appListBox.SelectedIndexChanged += (_, _) =>
        {
            if (_appListBox.SelectedItem is AppCatalogItem item)
            {
                _appNameTextBox.Text = item.Name;
                _pathTextBox.Text = item.ExePath;
            }
        };

        Controls.AddRange(
        [
            header,
            _installedRadio, _customRadio, _searchTextBox, _appListBox,
            appNameLabel, _appNameTextBox, pathLabel, _pathTextBox, _browseButton,
            hotkeyTitleLabel, _hotkeyLabel, setHotkeyButton, okButton, cancelButton
        ]);

        ReloadList();
        RefreshMode();
    }

    private void ReloadList()
    {
        _appListBox.BeginUpdate();
        _appListBox.Items.Clear();

        var keyword = _searchTextBox.Text.Trim();
        IEnumerable<AppCatalogItem> items = _allApps;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            items = items.Where(x =>
                x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                x.ExePath.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in items)
        {
            _appListBox.Items.Add(item);
        }

        _appListBox.EndUpdate();
    }

    private void RefreshMode()
    {
        var installedMode = _installedRadio.Checked;
        _searchTextBox.Enabled = installedMode;
        _appListBox.Enabled = installedMode;
        _browseButton.Enabled = !installedMode;
    }

    private void BrowseExe()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Executable Files (*.exe)|*.exe",
            CheckFileExists = true,
            Title = L.T("Choose executable file", "选择可执行文件")
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _pathTextBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(_appNameTextBox.Text))
            {
                _appNameTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private void CaptureHotkey()
    {
        using var capture = new HotkeyCaptureForm(_hotkey);
        if (capture.ShowDialog(this) == DialogResult.OK && capture.Result is not null)
        {
            _hotkey = capture.Result;
            _hotkeyLabel.Text = _hotkey.ToString();
        }
    }

    private void Confirm()
    {
        var appName = _appNameTextBox.Text.Trim();
        var path = _pathTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(appName))
        {
            AppDialog.Warn(this, L.T("Please provide an application name.", "请输入应用名称。"));
            return;
        }

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            AppDialog.Warn(this, L.T("Please choose a valid executable path.", "请选择有效的可执行文件路径。"));
            return;
        }

        if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            AppDialog.Warn(this, L.T("Only .exe files are supported.", "仅支持 .exe 文件。"));
            return;
        }

        if (_hotkey is null)
        {
            AppDialog.Warn(this, L.T("Please set a hotkey first.", "请先设置快捷键。"));
            return;
        }

        Result = new AppBinding
        {
            AppName = appName,
            ExePath = path,
            Hotkey = _hotkey,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
