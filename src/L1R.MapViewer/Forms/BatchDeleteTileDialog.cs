using System;
using Eto.Forms;
using Eto.Drawing;
using L1MapViewer.Compatibility;
using L1MapViewer.Localization;

namespace L1MapViewer.Forms
{
    /// <summary>
    /// 批次刪除 Tile 對話框
    /// </summary>
    public class BatchDeleteTileDialog : Dialog
    {
        /// <summary>
        /// TileId 起始值
        /// </summary>
        public int TileIdStart => (int)nudTileIdStart.Value;

        /// <summary>
        /// TileId 結束值
        /// </summary>
        public int TileIdEnd => (int)nudTileIdEnd.Value;

        /// <summary>
        /// IndexId 起始值
        /// </summary>
        public int IndexIdStart => (int)nudIndexIdStart.Value;

        /// <summary>
        /// IndexId 結束值
        /// </summary>
        public int IndexIdEnd => (int)nudIndexIdEnd.Value;

        /// <summary>
        /// 是否處理所有地圖
        /// </summary>
        public bool ProcessAllMaps => rbAllMaps.Checked;

        /// <summary>
        /// 使用者是否確認執行
        /// </summary>
        public bool Confirmed { get; private set; }

        private GroupBox grpTileId;
        private GroupBox grpIndexId;
        private GroupBox grpScope;
        private NumericStepper nudTileIdStart;
        private NumericStepper nudTileIdEnd;
        private NumericStepper nudIndexIdStart;
        private NumericStepper nudIndexIdEnd;
        private RadioButton rbCurrentMap;
        private RadioButton rbAllMaps;
        private Label lblTileIdStart;
        private Label lblTileIdEnd;
        private Label lblIndexIdStart;
        private Label lblIndexIdEnd;
        private Label lblWarning;
        private Button btnDelete;
        private Button btnCancel;

        private bool _hasCurrentMap;

        /// <summary>
        /// 建立批次刪除 Tile 對話框
        /// </summary>
        /// <param name="hasCurrentMap">是否已載入地圖</param>
        public BatchDeleteTileDialog(bool hasCurrentMap = true)
        {
            _hasCurrentMap = hasCurrentMap;
            InitializeComponents();
            UpdateLocalization();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            Application.Instance.Invoke(() => UpdateLocalization());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationManager.LanguageChanged -= OnLanguageChanged;
            }
            base.Dispose(disposing);
        }

        private void InitializeComponents()
        {
            Title = "批次刪除 Tile";
            MinimumSize = new Size(320, 300);
            Resizable = false;

            // TileId 範圍
            lblTileIdStart = new Label { Text = "起始:" };
            nudTileIdStart = new NumericStepper
            {
                MinValue = 0,
                MaxValue = 65535,
                Value = 0,
                Width = 80
            };

            lblTileIdEnd = new Label { Text = "結束:" };
            nudTileIdEnd = new NumericStepper
            {
                MinValue = 0,
                MaxValue = 65535,
                Value = 65535,
                Width = 80
            };

            grpTileId = new GroupBox
            {
                Text = "TileId 範圍",
                Content = new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Padding = new Padding(10),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Items =
                    {
                        lblTileIdStart,
                        nudTileIdStart,
                        lblTileIdEnd,
                        nudTileIdEnd
                    }
                }
            };

            // IndexId 範圍
            lblIndexIdStart = new Label { Text = "起始:" };
            nudIndexIdStart = new NumericStepper
            {
                MinValue = 0,
                MaxValue = 255,
                Value = 0,
                Width = 80
            };

            lblIndexIdEnd = new Label { Text = "結束:" };
            nudIndexIdEnd = new NumericStepper
            {
                MinValue = 0,
                MaxValue = 255,
                Value = 255,
                Width = 80
            };

