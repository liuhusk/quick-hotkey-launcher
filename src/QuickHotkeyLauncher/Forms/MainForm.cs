using QuickHotkeyLauncher.Localization;
using QuickHotkeyLauncher.Models;
using QuickHotkeyLauncher.Services;
using QuickHotkeyLauncher.UI;
using System.Drawing.Drawing2D;

namespace QuickHotkeyLauncher.Forms;

public sealed class MainForm : Form
{
    private const int ActionButtonWidth = 74;
    private const int ActionButtonHeight = 26;

    private readonly ConfigService _configService = new();
    private readonly InstalledAppCatalogService _catalogService = new();
    private readonly LaunchFocusService _launchFocusService = new();
    private readonly StartupService _startupService = new();
    private readonly List<AppBinding> _bindings = new();
    private readonly Dictionary<Guid, string> _registerErrors = new();
    private readonly Dictionary<Guid, Image> _iconCache = new();

    private HotkeyService? _hotkeyService;
    private AppConfig _config;
    private int _hoveredRowIndex = -1;

    private readonly RoundedPanel _topPanel;
    private readonly RoundedPanel _gridPanel;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly DataGridView _grid;
    private readonly Label _statusLabel;
    private readonly Button _addButton;

    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly ToolStripMenuItem _showWindowMenuItem;
    private readonly ToolStripMenuItem _startupMenuItem;
    private readonly ToolStripMenuItem _languageMenuItem;
    private readonly ToolStripMenuItem _langSystemMenuItem;
    private readonly ToolStripMenuItem _langChineseMenuItem;
    private readonly ToolStripMenuItem _langEnglishMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;

    private bool _exitRequested;

