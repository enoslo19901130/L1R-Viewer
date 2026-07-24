using System.Diagnostics;
using Eto.Drawing;
using Eto.Forms;
using L1R.Shared;

namespace L1R.Shell;

public sealed class MainForm : Form
{
    readonly Label _clientLabel;
    readonly Label _statusLabel;
    readonly Label _modeBadge;
    readonly ListBox _recentList;
    readonly TextArea _doctorOutput;
    AppSettings _settings;

    public MainForm()
    {
        _settings = AppSettings.Load();

        Title = Program.EnableEdit
            ? "L1R-Viewer — 主畫面 [編輯模式]"
            : "L1R-Viewer — 主畫面 [唯讀]";
        ClientSize = new Size(720, 560);
        MinimumSize = new Size(560, 420);

        _modeBadge = new Label
        {
            Text = Program.EnableEdit ? "模式：編輯（危險操作已解鎖）" : "模式：唯讀（建議日常使用）",
            TextColor = Program.EnableEdit ? Colors.DarkOrange : Colors.DarkGreen,
            Font = new Font(SystemFont.Bold, 11)
        };

        _clientLabel = new Label { Text = "尚未選擇客戶端" };
        _statusLabel = new Label { Text = "就緒。請選擇含 map\\ 與 Tile.idx 的客戶端根目錄。" };

        _recentList = new ListBox { Height = 120 };
        _recentList.MouseDoubleClick += (_, _) => UseSelectedRecent();

        _doctorOutput = new TextArea
        {
            ReadOnly = true,
            Wrap = true,
            Height = 140,
            Font = new Font(FontFamilies.Monospace, 9)
        };

        var btnBrowse = new Button { Text = "選擇客戶端資料夾…" };
        btnBrowse.Click += (_, _) => BrowseClient();

        var btnDoctor = new Button { Text = "健康檢查 (doctor)" };
        btnDoctor.Click += (_, _) => RunDoctor();

        var btnMap = new Button { Text = "開啟地圖 MapViewer", Width = 200 };
        btnMap.Click += (_, _) => LaunchMap();

        var btnPak = new Button { Text = "開啟資產 PakBrowser", Width = 200 };
        btnPak.Click += (_, _) => LaunchPak();

        var btnOut = new Button { Text = "開啟輸出資料夾" };
        btnOut.Click += (_, _) => OpenOutputDir();

        var btnHelp = new Button { Text = "說明（五分鐘上手）" };
        btnHelp.Click += (_, _) => OpenGettingStarted();

        var btnSettings = new Button { Text = "設定…" };
        btnSettings.Click += (_, _) => ShowSettings();

        var btnRemember = new Button { Text = "記住目前路徑" };
        btnRemember.Click += (_, _) => RememberCurrent();

        var actions = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Items = { btnMap, btnPak, btnOut, btnHelp }
        };

