using System.Text.Json;
using System.Text.Json.Serialization;

namespace L1R.Shared;

/// <summary>
/// Persisted operator settings under %AppData%\L1R-Viewer\settings.json.
/// </summary>
public sealed class AppSettings
{
    public const int SchemaVersionCurrent = 1;
    public const int MaxRecentClients = 8;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = SchemaVersionCurrent;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "zh-TW";

    [JsonPropertyName("recentClients")]
    public List<RecentClientEntry> RecentClients { get; set; } = new();

    [JsonPropertyName("lastClientPath")]
    public string? LastClientPath { get; set; }

    [JsonPropertyName("defaultOutputDir")]
    public string? DefaultOutputDir { get; set; }

    [JsonPropertyName("map")]
    public MapSettings Map { get; set; } = new();

    [JsonPropertyName("ui")]
    public UiSettings Ui { get; set; } = new();

    public static string GetDefaultSettingsDirectory()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "L1R-Viewer");
    }

    public static string GetDefaultSettingsPath()
        => Path.Combine(GetDefaultSettingsDirectory(), "settings.json");

    public static string GetDefaultOutputDirectory()
    {
        string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, "L1R-Viewer", "exports");
    }

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            SchemaVersion = SchemaVersionCurrent,
            Language = "zh-TW",
            DefaultOutputDir = GetDefaultOutputDirectory(),
            Map = new MapSettings(),
            Ui = new UiSettings()
        };
    }

    public static AppSettings Load(string? settingsPath = null)
    {
        string path = settingsPath ?? GetDefaultSettingsPath();
        if (!File.Exists(path))
            return CreateDefault();

        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions());
            if (loaded == null)
                return CreateDefault();

            if (string.IsNullOrWhiteSpace(loaded.DefaultOutputDir))
                loaded.DefaultOutputDir = GetDefaultOutputDirectory();
            loaded.RecentClients ??= new List<RecentClientEntry>();
            loaded.Map ??= new MapSettings();
            loaded.Ui ??= new UiSettings();
            // Cap list even if file was hand-edited
            if (loaded.RecentClients.Count > MaxRecentClients)
                loaded.RecentClients = loaded.RecentClients.Take(MaxRecentClients).ToList();
            return loaded;
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save(string? settingsPath = null)
    {
        string path = settingsPath ?? GetDefaultSettingsPath();
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        SchemaVersion = SchemaVersionCurrent;
        if (RecentClients.Count > MaxRecentClients)
            RecentClients = RecentClients.Take(MaxRecentClients).ToList();

        string json = JsonSerializer.Serialize(this, JsonOptionsWrite());
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Record a successfully validated client path as last + recent.
    /// </summary>
    public void RememberClient(string clientPath)
    {
        string full = Path.GetFullPath(clientPath);
        LastClientPath = full;
        RecentClients.RemoveAll(r =>
            string.Equals(r.Path, full, StringComparison.OrdinalIgnoreCase));
        RecentClients.Insert(0, new RecentClientEntry
        {
            Path = full,
            LastOpenedUtc = DateTime.UtcNow.ToString("o")
        });
        if (RecentClients.Count > MaxRecentClients)
            RecentClients = RecentClients.Take(MaxRecentClients).ToList();
    }

    static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static JsonSerializerOptions JsonOptionsWrite() => new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

public sealed class RecentClientEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("lastOpenedUtc")]
    public string? LastOpenedUtc { get; set; }
}

public sealed class MapSettings
{
    [JsonPropertyName("defaultMaxSize")]
    public int DefaultMaxSize { get; set; } = 2048;

    [JsonPropertyName("defaultShowLayer8")]
    public bool DefaultShowLayer8 { get; set; } = true;
}

public sealed class UiSettings
{
    [JsonPropertyName("enableEdit")]
    public bool EnableEdit { get; set; }

    [JsonPropertyName("confirmDangerousActions")]
    public bool ConfirmDangerousActions { get; set; } = true;
}
