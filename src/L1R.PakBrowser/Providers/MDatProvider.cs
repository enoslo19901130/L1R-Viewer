using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lin.Helper.Core.Dat;
using PakViewer.Localization;
using ResourceDatTools = PakViewer.Utility.DatTools;

namespace PakViewer.Providers
{
    /// <summary>
    /// M DAT 提供者 - 支援 ZIP-based 容器及 Lineage M Icon/Image 資源 DAT
    /// </summary>
    public class MDatProvider : IFileProvider
    {
        public static string AllSourcesOption => I18n.T("Filter.All");

        private readonly Dictionary<string, MDat> _containerFiles;
        private readonly Dictionary<string, ResourceDatTools.DatFile> _resourceFiles;
        private readonly List<FileEntry> _allFiles;
        private List<FileEntry> _filteredFiles;
        private string _currentSourceOption;
        private bool _disposed;

        /// <summary>
        /// 建立 MDat 提供者
        /// </summary>
        /// <param name="datPaths">M DAT 容器及 Icon/Image 資源 .dat 路徑 (一或多個)</param>
        /// <param name="password">ZIP 密碼 (null 表示無密碼)</param>
        public MDatProvider(string[] datPaths, string password = null)
        {
            if (datPaths == null || datPaths.Length == 0)
                throw new ArgumentException("At least one DAT path is required", nameof(datPaths));

            _containerFiles = new Dictionary<string, MDat>(StringComparer.OrdinalIgnoreCase);
            _resourceFiles = new Dictionary<string, ResourceDatTools.DatFile>(StringComparer.OrdinalIgnoreCase);
            _allFiles = new List<FileEntry>();

            int globalIndex = 0;
            foreach (var datPath in datPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(datPath))
                    continue;

                try
                {
                    var status = MDat.DetectStatus(datPath);
                    var sourceName = GetUniqueSourceName(datPath);

                    if (status != MDatStatus.Sealed)
                    {
                        var dat = new MDat(datPath, password);
                        _containerFiles[sourceName] = dat;

                        foreach (var entry in dat.Entries)
                        {
                            _allFiles.Add(new FileEntry
                            {
                                Index = globalIndex++,
                                FileName = entry.FileName,
                                FileSize = entry.UncompressedSize,
                                FilePath = entry.FileName,
                                SourceName = sourceName,
                                Source = this
                            });
                        }
                    }
                    else if (ResourceDatTools.IsDatFile(datPath))
                    {
                        var dat = new ResourceDatTools.DatFile(datPath);
                        dat.ParseEntries();
                        _resourceFiles[sourceName] = dat;

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
                }
                catch
                {
                    // 忽略無法開啟的 .dat 檔案
                }
            }

            if (SourceCount == 0)
                throw new InvalidDataException("No supported M DAT containers or Icon/Image DAT resources could be opened");

            // 預設選第一個 dat
            var defaultOption = GetSourceNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase).First();
            SetSourceOption(defaultOption);
        }

        private string GetUniqueSourceName(string datPath)
        {
            var baseName = Path.GetFileName(datPath);
            var sourceName = baseName;
            int suffix = 2;

            while (_containerFiles.ContainsKey(sourceName) || _resourceFiles.ContainsKey(sourceName))
                sourceName = $"{baseName} ({suffix++})";

            return sourceName;
        }

        private IEnumerable<string> GetSourceNames()
        {
            return _containerFiles.Keys.Concat(_resourceFiles.Keys);
        }

        public int ContainerCount => _containerFiles.Count;

        public int ResourceDatCount => _resourceFiles.Count;

        public int SourceCount => ContainerCount + ResourceDatCount;

        public int TotalCount => _allFiles.Count;

        public string Name
        {
            get
            {
                if (SourceCount == 1)
                    return GetSourceNames().First();
                return $"MDat ({SourceCount} files)";
            }
        }

        public int Count => _filteredFiles?.Count ?? _allFiles.Count;

        public IReadOnlyList<FileEntry> Files => (_filteredFiles ?? _allFiles).AsReadOnly();

        public byte[] Extract(int index)
        {
            var files = _filteredFiles ?? _allFiles;
            if (index < 0 || index >= files.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Extract(files[index]);
        }

        public byte[] Extract(FileEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (_containerFiles.TryGetValue(entry.SourceName, out var container))
            {
                var containerEntry = container.Entries.FirstOrDefault(e =>
                    e.FileName.Equals(entry.FilePath ?? entry.FileName, StringComparison.Ordinal));
                if (containerEntry == null)
                    throw new InvalidOperationException($"Entry not found in DAT: {entry.FileName}");

                return container.Extract(containerEntry);
            }

            if (_resourceFiles.TryGetValue(entry.SourceName, out var resource))
            {
                var resourceEntry = resource.Entries.FirstOrDefault(e =>
                    e.Path == entry.FilePath && e.Offset == entry.Offset);
                if (resourceEntry == null)
                    throw new InvalidOperationException($"Entry not found in DAT: {entry.FileName}");

                return resource.ExtractFile(resourceEntry);
            }

            throw new InvalidOperationException($"DAT file not found: {entry.SourceName}");
        }

        public IEnumerable<string> GetExtensions()
        {
            var files = _filteredFiles ?? _allFiles;
            return files
                .Select(f => Path.GetExtension(f.FileName)?.ToLowerInvariant() ?? "")
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Distinct()
                .OrderBy(ext => ext);
        }

        public IEnumerable<string> GetSourceOptions()
        {
            if (SourceCount > 1)
                yield return AllSourcesOption;

            foreach (var name in GetSourceNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                yield return name;
        }

        public void SetSourceOption(string option)
        {
            _currentSourceOption = option;

            if (option == AllSourcesOption || string.IsNullOrEmpty(option))
            {
                _filteredFiles = null;
            }
            else
            {
                _filteredFiles = _allFiles
                    .Where(f => f.SourceName.Equals(option, StringComparison.OrdinalIgnoreCase))
                    .Select((f, i) => new FileEntry
                    {
                        Index = i,
                        FileName = f.FileName,
                        FileSize = f.FileSize,
                        Offset = f.Offset,
                        FilePath = f.FilePath,
                        SourceName = f.SourceName,
                        Source = f.Source
                    })
                    .ToList();
            }
        }

        public string CurrentSourceOption => _currentSourceOption ?? AllSourcesOption;

        public bool HasMultipleSourceOptions => SourceCount > 1;

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var dat in _containerFiles.Values)
                {
                    dat?.Dispose();
                }
                _containerFiles.Clear();
                _resourceFiles.Clear();
                _disposed = true;
            }
        }
    }
}
