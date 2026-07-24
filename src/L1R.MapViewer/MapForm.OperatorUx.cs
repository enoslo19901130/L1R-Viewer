using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eto.Forms;
using L1MapViewer.Helper;
using L1MapViewer.Models;
using L1R.Shared;
using SkiaSharp;

namespace L1FlyMapViewer
{
    /// <summary>
    /// Phase 9: operator-friendly read exports + info panel (works in read-only mode).
    /// </summary>
    public partial class MapForm
    {
        private Label _opInfoSummary;
        private ListBox _opPortalList;
        private Label _opRegionLabel;
        private List<(string s32, string name, int x, int y, int target, int portalId)> _opPortals
            = new List<(string, string, int, int, int, int)>();

        private StackLayout BuildOperatorExportToolbar()
        {
            var row = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Padding = new Eto.Drawing.Padding(5, 0, 5, 5),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var lbl = new Label { Text = "匯出:", VerticalAlignment = VerticalAlignment.Center };
            var btnPng = new Button { Text = "地圖 PNG", ToolTip = "匯出目前地圖（自動 max-size）" };
            var btnPortals = new Button { Text = "傳送點 JSON", ToolTip = "匯出 Layer7 傳送點" };
            var btnPass = new Button { Text = "通行屬性", ToolTip = "匯出 Layer3 通行" };
            var btnFolder = new Button { Text = "輸出資料夾", ToolTip = "開啟匯出目錄" };
            var btnRefreshInfo = new Button { Text = "重新整理資訊", ToolTip = "更新側欄傳點/區域" };

            btnPng.Click += async (s, e) => await OperatorExportMapPngAsync();
            btnPortals.Click += (s, e) => OperatorExportPortals();
            btnPass.Click += (s, e) => OperatorExportPassability();
            btnFolder.Click += (s, e) => OperatorOpenOutputFolder();
            btnRefreshInfo.Click += (s, e) => RefreshOperatorInfoPanel();

            row.Items.Add(lbl);
            row.Items.Add(btnPng);
            row.Items.Add(btnPortals);
            row.Items.Add(btnPass);
            row.Items.Add(btnFolder);
            row.Items.Add(btnRefreshInfo);
            return row;
        }

        private Control BuildOperatorInfoPanel()
        {
            _opInfoSummary = new Label { Text = "載入地圖後顯示資訊", Wrap = WrapMode.Word };
            _opPortalList = new ListBox { Height = 220 };
            _opPortalList.MouseDoubleClick += (s, e) =>
            {
                if (_opPortalList.SelectedIndex < 0 || _opPortalList.SelectedIndex >= _opPortals.Count)
                    return;
                var p = _opPortals[_opPortalList.SelectedIndex];
                try
                {
                    // Jump to game coords if helper exists
                    toolStripStatusLabel1.Text = $"傳送點: {p.name} ({p.x},{p.y}) → map {p.target}";
                    toolStripStatusLabel2.Text = $"{p.x},{p.y}";
                }
                catch { /* ignore */ }
            };
            _opRegionLabel = new Label { Text = "區域檔：—", Wrap = WrapMode.Word };

            return new StackLayout
            {
                Padding = 8,
                Spacing = 6,
                Items =
                {
                    new Label { Text = "地圖資訊（唯讀）", Font = new Eto.Drawing.Font(Eto.Drawing.SystemFont.Bold, 10) },
                    _opInfoSummary,
                    new Label { Text = "傳送點 Layer7（雙擊看座標）", Font = new Eto.Drawing.Font(Eto.Drawing.SystemFont.Bold, 10) },
                    _opPortalList,
                    _opRegionLabel
                }
            };
        }

