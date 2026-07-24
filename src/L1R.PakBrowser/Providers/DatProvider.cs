using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PakViewer.Localization;
using PakViewer.Utility;

namespace PakViewer.Providers
{
    /// <summary>
    /// DAT 檔案提供者 - Lineage M Icon/Image DAT 格式，支援單一或多個來源
    /// </summary>
    public class DatProvider : IFileProvider
    {
        public static string AllSourcesOption => I18n.T("Filter.All");

        private readonly Dictionary<string, DatTools.DatFile> _datFiles;
        private readonly List<FileEntry> _allFiles;
        private List<FileEntry> _filteredFiles;
        private string _currentSourceOption;
        private bool _disposed;

        public DatProvider(string datPath)
            : this(new[] { datPath })
        {
        }

        public DatProvider(string[] datPaths)
        {
            if (datPaths == null || datPaths.Length == 0)
                throw new ArgumentException("At least one DAT path is required", nameof(datPaths));

            _datFiles = new Dictionary<string, DatTools.DatFile>(StringComparer.OrdinalIgnoreCase);
            _allFiles = new List<FileEntry>();

            int globalIndex = 0;
            foreach (var datPath in datPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(datPath) || !DatTools.IsDatFile(datPath))
                    continue;

                try
                {
                    var dat = new DatTools.DatFile(datPath);
                    dat.ParseEntries();

                    var sourceName = GetUniqueSourceName(datPath);
                    _datFiles[sourceName] = dat;

                    foreach (var entry in dat.Entries)
                    {
                        _allFiles.Add(new FileEntry
                        {
                            Index = globalIndex++,
                            FileName = entry.Path,
                            FileSize = entry.Size,
                            Offset = entry.Offset,
                            FilePath = entry.Path,
                            SourceName = sourceName,
                            Source = this
                        });
                    }
                }
                catch
                {
                    // 其他有效 DAT 仍可繼續載入；全部失敗時會在下方回報。
                }
            }

            if (_datFiles.Count == 0)
                throw new InvalidDataException("No valid Lineage M Icon/Image DAT files could be opened");

            // 多檔時先顯示第一個來源，避免一次建立過大的 UI 清單；仍可切換到「全部」。
            SetSourceOption(_datFiles.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).First());
        }

        private string GetUniqueSourceName(string datPath)
        {
            var baseName = Path.GetFileName(datPath);
            var sourceName = baseName;
            int suffix = 2;

            while (_datFiles.ContainsKey(sourceName))
                sourceName = $"{baseName} ({suffix++})";

            return sourceName;
        }

        /// <summary>
        /// 單一來源時取得內部 DatFile 實例 (用於既有進階操作)
        /// </summary>
        public DatTools.DatFile DatFile => _datFiles.Count == 1 ? _datFiles.Values.First() : null;

        public int SourceCount => _datFiles.Count;

        public int TotalCount => _allFiles.Count;

        public string Name => _datFiles.Count == 1
            ? _datFiles.Keys.First()
            : $"DAT ({_datFiles.Count} files)";

        public int Count => _filteredFiles?.Count ?? _allFiles.Count;

        public IReadOnlyList<FileEntry> Files => (_filteredFiles ?? _allFiles).AsReadOnly();

        public byte[] Extract(int index)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DatProvider));
            var files = _filteredFiles ?? _allFiles;
            if (index < 0 || index >= files.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Extract(files[index]);
        }

        public byte[] Extract(FileEntry entry)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DatProvider));
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (!_datFiles.TryGetValue(entry.SourceName, out var dat))
                throw new InvalidOperationException($"DAT file not found: {entry.SourceName}");

            var datEntry = dat.Entries.FirstOrDefault(e =>
                e.Path == entry.FilePath && e.Offset == entry.Offset);

            if (datEntry == null)
                throw new InvalidOperationException($"File not found in DAT: {entry.FileName}");

            return dat.ExtractFile(datEntry);
        }

        public IEnumerable<string> GetExtensions()
        {
            return (_filteredFiles ?? _allFiles)
                .Select(f => Path.GetExtension(f.FileName)?.ToLowerInvariant() ?? "")
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Distinct()
                .OrderBy(ext => ext);
        }

        public IEnumerable<string> GetSourceOptions()
        {
            if (_datFiles.Count > 1)
                yield return AllSourcesOption;

            foreach (var name in _datFiles.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                yield return name;
        }

        public void SetSourceOption(string option)
        {
            _currentSourceOption = option;

            if (option == AllSourcesOption || string.IsNullOrEmpty(option))
            {
                _filteredFiles = null;
                return;
            }

            _filteredFiles = _allFiles
                .Where(file => file.SourceName.Equals(option, StringComparison.OrdinalIgnoreCase))
                .Select((file, index) => new FileEntry
                {
                    Index = index,
                    FileName = file.FileName,
                    FileSize = file.FileSize,
                    Offset = file.Offset,
                    FilePath = file.FilePath,
                    SourceName = file.SourceName,
                    Source = this
                })
                .ToList();
        }

        public string CurrentSourceOption => _currentSourceOption ?? AllSourcesOption;

        public bool HasMultipleSourceOptions => _datFiles.Count > 1;

        public void Dispose()
        {
            if (!_disposed)
            {
                // DatFile 每次提取時才短暫開啟 stream，沒有持有中的資源。
                _datFiles.Clear();
                _disposed = true;
            }
        }
    }
}