    public MainForm()
    {
        _config = _configService.Load();
        L.SetMode(ParseLanguageMode(_config.LanguageMode));

        Text = "QuickHotkeyLauncher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 560);
        ClientSize = new Size(980, 620);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        UiTheme.StyleForm(this);

        _topPanel = new RoundedPanel
        {
            Left = 12,
            Top = 12,
            Width = 940,
            Height = 68,
            BackColor = UiTheme.Card,
            CornerRadius = 8,
            BorderColor = UiTheme.Border,
            BorderThickness = 1,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _gridPanel = new RoundedPanel
        {
            Left = 12,
            Top = 92,
            Width = 940,
            Height = 470,
            BackColor = UiTheme.Card,
            CornerRadius = 8,
            BorderColor = UiTheme.Border,
            BorderThickness = 1,
            Padding = new Padding(8),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        _titleLabel = new Label
        {
            Left = 16,
            Top = 12,
            Width = 460,
            Height = 24,
            Font = UiTheme.FontSemibold(11f),
            ForeColor = UiTheme.Text
        };

        _subtitleLabel = new Label
        {
            Left = 16,
            Top = 36,
            Width = 520,
            Height = 20,
            Font = UiTheme.FontRegular(9f),
            ForeColor = UiTheme.MutedText
        };

        _addButton = new Button
        {
            Left = 808,
            Top = 17,
            Width = 116,
            Height = 34,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        UiTheme.StylePrimaryButton(_addButton);
        _addButton.Click += (_, _) => AddBinding();

        _topPanel.Controls.Add(_titleLabel);
        _topPanel.Controls.Add(_subtitleLabel);
        _topPanel.Controls.Add(_addButton);

        _grid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            Dock = DockStyle.Fill
        };
        UiTheme.StyleGrid(_grid);
        _grid.BorderStyle = BorderStyle.None;
        ConfigureGrid();
        _grid.CellContentClick += OnGridCellContentClick;
        _grid.CellFormatting += OnGridCellFormatting;
        _grid.CellPainting += OnGridCellPainting;
        _grid.ColumnWidthChanged += (_, _) => _grid.Invalidate();
        _grid.Scroll += (_, _) => _grid.Invalidate();
        _grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                _hoveredRowIndex = e.RowIndex;
                _grid.InvalidateRow(e.RowIndex);
            }
        };
        _grid.CellMouseLeave += (_, e) =>
        {
            if (e.RowIndex >= 0 && _hoveredRowIndex == e.RowIndex)
            {
                _hoveredRowIndex = -1;
                _grid.InvalidateRow(e.RowIndex);
            }
        };

        _statusLabel = new Label
        {
            Left = 14,
            Top = 568,
            Width = 938,
            Height = 24,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = UiTheme.MutedText,
            Font = UiTheme.FontRegular(9f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _gridPanel.Controls.Add(_grid);
        Controls.Add(_topPanel);
        Controls.Add(_gridPanel);
        Controls.Add(_statusLabel);

        _trayMenu = new ContextMenuStrip
        {
            Font = UiTheme.FontRegular(9.5f),
            BackColor = UiTheme.Card
        };

        _showWindowMenuItem = new ToolStripMenuItem();
        _showWindowMenuItem.Click += (_, _) => ShowFromTray();

        _startupMenuItem = new ToolStripMenuItem
        {
            CheckOnClick = true,
            Checked = _startupService.IsEnabled()
        };
        _startupMenuItem.CheckedChanged += OnStartupMenuCheckedChanged;

        _languageMenuItem = new ToolStripMenuItem();
        _langSystemMenuItem = new ToolStripMenuItem();
        _langChineseMenuItem = new ToolStripMenuItem();
        _langEnglishMenuItem = new ToolStripMenuItem();

        _langSystemMenuItem.Click += (_, _) => ChangeLanguage(LanguageMode.System);
        _langChineseMenuItem.Click += (_, _) => ChangeLanguage(LanguageMode.Chinese);
        _langEnglishMenuItem.Click += (_, _) => ChangeLanguage(LanguageMode.English);

        _languageMenuItem.DropDownItems.Add(_langSystemMenuItem);
        _languageMenuItem.DropDownItems.Add(_langChineseMenuItem);
        _languageMenuItem.DropDownItems.Add(_langEnglishMenuItem);

        _exitMenuItem = new ToolStripMenuItem();
        _exitMenuItem.Click += (_, _) => ExitFromTray();

        _trayMenu.Items.Add(_showWindowMenuItem);
        _trayMenu.Items.Add(_startupMenuItem);
        _trayMenu.Items.Add(_languageMenuItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(_exitMenuItem);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "QuickHotkeyLauncher",
            Visible = true,
            ContextMenuStrip = _trayMenu
        };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();

        ApplyLocalizedTexts();
        UpdateLanguageMenuChecks();
        Layout += (_, _) => ReflowLayout();

        Load += (_, _) => InitializeData();
        FormClosing += OnMainFormClosing;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _hotkeyService = new HotkeyService(Handle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey && _hotkeyService is not null)
        {
            var hotkeyId = m.WParam.ToInt32();
            if (_hotkeyService.TryGetBindingId(hotkeyId, out var bindingId))
            {
                var binding = _bindings.FirstOrDefault(x => x.Id == bindingId);
                if (binding is not null && File.Exists(binding.ExePath))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            _launchFocusService.LaunchOrFocus(binding.ExePath, binding.AppName, binding.LaunchArguments);
                        }
                        catch
                        {
                        }
                    });
                }
            }
        }

        base.WndProc(ref m);
    }

    private void InitializeData()
    {
        _bindings.Clear();
        _bindings.AddRange(_config.Bindings);
        RegisterAllHotkeys();
        RefreshGrid();
    }

    private void ConfigureGrid()
    {
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "AppIcon",
            HeaderText = "",
            DataPropertyName = "AppIcon",
            Width = 34,
            ImageLayout = DataGridViewImageCellLayout.Zoom
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "AppName",
            DataPropertyName = "AppName",
            Width = 280
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Hotkey",
            DataPropertyName = "Hotkey",
            Width = 160
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            DataPropertyName = "Status",
            Width = 150
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ResetHotkey",
            DataPropertyName = "ResetText",
            Width = 95
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ClearHotkey",
            HeaderText = " ",
            DataPropertyName = "ClearText",
            Width = 95
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Remove",
            HeaderText = " ",
            DataPropertyName = "RemoveText",
            Width = 95
        });

        foreach (DataGridViewColumn column in _grid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        ((DataGridViewTextBoxColumn)_grid.Columns["ResetHotkey"]).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        ((DataGridViewTextBoxColumn)_grid.Columns["ClearHotkey"]).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        var resetColumn = (DataGridViewTextBoxColumn)_grid.Columns["ResetHotkey"];
        var clearColumn = (DataGridViewTextBoxColumn)_grid.Columns["ClearHotkey"];
        var removeColumn = (DataGridViewTextBoxColumn)_grid.Columns["Remove"];
        removeColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        resetColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        clearColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        removeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        resetColumn.FillWeight = 1;
        clearColumn.FillWeight = 1;
        removeColumn.FillWeight = 1;
        resetColumn.MinimumWidth = 95;
        clearColumn.MinimumWidth = 95;
        removeColumn.MinimumWidth = 95;
    }

    private void ReflowLayout()
    {
        const int margin = 12;
        const int sectionGap = 12;
        const int statusHeight = 24;
        const int bottomPadding = 8;

        _topPanel.SetBounds(margin, margin, Math.Max(200, ClientSize.Width - margin * 2), 68);

        var gridTop = _topPanel.Bottom + sectionGap;
        var statusTop = ClientSize.Height - statusHeight - bottomPadding;
        var gridHeight = Math.Max(120, statusTop - gridTop - 6);

        _gridPanel.SetBounds(margin, gridTop, Math.Max(200, ClientSize.Width - margin * 2), gridHeight);
        _statusLabel.SetBounds(14, statusTop, Math.Max(160, ClientSize.Width - 28), statusHeight);
    }

    private void ApplyLocalizedTexts()
    {
        _titleLabel.Text = L.T("Application Hotkey List", "应用快捷键列表");
        _subtitleLabel.Text = L.T("Launch, focus, and manage app hotkeys", "管理应用启动、置前与快捷键");
        _addButton.Text = L.T("Add App", "添加应用");

        _statusLabel.Text = L.T("Ready", "就绪");

        _grid.Columns["AppName"].HeaderText = L.T("Application", "应用");
        _grid.Columns["Hotkey"].HeaderText = L.T("Hotkey", "快捷键");
        _grid.Columns["Status"].HeaderText = L.T("Status", "状态");
        _grid.Columns["ResetHotkey"].HeaderText = L.T("Actions", "操作");

        _showWindowMenuItem.Text = L.T("Show Window", "显示窗体");
        _startupMenuItem.Text = L.T("Run at startup", "开机启动");
        _languageMenuItem.Text = L.T("Language", "语言");
        _langSystemMenuItem.Text = L.T("Follow System", "跟随系统");
        _langChineseMenuItem.Text = L.T("Chinese", "中文");
        _langEnglishMenuItem.Text = L.T("English", "English");
        _exitMenuItem.Text = L.T("Exit", "退出");

        RefreshGrid();
        _grid.Invalidate();
    }

    private void UpdateLanguageMenuChecks()
    {
        _langSystemMenuItem.Checked = L.Mode == LanguageMode.System;
        _langChineseMenuItem.Checked = L.Mode == LanguageMode.Chinese;
        _langEnglishMenuItem.Checked = L.Mode == LanguageMode.English;
    }

    private void ChangeLanguage(LanguageMode mode)
    {
        L.SetMode(mode);
        _config.LanguageMode = ToConfigLanguage(mode);
        _configService.Save(_config);

        UpdateLanguageMenuChecks();
        ApplyLocalizedTexts();
    }

    private static LanguageMode ParseLanguageMode(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "zh" or "chinese" => LanguageMode.Chinese,
            "en" or "english" => LanguageMode.English,
            _ => LanguageMode.System
        };
    }

    private static string ToConfigLanguage(LanguageMode mode)
    {
        return mode switch
        {
            LanguageMode.Chinese => "zh",
            LanguageMode.English => "en",
            _ => "system"
        };
    }

    private void OnGridCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics is null)
        {
            return;
        }

        if (e.RowIndex != -1)
        {
            var colName = _grid.Columns[e.ColumnIndex].Name;
            if (colName is "ResetHotkey" or "ClearHotkey" or "Remove")
            {
                e.Handled = true;
                e.PaintBackground(e.ClipBounds, true);

                var actionRect = new Rectangle(
                    e.CellBounds.Left + Math.Max(8, (e.CellBounds.Width - ActionButtonWidth) / 2),
                    e.CellBounds.Top + Math.Max(6, (e.CellBounds.Height - ActionButtonHeight) / 2),
                    ActionButtonWidth,
                    ActionButtonHeight);
                var isHovered = e.RowIndex == _hoveredRowIndex;
                DrawActionButton(e.Graphics, actionRect, Convert.ToString(e.FormattedValue), colName, isHovered);
                e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
                return;
            }

            return;
        }

        var resetIndex = _grid.Columns["ResetHotkey"].Index;
        var clearIndex = _grid.Columns["ClearHotkey"].Index;
        var removeIndex = _grid.Columns["Remove"].Index;

        if (e.ColumnIndex == clearIndex || e.ColumnIndex == removeIndex)
        {
            e.Handled = true;
            return;
        }

        if (e.ColumnIndex != resetIndex)
        {
            return;
        }

        var resetRect = _grid.GetCellDisplayRectangle(resetIndex, -1, true);
        var removeRect = _grid.GetCellDisplayRectangle(removeIndex, -1, true);
        var mergedRect = Rectangle.FromLTRB(resetRect.Left, resetRect.Top, removeRect.Right, resetRect.Bottom);

        using var bgBrush = new SolidBrush(Color.FromArgb(244, 246, 250));
        using var borderPen = new Pen(UiTheme.Border);
        using var textBrush2 = new SolidBrush(UiTheme.MutedText);
        var format2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        e.Graphics.FillRectangle(bgBrush, mergedRect);
        e.Graphics.DrawRectangle(borderPen, mergedRect);
        e.Graphics.DrawString(L.T("Actions", "操作"), UiTheme.FontSemibold(9f), textBrush2, mergedRect, format2);

        e.Handled = true;
    }

    private static void DrawActionButton(Graphics g, Rectangle rect, string? text, string actionName, bool hovered)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var (fill, fillHover) = actionName switch
        {
            "ResetHotkey" => (Color.FromArgb(54, 107, 248), Color.FromArgb(42, 95, 238)),
            "ClearHotkey" => (Color.FromArgb(106, 116, 133), Color.FromArgb(94, 104, 122)),
            "Remove" => (Color.FromArgb(220, 75, 62), Color.FromArgb(204, 63, 51)),
            _ => (UiTheme.Primary, UiTheme.PrimaryHover)
        };

        var background = hovered ? fillHover : fill;
        using var path = CreateRoundRectPath(rect, 6);
        using var brush = new SolidBrush(background);
        using var textBrush = new SolidBrush(Color.White);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        g.FillPath(brush, path);
        g.DrawString(text ?? string.Empty, UiTheme.FontSemibold(8.75f), textBrush, rect, format);
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

    private void OnGridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        var row = _grid.Rows[e.RowIndex];
        if (!row.Selected)
        {
            row.DefaultCellStyle.BackColor = e.RowIndex == _hoveredRowIndex ? Color.FromArgb(250, 252, 255) : UiTheme.Card;
        }

        if (_grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is string status)
        {
            var color = UiTheme.StatusColor(
                status,
                L.T("OK", "正常"),
                L.T("Conflict", "冲突"),
                L.T("Invalid Path", "路径失效"),
                L.T("No Hotkey", "未设置快捷键"));
            row.Cells[e.ColumnIndex].Style.ForeColor = color;
            row.Cells[e.ColumnIndex].Style.Font = UiTheme.FontSemibold(9.25f);
        }
    }

    private void AddBinding()
    {
        List<AppCatalogItem> apps;
        try
        {
            apps = _catalogService.GetInstalledApps();
        }
        catch
        {
            apps = new List<AppCatalogItem>();
        }

        using var form = new AddAppForm(apps);
        if (form.ShowDialog(this) != DialogResult.OK || form.Result is null)
        {
            return;
        }

        if (_bindings.Any(x => string.Equals(x.ExePath, form.Result.ExePath, StringComparison.OrdinalIgnoreCase)))
        {
            AppDialog.Warn(this, L.T("This application already exists.", "该应用已存在。"));
            return;
        }

        var newItem = form.Result;
        if (!TryRegisterBinding(newItem, out var error))
        {
            AppDialog.Warn(this, error);
            return;
        }

        _bindings.Add(newItem);
        SaveConfig();
        RefreshGrid();
    }

    private void OnGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
        {
            return;
        }

        if (_grid.Rows[e.RowIndex].DataBoundItem is not MainGridRow row)
        {
            return;
        }

        var item = _bindings.FirstOrDefault(x => x.Id == row.BindingId);
        if (item is null)
        {
            return;
        }

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (columnName == "Remove")
        {
            RemoveBinding(item);
            return;
        }

        if (columnName == "ResetHotkey")
        {
            ResetHotkey(item);
            return;
        }

        if (columnName == "ClearHotkey")
        {
            ClearHotkey(item);
        }
    }

    private void RemoveBinding(AppBinding item)
    {
        var confirm = AppDialog.Confirm(this, L.T($"Remove {item.AppName}?", $"确认移除 {item.AppName}？"));
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _hotkeyService?.Unregister(item.Id);
        _registerErrors.Remove(item.Id);
        _bindings.Remove(item);
        if (_iconCache.TryGetValue(item.Id, out var icon))
        {
            icon.Dispose();
            _iconCache.Remove(item.Id);
        }

        SaveConfig();
        RefreshGrid();
    }

    private void ResetHotkey(AppBinding item)
    {
        using var form = new HotkeyCaptureForm(item.Hotkey);
        if (form.ShowDialog(this) != DialogResult.OK || form.Result is null)
        {
            return;
        }

        var oldHotkey = item.Hotkey;
        item.Hotkey = form.Result;
        item.UpdatedAtUtc = DateTime.UtcNow;

        if (!TryRegisterBinding(item, out var error))
        {
            item.Hotkey = oldHotkey;
            TryRegisterBinding(item, out _);
            AppDialog.Warn(this, error);
            RefreshGrid();
            return;
        }

        _registerErrors.Remove(item.Id);
        SaveConfig();
        RefreshGrid();
    }

    private void ClearHotkey(AppBinding item)
    {
        _hotkeyService?.Unregister(item.Id);
        _registerErrors.Remove(item.Id);
        item.Hotkey = null;
        item.UpdatedAtUtc = DateTime.UtcNow;
        SaveConfig();
        RefreshGrid();
    }

    private void RegisterAllHotkeys()
    {
        if (_hotkeyService is null)
        {
            return;
        }

        _registerErrors.Clear();
        foreach (var item in _bindings)
        {
            if (!TryRegisterBinding(item, out var error))
            {
                _registerErrors[item.Id] = error;
            }
        }
    }

    private bool TryRegisterBinding(AppBinding item, out string error)
    {
        error = string.Empty;
        if (_hotkeyService is null)
        {
            error = L.T("Hotkey service is not initialized.", "热键服务尚未初始化。");
            return false;
        }

        if (!File.Exists(item.ExePath))
        {
            error = L.T("Invalid path: executable file does not exist.", "路径失效：可执行文件不存在。");
            return false;
        }

        if (item.Hotkey is null || item.Hotkey.Key == Keys.None)
        {
            _hotkeyService.Unregister(item.Id);
            _registerErrors.Remove(item.Id);
            return true;
        }

        var duplicate = _bindings.FirstOrDefault(x =>
            x.Id != item.Id &&
            x.Hotkey is not null &&
            x.Hotkey.Ctrl == item.Hotkey.Ctrl &&
            x.Hotkey.Alt == item.Hotkey.Alt &&
            x.Hotkey.Shift == item.Hotkey.Shift &&
            x.Hotkey.Win == item.Hotkey.Win &&
            x.Hotkey.Key == item.Hotkey.Key);
        if (duplicate is not null)
        {
            error = L.T($"Hotkey conflicts with {duplicate.AppName}.", $"快捷键与 {duplicate.AppName} 冲突。");
            return false;
        }

        var ok = _hotkeyService.TryRegister(item.Id, item.Hotkey, out var registerError);
        if (!ok)
        {
            error = registerError;
            _registerErrors[item.Id] = registerError;
            return false;
        }

        _registerErrors.Remove(item.Id);
        return true;
    }

    private void RefreshGrid()
    {
        var rows = _bindings.Select(x => new MainGridRow
        {
            BindingId = x.Id,
            AppIcon = GetAppIcon(x),
            AppName = x.AppName,
            Hotkey = GetHotkeyText(x),
            Status = GetStatus(x),
            ResetText = L.T("Reset", "重设"),
            ClearText = L.T("Clear", "清除快捷键"),
            RemoveText = L.T("Remove", "删除应用")
        }).ToList();

        _grid.DataSource = rows;

        var okStatus = L.T("OK", "正常");
        var conflictStatus = L.T("Conflict", "冲突");
        var invalidStatus = L.T("Invalid Path", "路径失效");
        var noHotkeyStatus = L.T("No Hotkey", "未设置快捷键");

        var okCount = rows.Count(x => x.Status == okStatus);
        var conflictCount = rows.Count(x => x.Status == conflictStatus);
        var invalidCount = rows.Count(x => x.Status == invalidStatus);
        var noHotkeyCount = rows.Count(x => x.Status == noHotkeyStatus);

        _statusLabel.Text = L.T(
            $"Total {rows.Count}, OK {okCount}, Conflict {conflictCount}, Invalid Path {invalidCount}, No Hotkey {noHotkeyCount}",
            $"共 {rows.Count} 项，正常 {okCount}，冲突 {conflictCount}，路径失效 {invalidCount}，未设置快捷键 {noHotkeyCount}");
    }

    private string GetStatus(AppBinding item)
    {
        if (!File.Exists(item.ExePath))
        {
            return L.T("Invalid Path", "路径失效");
        }

        if (_registerErrors.ContainsKey(item.Id))
        {
            return L.T("Conflict", "冲突");
        }

        if (item.Hotkey is null || item.Hotkey.Key == Keys.None)
        {
            return L.T("No Hotkey", "未设置快捷键");
        }

        return L.T("OK", "正常");
    }

    private string GetHotkeyText(AppBinding item)
    {
        if (item.Hotkey is null || item.Hotkey.Key == Keys.None)
        {
            return L.T("Not set", "未设置");
        }

        return item.Hotkey.ToString();
    }

    private Image GetAppIcon(AppBinding item)
    {
        if (_iconCache.TryGetValue(item.Id, out var cached))
        {
            return cached;
        }

        Image image = SystemIcons.Application.ToBitmap();
        try
        {
            if (File.Exists(item.ExePath))
            {
                using var icon = Icon.ExtractAssociatedIcon(item.ExePath);
                if (icon is not null)
                {
                    using var small = new Icon(icon, new Size(16, 16));
                    image = small.ToBitmap();
                }
            }
        }
        catch
        {
        }

        _iconCache[item.Id] = image;
        return image;
    }

    private void SaveConfig()
    {
        _config.Bindings = _bindings.ToList();
        _configService.Save(_config);
    }

    private void ShowFromTray()
    {
        if (!Visible)
        {
            Show();
        }

        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        Close();
    }

    private void OnStartupMenuCheckedChanged(object? sender, EventArgs e)
    {
        try
        {
            _startupService.SetEnabled(_startupMenuItem.Checked);
        }
        catch
        {
            _startupMenuItem.CheckedChanged -= OnStartupMenuCheckedChanged;
            _startupMenuItem.Checked = !_startupMenuItem.Checked;
            _startupMenuItem.CheckedChanged += OnStartupMenuCheckedChanged;
            AppDialog.Warn(this, L.T("Failed to update startup setting.", "开机启动设置更新失败。"));
        }
    }

    private void OnMainFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _hotkeyService?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();

        foreach (var icon in _iconCache.Values)
        {
            icon.Dispose();
        }

        _iconCache.Clear();
    }

    private sealed class MainGridRow
    {
        public Guid BindingId { get; set; }
        public Image AppIcon { get; set; } = SystemIcons.Application.ToBitmap();
        public string AppName { get; set; } = string.Empty;
        public string Hotkey { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ResetText { get; set; } = string.Empty;
        public string ClearText { get; set; } = string.Empty;
        public string RemoveText { get; set; } = string.Empty;
    }
}