        private void RefreshOperatorInfoPanel()
        {
            if (_opInfoSummary == null) return;

            if (_document == null || string.IsNullOrEmpty(_document.MapId))
            {
                _opInfoSummary.Text = "尚未載入地圖。";
                _opPortalList?.Items.Clear();
                _opPortals.Clear();
                if (_opRegionLabel != null) _opRegionLabel.Text = "區域檔：—";
                return;
            }

            int segs = _document.S32Files.Count;
            _opInfoSummary.Text =
                $"地圖 ID: {_document.MapId}\n" +
                $"S32 分段: {segs}\n" +
                $"像素: {_document.MapPixelWidth} x {_document.MapPixelHeight}\n" +
                $"遊戲座標: X {_document.MapMinGameX}~{_document.MapMaxGameX}, Y {_document.MapMinGameY}~{_document.MapMaxGameY}";

            _opPortals.Clear();
            _opPortalList.Items.Clear();
            foreach (var s32 in _document.S32Files.Values)
            {
                if (s32.Layer7 == null) continue;
                string fname = Path.GetFileName(s32.FilePath);
                foreach (var portal in s32.Layer7)
                {
                    string name = portal.Name ?? "";
                    int x = portal.X;
                    int y = portal.Y;
                    int target = portal.TargetMapId;
                    int pid = portal.PortalId;
                    _opPortals.Add((fname, name, x, y, target, pid));
                    _opPortalList.Items.Add($"{name}  ({x},{y}) → {target}  [{fname}]");
                }
            }

            // Region files beside any s32
            int market = 0, tel = 0, fish = 0;
            string mapDir = null;
            foreach (var path in _document.S32Files.Keys)
            {
                mapDir = Path.GetDirectoryName(path);
                break;
            }
            if (!string.IsNullOrEmpty(mapDir) && Directory.Exists(mapDir))
            {
                market = Directory.GetFiles(mapDir, "*.MarketRegion").Length;
                tel = Directory.GetFiles(mapDir, "*.TeleportOkRegion").Length;
                fish = Directory.GetFiles(mapDir, "*.fishingRegion").Length;
            }
            _opRegionLabel.Text =
                $"區域檔（{mapDir}）:\n" +
                $"  MarketRegion: {market}\n" +
                $"  TeleportOkRegion: {tel}\n" +
                $"  fishingRegion: {fish}\n" +
                $"傳送點合計: {_opPortals.Count}";
        }

        private string GetOperatorOutputDir()
        {
            var settings = AppSettings.Load();
            string dir = settings.DefaultOutputDir ?? AppSettings.GetDefaultOutputDirectory();
            Directory.CreateDirectory(dir);
            return dir;
        }

        private int GetDefaultMaxSize()
        {
            var settings = AppSettings.Load();
            int m = settings.Map?.DefaultMaxSize ?? 2048;
            return m > 0 ? m : 2048;
        }

