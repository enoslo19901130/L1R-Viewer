using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using L1MapViewer.Helper;
using L1MapViewer.Models;
using SkiaSharp;

namespace L1MapViewer.CLI.Commands
{
    /// <summary>
    /// 地圖匯出相關命令。
    /// headless 算圖走 MapExporter (SkiaSharp + TileProvider)，避開 WinForms→Eto shim 的 Bitmap 型別問題。
    /// </summary>
    public static class ExportCommands
    {
        /// <summary>
        /// export-fullmap 命令 - 匯出單張地圖全圖
        /// </summary>
        public static int ExportFullMap(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: -cli export-fullmap <地圖資料夾> <輸出.png> [選項]");
                Console.WriteLine();
                Console.WriteLine("選項:");
                Console.WriteLine("  --scale <比例>    縮放比例 (預設 1.0，即原始大小)");
                Console.WriteLine("  --max-size <px>   最大邊長像素 (與 scale 互斥)");
                Console.WriteLine("  --no-layer8       不繪製 Layer8 SPR 特效");
                Console.WriteLine();
                Console.WriteLine("範例:");
                Console.WriteLine("  export-fullmap C:\\client\\map\\4 map4.png");
                Console.WriteLine("  export-fullmap C:\\client\\map\\4 map4.png --scale 0.5");
                Console.WriteLine("  export-fullmap C:\\client\\map\\4 map4.png --max-size 4096");
                return 1;
            }

            string mapPath = args[0];
            string outputPath = args[1];

            float scale = 1.0f;
            int maxSize = 0;
            bool showLayer8 = true;

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--scale" && i + 1 < args.Length)
                {
                    if (!float.TryParse(args[++i], out scale) || scale <= 0 || scale > 1)
                    {
                        Console.WriteLine("錯誤: scale 必須在 0 到 1 之間");
                        return 1;
                    }
                }
                else if (args[i] == "--max-size" && i + 1 < args.Length)
                {
                    if (!int.TryParse(args[++i], out maxSize) || maxSize <= 0)
                    {
                        Console.WriteLine("錯誤: max-size 必須為正整數");
                        return 1;
                    }
                }
                else if (args[i] == "--no-layer8")
                {
                    showLayer8 = false;
                }
                else if (args[i] == "--quality" && i + 1 < args.Length)
                {
                    // 相容舊參數；Skia 路徑固定高品質，略過數值
                    i++;
                }
            }

            // MapLoader 設定 Share.LineagePath 並預讀 map 索引
            var loadResult = MapLoader.Load(mapPath);
            if (!loadResult.Success)
                return 1;

            var document = new MapDocument();
            if (!document.Load(loadResult.MapId) || document.S32Files.Count == 0)
            {
                Console.WriteLine($"無法載入地圖文件: {loadResult.MapId}");
                return 1;
            }

            int scalePercent = ComputeScalePercent(document, scale, maxSize);
            int outputWidth = Math.Max(1, (int)(document.MapPixelWidth * (scalePercent / 100f)));
            int outputHeight = Math.Max(1, (int)(document.MapPixelHeight * (scalePercent / 100f)));

            Console.WriteLine($"地圖: {document.MapId}");
            Console.WriteLine($"原始大小: {document.MapPixelWidth} x {document.MapPixelHeight} px");
            Console.WriteLine($"縮放: {scalePercent}%");
            Console.WriteLine($"輸出大小: {outputWidth} x {outputHeight} px");
            Console.WriteLine($"S32 區塊: {document.S32Files.Count}");

            var options = new MapExporter.ExportOptions
            {
                ShowLayer1 = true,
                ShowLayer2 = true,
                ShowLayer4 = true,
                ShowLayer8 = showLayer8,
                ScalePercent = scalePercent,
            };

