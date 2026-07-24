using System;
using System.Text.Json;
using L1R.Shared;

namespace PakViewer.Cli
{
    /// <summary>
    /// Client health check: pakviewer-cli doctor &lt;client&gt; [--json] [--remember]
    /// </summary>
    internal static class DoctorCommands
    {
        public static int Run(string[] args)
        {
            if (args.Length == 0 || args[0] is "--help" or "-h")
            {
                PrintUsage();
                return args.Length == 0 ? 1 : 0;
            }

            string clientPath = args[0];
            bool json = false;
            bool remember = false;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].Equals("--json", StringComparison.OrdinalIgnoreCase))
                    json = true;
                else if (args[i].Equals("--remember", StringComparison.OrdinalIgnoreCase))
                    remember = true;
            }

            var result = ClientPathValidator.Validate(clientPath);

            if (json)
            {
                var payload = new
                {
                    ok = result.Ok,
                    command = "doctor",
                    path = result.Path,
                    missing = result.Missing,
                    hints = result.Hints,
                    error = result.Error,
                    reason = result.Reason,
                    suggestion = result.Suggestion,
                    has_map = result.HasMap,
                    has_tile_idx = result.HasTileIdx,
                    has_any_idx = result.HasAnyIdx,
                    has_sprite_idx = result.HasSpriteIdx,
                    map_count = result.MapCount,
                    idx_count = result.IdxCount
                };
                Console.WriteLine(JsonSerializer.Serialize(payload));
            }
            else
            {
                if (result.Ok)
                    Console.WriteLine(result.FormatOperatorMessage());
                else
                    Console.Error.WriteLine(result.FormatOperatorMessage());
            }

            if (result.Ok && remember && !string.IsNullOrEmpty(result.Path))
            {
                var settings = AppSettings.Load();
                settings.RememberClient(result.Path);
                settings.Save();
                if (!json)
                    Console.WriteLine($"已記住客戶端路徑（settings: {AppSettings.GetDefaultSettingsPath()}）");
            }

            return result.Ok ? 0 : 2;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: pakviewer-cli doctor <client-folder> [--json] [--remember]");
            Console.WriteLine();
            Console.WriteLine("  檢查客戶端根目錄是否含 map\\ 與 *.idx（建議 Tile.idx）。");
            Console.WriteLine("  失敗時輸出 錯誤/原因/建議，exit code = 2。");
            Console.WriteLine("  --json      單行 JSON");
            Console.WriteLine("  --remember  通過時寫入 %AppData%\\L1R-Viewer\\settings.json");
        }
    }
}