        private async Task OperatorExportMapPngAsync()
        {
            if (_document == null || _document.S32Files.Count == 0)
            {
                WinFormsMessageBox.Show("請先載入地圖。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                toolStripStatusLabel1.Text = "渲染地圖中…";
                int maxSize = GetDefaultMaxSize();
                string outDir = GetOperatorOutputDir();
                string outPath = Path.Combine(outDir, $"map-{_document.MapId}.png");

                await Task.Run(() =>
                {
                    int scalePercent = 100;
                    if (_document.MapPixelWidth > 0 && _document.MapPixelHeight > 0)
                    {
                        float s = Math.Min(
                            (float)maxSize / _document.MapPixelWidth,
                            (float)maxSize / _document.MapPixelHeight);
                        if (s < 1f) scalePercent = Math.Max(1, (int)Math.Round(s * 100));
                    }
                    if (MapExporter.WillExceedMemoryLimit(_document, scalePercent))
                        scalePercent = MapExporter.GetMaxScaleWithinLimit(_document);

                    var exporter = new MapExporter();
                    var options = new MapExporter.ExportOptions
                    {
                        ShowLayer1 = true,
                        ShowLayer2 = true,
                        ShowLayer4 = true,
                        ShowLayer8 = true,
                        ScalePercent = scalePercent
                    };
                    using var bmp = exporter.ExportMap(_document, options);
                    if (bmp == null)
                        throw new InvalidOperationException("ExportMap 回傳 null（可能記憶體不足，請在 Shell 設定較小 max-size）");
                    exporter.SaveToPng(bmp, outPath);
                    exporter.ClearCache();
                });

                toolStripStatusLabel1.Text = $"已匯出: {outPath}";
                WinFormsMessageBox.Show($"已儲存:\n{outPath}", "匯出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "OperatorExportMapPng failed");
                string hint = ex is OutOfMemoryException
                    ? "\n建議：在 Shell 設定將 max-size 調小（例如 1024）。"
                    : "";
                WinFormsMessageBox.Show($"匯出失敗: {ex.Message}{hint}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                toolStripStatusLabel1.Text = "匯出失敗";
            }
        }

        private void OperatorExportPortals()
        {
            if (_document == null || _document.S32Files.Count == 0)
            {
                WinFormsMessageBox.Show("請先載入地圖。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RefreshOperatorInfoPanel();
            string outDir = GetOperatorOutputDir();
            string outPath = Path.Combine(outDir, $"map-{_document.MapId}-portals.json");

            var portals = _opPortals.Select(p => new
            {
                s32 = p.s32,
                name = p.name,
                x = p.x,
                y = p.y,
                targetMapId = p.target,
                portalId = p.portalId
            }).ToList();

            var payload = new
            {
                ok = true,
                map_id = _document.MapId,
                count = portals.Count,
                portals
            };
            File.WriteAllText(outPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            toolStripStatusLabel1.Text = $"已匯出傳送點: {outPath} ({portals.Count})";
            WinFormsMessageBox.Show($"傳送點 {portals.Count} 筆\n{outPath}", "匯出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OperatorExportPassability()
        {
            if (_document == null || _document.S32Files.Count == 0)
            {
                WinFormsMessageBox.Show("請先載入地圖。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string mapDir = null;
            foreach (var path in _document.S32Files.Keys)
            {
                mapDir = Path.GetDirectoryName(path);
                break;
            }
            if (string.IsNullOrEmpty(mapDir) || !Directory.Exists(mapDir))
            {
                WinFormsMessageBox.Show("找不到地圖資料夾。", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string outDir = GetOperatorOutputDir();
            string outPath = Path.Combine(outDir, $"map-{_document.MapId}-pass.txt");

            // Reuse MapViewer CLI export-passability via process for consistency
            try
            {
                string exe = Process.GetCurrentProcess().MainModule?.FileName
                    ?? typeof(L1MapViewerCore.Program).Assembly.Location;
                // When running as MapViewer, invoke -cli on self
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    ArgumentList = { "-cli", "export-passability", mapDir, outPath }
                };
                // If assembly is dll, find L1MapViewerCore.exe beside it
                if (exe.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var name in new[] { "L1R-MapViewer.exe", "L1MapViewerCore.exe" })
                    {
                        string cand = Path.Combine(AppContext.BaseDirectory, name);
                        if (File.Exists(cand)) { psi.FileName = cand; break; }
                    }
                }

                toolStripStatusLabel1.Text = "匯出通行屬性中…";
                using var p = Process.Start(psi);
                p?.WaitForExit(180000);
                if (p == null || p.ExitCode != 0 || !File.Exists(outPath))
                {
                    // Fallback: write summary from Layer3
                    var sb = new StringBuilder();
                    sb.AppendLine($"# map {_document.MapId} passability summary");
                    foreach (var s32 in _document.S32Files.Values)
                    {
                        sb.AppendLine($"# {Path.GetFileName(s32.FilePath)} Layer3 present={s32.Layer3 != null}");
                    }
                    File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
                }
                toolStripStatusLabel1.Text = $"已匯出通行: {outPath}";
                WinFormsMessageBox.Show(outPath, "匯出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                WinFormsMessageBox.Show(ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OperatorOpenOutputFolder()
        {
            string dir = GetOperatorOutputDir();
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
    }
}