            var exporter = new MapExporter();
            var sw = Stopwatch.StartNew();
            try
            {
                using var bitmap = exporter.ExportMap(document, options);
                sw.Stop();
                if (bitmap == null)
                {
                    Console.WriteLine("渲染失敗: ExportMap 回傳 null");
                    return 1;
                }

                Console.WriteLine($"渲染耗時: {sw.ElapsedMilliseconds} ms");

                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string ext = Path.GetExtension(outputPath).ToLowerInvariant();
                if (ext == ".jpg" || ext == ".jpeg")
                    SaveAsJpeg(bitmap, outputPath, quality: 90);
                else
                    exporter.SaveToPng(bitmap, outputPath);

                var fi = new FileInfo(outputPath);
                Console.WriteLine($"已儲存: {outputPath} ({fi.Length:N0} bytes)");
                return 0;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"錯誤: {ex.Message}");
                return 1;
            }
            finally
            {
                exporter.ClearCache();
            }
        }

        /// <summary>
        /// batch-export 命令 - 批次匯出所有地圖
        /// </summary>
        public static int BatchExport(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: -cli batch-export <map目錄> <輸出目錄> [選項]");
                Console.WriteLine();
                Console.WriteLine("選項:");
                Console.WriteLine("  --scale <比例>    縮放比例 (預設 1.0)");
                Console.WriteLine("  --max-size <px>   最大邊長像素 (與 scale 互斥)");
                Console.WriteLine("  --format <格式>   輸出格式 png/jpg (預設 png)");
                Console.WriteLine("  --skip-existing   跳過已存在的檔案");
                Console.WriteLine("  --no-layer8       不繪製 Layer8 SPR");
                Console.WriteLine();
                Console.WriteLine("範例:");
                Console.WriteLine("  batch-export C:\\client\\map C:\\output");
                Console.WriteLine("  batch-export C:\\client\\map C:\\output --max-size 2048 --format jpg");
                return 1;
            }

            string mapRoot = args[0];
            string outputDir = args[1];

            float scale = 1.0f;
            int maxSize = 0;
            string format = "png";
            bool skipExisting = false;
            bool showLayer8 = true;

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--scale" && i + 1 < args.Length)
                    float.TryParse(args[++i], out scale);
                else if (args[i] == "--max-size" && i + 1 < args.Length)
                    int.TryParse(args[++i], out maxSize);
                else if (args[i] == "--format" && i + 1 < args.Length)
                    format = args[++i].ToLowerInvariant();
                else if (args[i] == "--skip-existing")
                    skipExisting = true;
                else if (args[i] == "--no-layer8")
                    showLayer8 = false;
                else if (args[i] == "--quality" && i + 1 < args.Length)
                    i++; // 相容舊參數
            }

            if (!Directory.Exists(mapRoot))
            {
                Console.WriteLine($"目錄不存在: {mapRoot}");
                return 1;
            }

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var mapDirs = Directory.GetDirectories(mapRoot)
                .Where(d => Directory.GetFiles(d, "*.s32").Length > 0)
                .OrderBy(d =>
                {
                    string name = Path.GetFileName(d);
                    return int.TryParse(name, out int n) ? n : int.MaxValue;
                })
                .ToList();

            Console.WriteLine($"找到 {mapDirs.Count} 個地圖");
            Console.WriteLine();

            int successCount = 0;
            int skipCount = 0;
            int failCount = 0;
            var totalSw = Stopwatch.StartNew();
            var exporter = new MapExporter();

            try
            {
                for (int i = 0; i < mapDirs.Count; i++)
                {
                    string mapPath = mapDirs[i];
                    string mapId = Path.GetFileName(mapPath);
                    string outputPath = Path.Combine(outputDir, $"{mapId}.{format}");

                    Console.WriteLine($"[{i + 1}/{mapDirs.Count}] 處理地圖 {mapId}...");

                    if (skipExisting && File.Exists(outputPath))
                    {
                        Console.WriteLine("  已存在，跳過");
                        skipCount++;
                        continue;
                    }

                    try
                    {
                        var loadResult = MapLoader.Load(mapPath, verbose: false);
                        if (!loadResult.Success)
                        {
                            Console.WriteLine("  載入失敗");
                            failCount++;
                            continue;
                        }

                        var document = new MapDocument();
                        if (!document.Load(loadResult.MapId) || document.S32Files.Count == 0)
                        {
                            Console.WriteLine("  MapDocument 載入失敗");
                            failCount++;
                            continue;
                        }

                        int scalePercent = ComputeScalePercent(document, scale, maxSize);
                        int outputWidth = Math.Max(1, (int)(document.MapPixelWidth * (scalePercent / 100f)));
                        int outputHeight = Math.Max(1, (int)(document.MapPixelHeight * (scalePercent / 100f)));

                        var options = new MapExporter.ExportOptions
                        {
                            ShowLayer1 = true,
                            ShowLayer2 = true,
                            ShowLayer4 = true,
                            ShowLayer8 = showLayer8,
                            ScalePercent = scalePercent,
                        };

                        var sw = Stopwatch.StartNew();
                        using var bitmap = exporter.ExportMap(document, options);
                        sw.Stop();

                        if (bitmap == null)
                        {
                            Console.WriteLine("  渲染失敗");
                            failCount++;
                            continue;
                        }

                        if (format == "jpg" || format == "jpeg")
                            SaveAsJpeg(bitmap, outputPath, quality: 90);
                        else
                            exporter.SaveToPng(bitmap, outputPath);

                        Console.WriteLine($"  {document.MapPixelWidth}x{document.MapPixelHeight} -> {outputWidth}x{outputHeight} ({scalePercent}%), {sw.ElapsedMilliseconds}ms");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  錯誤: {ex.Message}");
                        failCount++;
                    }

                    if ((i + 1) % 10 == 0)
                    {
                        exporter.ClearCache();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
            finally
            {
                exporter.ClearCache();
            }

            totalSw.Stop();

            Console.WriteLine();
            Console.WriteLine("=== 完成 ===");
            Console.WriteLine($"成功: {successCount}");
            Console.WriteLine($"跳過: {skipCount}");
            Console.WriteLine($"失敗: {failCount}");
            Console.WriteLine($"總耗時: {totalSw.Elapsed.TotalSeconds:F1} 秒");

            return failCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// 依 scale / max-size / 記憶體上限計算實際縮放百分比。
        /// </summary>
        private static int ComputeScalePercent(MapDocument document, float scale, int maxSize)
        {
            float effective = scale;
            if (effective <= 0 || effective > 1) effective = 1f;

            if (maxSize > 0 && document.MapPixelWidth > 0 && document.MapPixelHeight > 0)
            {
                float byMax = Math.Min(
                    (float)maxSize / document.MapPixelWidth,
                    (float)maxSize / document.MapPixelHeight);
                if (byMax < effective) effective = byMax;
                if (effective > 1f) effective = 1f;
            }

            int percent = Math.Max(1, (int)Math.Round(effective * 100f));
            if (percent > 100) percent = 100;

            // 超過 MapExporter 記憶體上限時自動縮小
            if (MapExporter.WillExceedMemoryLimit(document, percent))
            {
                int maxSafe = MapExporter.GetMaxScaleWithinLimit(document);
                if (maxSafe < percent)
                {
                    Console.WriteLine($"記憶體限制: 自動縮放 {percent}% → {maxSafe}%");
                    percent = maxSafe;
                }
            }

            return Math.Max(1, percent);
        }

        private static void SaveAsJpeg(SKBitmap bitmap, string filePath, int quality)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
        }
    }
}
