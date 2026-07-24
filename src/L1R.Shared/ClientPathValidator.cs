namespace L1R.Shared;

/// <summary>
/// Validates a Lineage client root folder for offline asset tooling.
/// Requires map\ and at least one *.idx (prefers Tile.idx).
/// </summary>
public static class ClientPathValidator
{
    public static ClientValidationResult Validate(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ClientValidationResult.Failed(
                error: "未指定客戶端資料夾。",
                reason: "client_path 為空。",
                suggestion: "請提供包含 map\\ 與 Tile.idx 的天堂客戶端根目錄。",
                missing: new[] { "path" });
        }

        string full;
        try
        {
            full = Path.GetFullPath(path.Trim().Trim('"'));
        }
        catch (Exception ex)
        {
            return ClientValidationResult.Failed(
                error: "客戶端路徑無法解析。",
                reason: ex.Message,
                suggestion: "請改用絕對路徑，例如 D:\\Games\\LineageRemastered。",
                missing: new[] { "path" });
        }

        if (!Directory.Exists(full))
        {
            return ClientValidationResult.Failed(
                error: "客戶端資料夾不存在。",
                reason: $"路徑不存在：{full}",
                suggestion: "請用檔案總管確認路徑，並選擇「客戶端根目錄」（其下應有 map 資料夾）。",
                missing: new[] { "directory" },
                path: full);
        }

        var missing = new List<string>();
        var hints = new List<string>();

        string mapDir = Path.Combine(full, "map");
        bool hasMap = Directory.Exists(mapDir);
        if (!hasMap)
        {
            missing.Add("map");
            hints.Add("此資料夾下找不到 map\\。請選到「客戶端根目錄」，而不是 map 子資料夾本身。");
        }

        // Prefer Tile.idx; accept any *.idx as archive presence signal
        string tileIdx = Path.Combine(full, "Tile.idx");
        bool hasTileIdx = File.Exists(tileIdx);
        string[] idxFiles = Array.Empty<string>();
        try
        {
            idxFiles = Directory.GetFiles(full, "*.idx", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex)
        {
            return ClientValidationResult.Failed(
                error: "無法讀取客戶端目錄內容。",
                reason: ex.Message,
                suggestion: "請確認對該資料夾有讀取權限。",
                missing: new[] { "idx" },
                path: full);
        }

        bool hasAnyIdx = idxFiles.Length > 0;
        if (!hasAnyIdx)
        {
            missing.Add("*.idx");
            hints.Add("找不到任何 *.idx（預期至少有 Tile.idx）。這通常不是完整客戶端根目錄。");
        }
        else if (!hasTileIdx)
        {
            hints.Add("有 *.idx 但沒有 Tile.idx；地圖渲染可能失敗。建議使用含 Tile.idx / Tile.pak 的完整客戶端。");
        }

        // Optional signals (not hard fail)
        bool hasSpriteIdx = Directory.GetFiles(full, "sprite*.idx", SearchOption.TopDirectoryOnly).Length > 0
            || Directory.GetFiles(full, "Sprite*.idx", SearchOption.TopDirectoryOnly).Length > 0;
        if (!hasSpriteIdx)
            hints.Add("未偵測到 sprite*.idx（可選）：精靈預覽/搜尋可能不可用。");

        int mapCount = 0;
        if (hasMap)
        {
            try
            {
                mapCount = Directory.GetDirectories(mapDir)
                    .Count(d => Directory.EnumerateFiles(d, "*.s32").Any());
            }
            catch
            {
                // non-fatal
            }
        }

        if (missing.Count > 0)
        {
            string missingText = string.Join("、", missing);
            return ClientValidationResult.Failed(
                error: $"客戶端資料夾不完整（缺少：{missingText}）。",
                reason: $"路徑 {full} 未通過健康檢查。",
                suggestion: string.Join(" ", hints.DefaultIfEmpty("請選擇含 map\\ 與 Tile.idx 的客戶端根目錄。")),
                missing: missing.ToArray(),
                hints: hints.ToArray(),
                path: full,
                hasMap: hasMap,
                hasTileIdx: hasTileIdx,
                hasAnyIdx: hasAnyIdx,
                hasSpriteIdx: hasSpriteIdx,
                mapCount: mapCount,
                idxCount: idxFiles.Length);
        }

        return ClientValidationResult.Succeeded(
            path: full,
            hints: hints.ToArray(),
            hasMap: hasMap,
            hasTileIdx: hasTileIdx,
            hasAnyIdx: hasAnyIdx,
            hasSpriteIdx: hasSpriteIdx,
            mapCount: mapCount,
            idxCount: idxFiles.Length);
    }
}

public sealed class ClientValidationResult
{
    public bool Ok { get; init; }
    public string? Path { get; init; }
    public string[] Missing { get; init; } = Array.Empty<string>();
    public string[] Hints { get; init; } = Array.Empty<string>();
    public string? Error { get; init; }
    public string? Reason { get; init; }
    public string? Suggestion { get; init; }
    public bool HasMap { get; init; }
    public bool HasTileIdx { get; init; }
    public bool HasAnyIdx { get; init; }
    public bool HasSpriteIdx { get; init; }
    public int MapCount { get; init; }
    public int IdxCount { get; init; }

    public static ClientValidationResult Succeeded(
        string path,
        string[]? hints = null,
        bool hasMap = true,
        bool hasTileIdx = true,
        bool hasAnyIdx = true,
        bool hasSpriteIdx = false,
        int mapCount = 0,
        int idxCount = 0) => new()
    {
        Ok = true,
        Path = path,
        Hints = hints ?? Array.Empty<string>(),
        HasMap = hasMap,
        HasTileIdx = hasTileIdx,
        HasAnyIdx = hasAnyIdx,
        HasSpriteIdx = hasSpriteIdx,
        MapCount = mapCount,
        IdxCount = idxCount
    };

    public static ClientValidationResult Failed(
        string error,
        string reason,
        string suggestion,
        string[] missing,
        string[]? hints = null,
        string? path = null,
        bool hasMap = false,
        bool hasTileIdx = false,
        bool hasAnyIdx = false,
        bool hasSpriteIdx = false,
        int mapCount = 0,
        int idxCount = 0) => new()
    {
        Ok = false,
        Path = path,
        Error = error,
        Reason = reason,
        Suggestion = suggestion,
        Missing = missing,
        Hints = hints ?? Array.Empty<string>(),
        HasMap = hasMap,
        HasTileIdx = hasTileIdx,
        HasAnyIdx = hasAnyIdx,
        HasSpriteIdx = hasSpriteIdx,
        MapCount = mapCount,
        IdxCount = idxCount
    };

    /// <summary>
    /// Operator-facing multi-line text: 錯誤/原因/建議.
    /// </summary>
    public string FormatOperatorMessage()
    {
        if (Ok)
        {
            var lines = new List<string>
            {
                "狀態：通過",
                $"路徑：{Path}",
                $"map 子地圖數（含 .s32）：{MapCount}",
                $"idx 檔數：{IdxCount}",
                $"Tile.idx：{(HasTileIdx ? "有" : "無")}",
                $"sprite*.idx：{(HasSpriteIdx ? "有" : "無")}"
            };
            if (Hints.Length > 0)
            {
                lines.Add("提示：");
                foreach (var h in Hints)
                    lines.Add($"  - {h}");
            }
            return string.Join(Environment.NewLine, lines);
        }

        return string.Join(Environment.NewLine, new[]
        {
            $"錯誤：{Error}",
            $"原因：{Reason}",
            $"建議：{Suggestion}"
        });
    }
}