            grpIndexId = new GroupBox
            {
                Text = "IndexId 範圍",
                Content = new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Padding = new Padding(10),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Items =
                    {
                        lblIndexIdStart,
                        nudIndexIdStart,
                        lblIndexIdEnd,
                        nudIndexIdEnd
                    }
                }
            };

            // 範圍選擇
            rbCurrentMap = new RadioButton
            {
                Text = "當前地圖",
                Checked = _hasCurrentMap,
                Enabled = _hasCurrentMap
            };

            rbAllMaps = new RadioButton(rbCurrentMap)
            {
                Text = "所有地圖 (maps 資料夾)",
                Checked = !_hasCurrentMap
            };

            grpScope = new GroupBox
            {
                Text = "處理範圍",
                Content = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 8,
                    Padding = new Padding(10),
                    Items = { rbCurrentMap, rbAllMaps }
                }
            };

            // 警告訊息
            lblWarning = new Label
            {
                Text = "此操作會直接修改 S32 檔案，請先備份！",
                TextColor = Colors.Red
            };

            // 按鈕
            btnDelete = new Button { Text = "刪除" };
            btnDelete.Click += BtnDelete_Click;

            btnCancel = new Button { Text = "取消" };
            btnCancel.Click += (s, e) => Close();

            // 主要佈局
            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Padding = new Padding(15),
                Spacing = 10,
                Items =
                {
                    grpTileId,
                    grpIndexId,
                    grpScope,
                    lblWarning,
                    new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 10,
                        Items =
                        {
                            null, // 彈性空間
                            btnDelete,
                            btnCancel
                        }
                    }
                }
            };

            DefaultButton = btnDelete;
            AbortButton = btnCancel;
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            // 驗證範圍
            if (nudTileIdStart.Value > nudTileIdEnd.Value)
            {
                WinFormsMessageBox.Show(
                    LocalizationManager.L("BatchDeleteTile_InvalidTileIdRange"),
                    LocalizationManager.L("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (nudIndexIdStart.Value > nudIndexIdEnd.Value)
            {
                WinFormsMessageBox.Show(
                    LocalizationManager.L("BatchDeleteTile_InvalidIndexIdRange"),
                    LocalizationManager.L("Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // 確認刪除
            var scope = rbAllMaps.Checked
                ? LocalizationManager.L("BatchDeleteTile_AllMaps")
                : LocalizationManager.L("BatchDeleteTile_CurrentMap");
            var message = string.Format(
                LocalizationManager.L("BatchDeleteTile_ConfirmMessage"),
                TileIdStart, TileIdEnd, IndexIdStart, IndexIdEnd, scope);

            var result = WinFormsMessageBox.Show(
                message,
                LocalizationManager.L("BatchDeleteTile_Confirm"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Confirmed = true;
                Close();
            }
        }

        private void UpdateLocalization()
        {
            Title = LocalizationManager.L("Form_BatchDeleteTile_Title");
            grpTileId.Text = LocalizationManager.L("BatchDeleteTile_TileIdRange");
            grpIndexId.Text = LocalizationManager.L("BatchDeleteTile_IndexIdRange");
            grpScope.Text = LocalizationManager.L("BatchDeleteTile_Scope");
            lblTileIdStart.Text = LocalizationManager.L("BatchDeleteTile_Start") + ":";
            lblTileIdEnd.Text = LocalizationManager.L("BatchDeleteTile_End") + ":";
            lblIndexIdStart.Text = LocalizationManager.L("BatchDeleteTile_Start") + ":";
            lblIndexIdEnd.Text = LocalizationManager.L("BatchDeleteTile_End") + ":";
            rbCurrentMap.Text = LocalizationManager.L("BatchDeleteTile_CurrentMap");
            rbAllMaps.Text = LocalizationManager.L("BatchDeleteTile_AllMaps");
            lblWarning.Text = LocalizationManager.L("BatchDeleteTile_Warning");
            btnDelete.Text = LocalizationManager.L("Button_Delete");
            btnCancel.Text = LocalizationManager.L("Button_Cancel");
        }
    }
}
