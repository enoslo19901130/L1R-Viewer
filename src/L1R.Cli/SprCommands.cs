using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Lin.Helper.Core.Pak;
using Lin.Helper.Core.Sprite;
using SixLabors.ImageSharp.Formats.Png;

namespace PakViewer.Cli
{
    internal static class SprCommands
    {
        public static int Run(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            var command = args[0].ToLowerInvariant();
            var subArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

            return command switch
            {
                "info" => Info(subArgs),
                "export" => Export(subArgs),
                "search" => Search(subArgs),
                "list-parse" => ListParse(subArgs),
                "list-convert" => ListConvert(subArgs),
                "--help" or "-h" => PrintUsageOk(),
                _ => Unknown(command)
            };
        }

        static bool WantsJson(string[] args) =>
            args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));

        static int Info(string[] args)
        {
            if (args.Length < 2) { Console.Error.WriteLine("Usage: pakviewer-cli spr info <client-folder|idx-file> <spr-name|id> [--json]"); return 1; }

            var source = args[0];
            var sprName = args[1];
            bool json = WantsJson(args);

            // 支援純數字 ID：列出所有 {id}-*.spx / {id}.spr 變體
            if (int.TryParse(sprName, out int spriteId) && Directory.Exists(source))
            {
                return InfoById(source, spriteId, json);
            }

            var resolved = ResolveSprName(source, sprName);
            if (resolved == null) return 1;

            var data = LoadSprData(source, resolved);
            if (data == null) return 1;

            SprFrame[] frames;
            try
            {
                frames = DecodeFrames(data, resolved);
            }
            catch (Exception ex)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        ok = false,
                        command = "sprite.info",
                        error = ex.Message,
                        type = ex.GetType().Name
                    }));
                }
                else
                {
                    Console.Error.WriteLine($"Decode failed: {ex.Message}");
                }
                return 1;
            }

            if (json)
            {
                var frameMeta = frames.Select((f, i) => new
                {
                    index = i,
                    width = f.Width,
                    height = f.Height,
                    x_offset = f.XOffset,
                    y_offset = f.YOffset,
                    has_image = f.Image != null
                }).ToArray();
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = true,
                    command = "sprite.info",
                    file_name = resolved,
                    data_size = data.Length,
                    frame_count = frames.Length,
                    frames = frameMeta
                }));
            }
            else
            {
                Console.WriteLine($"SPR: {resolved}");
                Console.WriteLine($"Data Size: {data.Length:N0} bytes");
                Console.WriteLine($"Frames: {frames.Length}");
                Console.WriteLine();
                for (int i = 0; i < frames.Length; i++)
                {
                    var f = frames[i];
                    string imgInfo = f.Image != null ? $"{f.Width}x{f.Height}" : "no image";
                    Console.WriteLine($"  Frame {i}: {imgInfo}, offset=({f.XOffset},{f.YOffset})");
                }
            }

            return 0;
        }

        static int InfoById(string clientFolder, int spriteId, bool json)
        {
            var variants = FindVariants(clientFolder, spriteId);
            if (variants.Count == 0)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        ok = false,
                        command = "sprite.info",
                        sprite_id = spriteId,
                        error = $"No sprite variants found for id {spriteId}",
                        type = "NotFound"
                    }));
                }
                else
                {
                    Console.Error.WriteLine($"No sprite variants found for id {spriteId}");
                }
                return 1;
            }

            var variantInfos = new List<object>();
            foreach (var v in variants)
            {
                try
                {
                    var data = LoadSprData(clientFolder, v);
                    if (data == null) continue;
                    var frames = DecodeFrames(data, v);
                    variantInfos.Add(new
                    {
                        file_name = v,
                        data_size = data.Length,
                        frame_count = frames.Length,
                        frames = frames.Select((f, i) => new
                        {
                            index = i,
                            width = f.Width,
                            height = f.Height,
                            x_offset = f.XOffset,
                            y_offset = f.YOffset,
                            has_image = f.Image != null
                        }).ToArray()
                    });
                }
                catch (Exception ex)
                {
                    variantInfos.Add(new
                    {
                        file_name = v,
                        error = ex.Message
                    });
                }
            }

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = true,
                    command = "sprite.info",
                    sprite_id = spriteId,
                    client_path = Path.GetFullPath(clientFolder),
                    variant_count = variantInfos.Count,
                    variants = variantInfos
                }));
            }
            else
            {
                Console.WriteLine($"Sprite ID: {spriteId}");
                Console.WriteLine($"Variants: {variantInfos.Count}");
                foreach (var v in variants)
                    Console.WriteLine($"  - {v}");
            }
            return 0;
        }

        static int Export(string[] args)
        {
            if (args.Length < 2) { Console.Error.WriteLine("Usage: pakviewer-cli spr export <client-folder|idx-file> <spr-name|id> [-o <output-folder>] [--json]"); return 1; }

            var source = args[0];
            var sprName = args[1];
            string outputFolder = "output";
            bool json = WantsJson(args);
            int? frameFilter = null;

            for (int i = 2; i < args.Length; i++)
            {
                if ((args[i] == "-o" || args[i] == "--output") && i + 1 < args.Length)
                    outputFolder = args[++i];
                else if ((args[i] == "--frame" || args[i] == "-f") && i + 1 < args.Length && int.TryParse(args[i + 1], out int fi))
                {
                    frameFilter = fi;
                    i++;
                }
            }

            // 數字 ID：匯出第一個可用變體（或全部變體的 frame 0 不在此；先匯出最佳變體）
            if (int.TryParse(sprName, out int spriteId) && Directory.Exists(source))
            {
                var variants = FindVariants(source, spriteId);
                if (variants.Count == 0)
                {
                    Console.Error.WriteLine($"No sprite variants for id {spriteId}");
                    return 1;
                }
                sprName = variants[0];
            }
            else
            {
                var resolved = ResolveSprName(source, sprName);
                if (resolved == null) return 1;
                sprName = resolved;
            }

            var data = LoadSprData(source, sprName);
            if (data == null) return 1;

            SprFrame[] frames;
            try
            {
                frames = DecodeFrames(data, sprName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Decode failed: {ex.Message}");
                return 1;
            }

            Console.WriteLine($"Loaded {frames.Length} frames from {sprName}");
            Directory.CreateDirectory(outputFolder);

            var files = new List<string>();
            int exported = 0;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frameFilter.HasValue && i != frameFilter.Value) continue;
                var frame = frames[i];
                if (frame.Image == null)
                {
                    Console.WriteLine($"  Frame {i}: no image data, skipped");
                    continue;
                }

                var outputPath = Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(sprName)}_frame{i}.png");
                using (var fs = File.Create(outputPath))
                {
                    frame.Image.Save(fs, new PngEncoder());
                }
                Console.WriteLine($"  Frame {i}: {frame.Width}x{frame.Height} -> {outputPath}");
                files.Add(Path.GetFullPath(outputPath));
                exported++;
            }

            Console.WriteLine($"\nExported {exported}/{frames.Length} frames to {outputFolder}");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = true,
                    command = "sprite.export",
                    file_name = sprName,
                    frame_count = frames.Length,
                    exported_count = exported,
                    files,
                    output_directory = Path.GetFullPath(outputFolder)
                }));
            }
            return 0;
        }

        static int Search(string[] args)
        {
            if (args.Length < 2) { Console.Error.WriteLine("Usage: pakviewer-cli spr search <client-folder> <query> [--limit N] [--json]"); return 1; }

            var client = args[0];
            var query = args[1];
            int limit = 100;
            bool json = WantsJson(args);
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[i + 1], out int lim))
                {
                    limit = lim;
                    i++;
                }
            }

            if (!Directory.Exists(client))
            {
                Console.Error.WriteLine($"Client folder not found: {client}");
                return 1;
            }

            var matches = new List<object>();
            foreach (var idxFile in EnumerateSpriteIdx(client))
            {
                try
                {
                    using var pak = new PakFile(idxFile);
                    foreach (var entry in pak.Files)
                    {
                        string name = entry.FileName ?? "";
                        if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        matches.Add(new
                        {
                            file_name = name,
                            idx = Path.GetFileName(idxFile),
                            size = entry.FileSize
                        });
                        if (matches.Count >= limit) break;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: skip {Path.GetFileName(idxFile)}: {ex.Message}");
                }
                if (matches.Count >= limit) break;
            }

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = true,
                    command = "sprite.search",
                    query,
                    count = matches.Count,
                    results = matches
                }));
            }
            else
            {
                Console.WriteLine($"Search '{query}': {matches.Count} hit(s)");
                foreach (var m in matches)
                    Console.WriteLine($"  {m}");
            }
            return 0;
        }

        static int ListParse(string[] args)
        {
            if (args.Length < 1) { Console.Error.WriteLine("Usage: pakviewer-cli spr list-parse <sprlist-file>"); return 1; }

            var filePath = args[0];
            var result = SprListParser.LoadFromFile(filePath);

            Console.WriteLine($"SPR List: {Path.GetFileName(filePath)}");
            Console.WriteLine($"Total Entries: {result.TotalEntries}");
            Console.WriteLine($"Parsed Entries: {result.Entries.Count}");

            if (result.Warnings.Count > 0)
            {
                Console.WriteLine($"\nWarnings ({result.Warnings.Count}):");
                foreach (var w in result.Warnings.Take(10))
                    Console.WriteLine($"  {w}");
                if (result.Warnings.Count > 10)
                    Console.WriteLine($"  ... and {result.Warnings.Count - 10} more");
            }

            Console.WriteLine();
            int showCount = Math.Min(20, result.Entries.Count);
            Console.WriteLine($"First {showCount} entries:");
            for (int i = 0; i < showCount; i++)
            {
                var entry = result.Entries[i];
                Console.WriteLine($"  #{entry.Id}: ImageCount={entry.ImageCount}, Actions={entry.Actions.Count}, Attrs={entry.Attributes.Count}, Name={entry.Name}");
            }
            if (result.Entries.Count > showCount)
                Console.WriteLine($"  ... and {result.Entries.Count - showCount} more");

            return 0;
        }

        static int ListConvert(string[] args)
        {
            if (args.Length < 2) { Console.Error.WriteLine("Usage: pakviewer-cli spr list-convert <input-file> <output-file> [--compact]"); return 1; }

            var inputPath = args[0];
            var outputPath = args[1];
            bool compact = args.Any(a => a == "--compact");

            var sprList = SprListParser.LoadFromFile(inputPath);
            string output = compact
                ? SprListWriter.ToCompactFormat(sprList)
                : SprListWriter.ToStandardFormat(sprList);

            File.WriteAllText(outputPath, output);
            Console.WriteLine($"Converted: {Path.GetFileName(inputPath)} -> {Path.GetFileName(outputPath)} ({(compact ? "compact" : "standard")} format)");
            Console.WriteLine($"Entries: {sprList.Entries.Count}");
            return 0;
        }

        /// <summary>
        /// 從 client 資料夾或 IDX 檔案中找到並提取 SPR/SPX 資料
        /// </summary>
        static byte[] LoadSprData(string source, string sprName)
        {
            if (Directory.Exists(source))
            {
                foreach (var idxFile in EnumerateSpriteIdx(source))
                {
                    using var pak = new PakFile(idxFile);
                    int idx = pak.FindFileIndex(sprName);
                    if (idx >= 0)
                    {
                        Console.Error.WriteLine($"Found {sprName} in {Path.GetFileName(idxFile)}");
                        return pak.Extract(idx);
                    }
                }

                Console.Error.WriteLine($"File '{sprName}' not found in any sprite*.idx under {source}");
                return null;
            }

            if (File.Exists(source))
            {
                using var pak = new PakFile(source);
                return pak.Extract(sprName);
            }

            Console.Error.WriteLine($"Source not found: {source}");
            return null;
        }

        static IEnumerable<string> EnumerateSpriteIdx(string clientFolder)
        {
            return Directory.GetFiles(clientFolder, "sprite*.idx", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(clientFolder, "Sprite*.idx", SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 依副檔名選擇 SPR / SPX 解碼器
        /// </summary>
        static SprFrame[] DecodeFrames(byte[] data, string fileName)
        {
            if (fileName.EndsWith(".spx", StringComparison.OrdinalIgnoreCase))
                return L1SPX.Read(data);
            if (fileName.EndsWith(".sp2", StringComparison.OrdinalIgnoreCase))
            {
                // SP2 依 action 分組；合併所有 frame 供 info/export 使用
                var byAction = L1SP2.Read(data);
                return byAction.OrderBy(kv => kv.Key).SelectMany(kv => kv.Value).ToArray();
            }
            return SprReader.Load(data);
        }

        /// <summary>
        /// 解析使用者給的名稱（可省略副檔名），找到實際 idx 內檔名
        /// </summary>
        static string ResolveSprName(string source, string sprName)
        {
            if (string.IsNullOrWhiteSpace(sprName)) return null;

            // 已是完整檔名且找得到
            if (Directory.Exists(source))
            {
                foreach (var idxFile in EnumerateSpriteIdx(source))
                {
                    using var pak = new PakFile(idxFile);
                    if (pak.FindFileIndex(sprName) >= 0)
                        return sprName;
                }

                // 嘗試補副檔名
                foreach (var ext in new[] { ".spx", ".spr", ".sp2" })
                {
                    if (sprName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) continue;
                    string candidate = sprName + ext;
                    foreach (var idxFile in EnumerateSpriteIdx(source))
                    {
                        using var pak = new PakFile(idxFile);
                        if (pak.FindFileIndex(candidate) >= 0)
                            return candidate;
                    }
                }

                Console.Error.WriteLine($"File '{sprName}' not found in any sprite*.idx under {source}");
                return null;
            }

            return sprName;
        }

        /// <summary>
        /// 找出 sprite id 的所有變體檔名（如 167-0.spx, 167-1.spx）
        /// </summary>
        static List<string> FindVariants(string clientFolder, int spriteId)
        {
            var prefix = spriteId.ToString() + "-";
            var exactNames = new[]
            {
                spriteId + ".spx",
                spriteId + ".spr",
                spriteId + ".sp2"
            };
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var idxFile in EnumerateSpriteIdx(clientFolder))
            {
                try
                {
                    using var pak = new PakFile(idxFile);
                    foreach (var entry in pak.Files)
                    {
                        string name = entry.FileName ?? "";
                        if (exactNames.Any(e => e.Equals(name, StringComparison.OrdinalIgnoreCase))
                            || name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            found.Add(name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: skip {Path.GetFileName(idxFile)}: {ex.Message}");
                }
            }

            return found.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        static void PrintUsage()
        {
            Console.WriteLine("SPR/SPX sprite file operations");
            Console.WriteLine();
            Console.WriteLine("Usage: pakviewer-cli spr <command> [arguments]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  info <folder|idx> <spr-name|id> [--json]         Show SPR/SPX info (numeric id lists variants)");
            Console.WriteLine("  export <folder|idx> <spr-name|id> [-o <dir>] [--frame N] [--json]");
            Console.WriteLine("  search <folder> <query> [--limit N] [--json]     Search sprite idx entry names");
            Console.WriteLine("  list-parse <sprlist-file>                        Parse SPR list file");
            Console.WriteLine("  list-convert <input> <output> [--compact]        Convert SPR list format");
        }

        static int PrintUsageOk() { PrintUsage(); return 0; }
        static int Unknown(string cmd) { Console.Error.WriteLine($"Unknown spr command: {cmd}"); PrintUsage(); return 1; }
    }
}