        var topRow = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Items = { btnBrowse, btnDoctor, btnRemember, btnSettings }
        };

        Content = new Scrollable
        {
            Content = new StackLayout
            {
                Padding = 16,
                Spacing = 10,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Items =
                {
                    new Label
                    {
                        Text = "L1R-Viewer",
                        Font = new Font(SystemFont.Bold, 18)
                    },
                    new Label
                    {
                        Text = "離線讀取天堂 R 客戶端：地圖、精靈、封存。預設唯讀、不上 MCP 寫入。",
                        TextColor = Colors.Gray
                    },
                    _modeBadge,
                    new Label { Text = "目前客戶端：", Font = new Font(SystemFont.Bold, 10) },
                    _clientLabel,
                    topRow,
                    new Label { Text = "最近使用：", Font = new Font(SystemFont.Bold, 10) },
                    _recentList,
                    actions,
                    new Label { Text = "健康檢查結果：", Font = new Font(SystemFont.Bold, 10) },
                    _doctorOutput,
                    _statusLabel
                }
            }
        };

        RefreshFromSettings();
    }

    void RefreshFromSettings()
    {
        _settings = AppSettings.Load();
        string? path = _settings.LastClientPath;
        _clientLabel.Text = string.IsNullOrEmpty(path) ? "（未設定）" : path;
        _recentList.Items.Clear();
        foreach (var r in _settings.RecentClients)
        {
            if (!string.IsNullOrEmpty(r.Path))
                _recentList.Items.Add(r.Path);
        }
    }

    void BrowseClient()
    {
        using var dlg = new SelectFolderDialog
        {
            Title = "選擇天堂客戶端根目錄（需含 map\\ 與 Tile.idx）"
        };
        if (!string.IsNullOrEmpty(_settings.LastClientPath) && Directory.Exists(_settings.LastClientPath))
            dlg.Directory = _settings.LastClientPath;

        if (dlg.ShowDialog(this) != DialogResult.Ok)
            return;

        ApplyClient(dlg.Directory);
    }

    void UseSelectedRecent()
    {
        if (_recentList.SelectedIndex < 0 || _recentList.SelectedIndex >= _recentList.Items.Count)
            return;
        string path = _recentList.Items[_recentList.SelectedIndex].Text;
        ApplyClient(path);
    }

    void ApplyClient(string path)
    {
        var result = ClientPathValidator.Validate(path);
        _doctorOutput.Text = result.FormatOperatorMessage();
        if (!result.Ok)
        {
            _statusLabel.Text = "客戶端驗證失敗，請看上方說明。";
            MessageBox.Show(this, result.FormatOperatorMessage(), "客戶端無效", MessageBoxType.Warning);
            return;
        }

        _settings.RememberClient(result.Path!);
        _settings.Save();
        RefreshFromSettings();
        _statusLabel.Text = "客戶端已驗證並記住。";
    }

    void RememberCurrent()
    {
        string? path = _settings.LastClientPath;
        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show(this, "尚未選擇客戶端。", "提示", MessageBoxType.Information);
            return;
        }
        ApplyClient(path);
    }

    void RunDoctor()
    {
        string? path = _settings.LastClientPath;
        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show(this, "請先選擇客戶端資料夾。", "提示", MessageBoxType.Information);
            return;
        }

        var result = ClientPathValidator.Validate(path);
        _doctorOutput.Text = result.FormatOperatorMessage();
        _statusLabel.Text = result.Ok ? "健康檢查：通過" : "健康檢查：未通過";
        if (result.Ok)
        {
            _settings.RememberClient(result.Path!);
            _settings.Save();
            RefreshFromSettings();
        }
    }

    void LaunchMap()
    {
        string? client = RequireClient();
        if (client == null) return;

        string? exe = ToolLocator.FindMapViewer();
        if (exe == null)
        {
            MessageBox.Show(this,
                OperatorMessage.Format(
                    "找不到 MapViewer 執行檔。",
                    "尚未建置或路徑不正確。",
                    "請在倉庫根目錄執行：dotnet build L1R-Viewer.slnx -c Release"),
                "無法啟動", MessageBoxType.Error);
            return;
        }

        var args = new List<string>();
        if (Program.EnableEdit) args.Add("--enable-edit");
        args.Add(client);
        StartDetached(exe, args);
        _statusLabel.Text = "已啟動 MapViewer。";
    }

    void LaunchPak()
    {
        string? client = RequireClient();
        if (client == null) return;

        string? exe = ToolLocator.FindPakBrowser();
        if (exe == null)
        {
            MessageBox.Show(this,
                OperatorMessage.Format(
                    "找不到 PakBrowser 執行檔。",
                    "尚未建置或路徑不正確。",
                    "請執行：dotnet build L1R-Viewer.slnx -c Release"),
                "無法啟動", MessageBoxType.Error);
            return;
        }

        var args = new List<string>();
        if (Program.EnableEdit) args.Add("--enable-edit");
        args.Add(client);
        StartDetached(exe, args);
        _statusLabel.Text = "已啟動 PakBrowser。";
    }

    string? RequireClient()
    {
        string? path = _settings.LastClientPath;
        if (string.IsNullOrEmpty(path))
        {
            MessageBox.Show(this, "請先選擇並通過健康檢查的客戶端。", "提示", MessageBoxType.Information);
            return null;
        }
        var v = ClientPathValidator.Validate(path);
        if (!v.Ok)
        {
            _doctorOutput.Text = v.FormatOperatorMessage();
            MessageBox.Show(this, v.FormatOperatorMessage(), "客戶端無效", MessageBoxType.Warning);
            return null;
        }
        return v.Path;
    }

    void OpenOutputDir()
    {
        string dir = _settings.DefaultOutputDir ?? AppSettings.GetDefaultOutputDirectory();
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true
        });
        _statusLabel.Text = $"已開啟輸出資料夾：{dir}";
    }

    void OpenGettingStarted()
    {
        string? md = ToolLocator.FindGettingStarted();
        if (md == null)
        {
            MessageBox.Show(this,
                "找不到 docs/GETTING-STARTED.md。請在 GitHub 或倉庫 docs 資料夾閱讀。",
                "說明", MessageBoxType.Information);
            return;
        }
        Process.Start(new ProcessStartInfo { FileName = md, UseShellExecute = true });
    }

    void ShowSettings()
    {
        _settings = AppSettings.Load();
        var dlg = new Dialog
        {
            Title = "L1R-Viewer 設定",
            ClientSize = new Size(480, 280),
            Resizable = false
        };

        var outPath = new TextBox { Text = _settings.DefaultOutputDir ?? AppSettings.GetDefaultOutputDirectory(), Width = 360 };
        var maxSize = new NumericStepper
        {
            MinValue = 256,
            MaxValue = 16384,
            Value = _settings.Map.DefaultMaxSize,
            Increment = 256
        };
        var editCheck = new CheckBox
        {
            Text = "啟用進階編輯（危險：可寫回 S32/PAK；MCP 仍唯讀）",
            Checked = _settings.Ui.EnableEdit
        };

        var btnOk = new Button { Text = "儲存" };
        var btnCancel = new Button { Text = "取消" };
        btnOk.Click += (_, _) =>
        {
            _settings.DefaultOutputDir = outPath.Text;
            _settings.Map.DefaultMaxSize = (int)maxSize.Value;
            bool wantEdit = editCheck.Checked == true;
            if (wantEdit && !_settings.Ui.EnableEdit)
            {
                var confirm = MessageBox.Show(this,
                    "即將啟用編輯模式。寫入可能無法復原，且僅本機 GUI/CLI 有效。\nMCP 永遠不會寫入。\n\n確定？",
                    "確認編輯模式",
                    MessageBoxButtons.YesNo,
                    MessageBoxType.Warning);
                if (confirm != DialogResult.Yes)
                    return;
            }
            _settings.Ui.EnableEdit = wantEdit;
            _settings.Save();
            dlg.Close();
            MessageBox.Show(this,
                "設定已儲存。編輯模式變更請重新啟動主畫面後生效。",
                "已儲存", MessageBoxType.Information);
            RefreshFromSettings();
        };
        btnCancel.Click += (_, _) => dlg.Close();

        dlg.Content = new StackLayout
        {
            Padding = 12,
            Spacing = 8,
            Items =
            {
                new Label { Text = "預設輸出資料夾" },
                outPath,
                new Label { Text = "地圖匯出預設 max-size（像素）" },
                maxSize,
                editCheck,
                new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Items = { btnOk, btnCancel }
                }
            }
        };
        dlg.ShowModal(this);
    }

    static void StartDetached(string exe, List<string> args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        Process.Start(psi);
    }
}
