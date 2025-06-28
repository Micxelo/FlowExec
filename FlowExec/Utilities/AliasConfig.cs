using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace FlowExec
{
    public class AliasConfigService : IDisposable
    {
        private const string ConfigPath = "aliases.json";
        private readonly FileSystemWatcher _watcher;
        private Dictionary<string, string> _aliases = new();
        private readonly object _lock = new object();
        private bool _disposed;

        // 别名更新事件
        public event EventHandler<AliasUpdatedEventArgs>? AliasesUpdated;

        public AliasConfigService()
        {
            // 初始加载配置
            LoadConfig();

            // 设置文件监控
            var configDir = Path.GetDirectoryName(Path.GetFullPath(ConfigPath))
                          ?? Environment.CurrentDirectory;

            _watcher = new FileSystemWatcher(configDir)
            {
                Filter = Path.GetFileName(ConfigPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnConfigFileChanged;
            _watcher.Created += OnConfigFileChanged;
            _watcher.Deleted += OnConfigFileChanged;
            _watcher.Renamed += OnConfigFileRenamed;
        }

        private void LoadConfig()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(ConfigPath))
                    {
                        var json = File.ReadAllText(ConfigPath);
                        var config = JsonSerializer.Deserialize<AliasConfig>(json)
                                  ?? new AliasConfig();

                        _aliases = config.Aliases;
                    }
                    else
                    {
                        _aliases = new Dictionary<string, string>();
                    }

                    // 触发初始加载事件
                    AliasesUpdated?.Invoke(this, new AliasUpdatedEventArgs(
                        AliasUpdateType.InitialLoad,
                        null,
                        _aliases
                    ));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Load Config Fatal: {0}", ex.Message));
                    _aliases = new Dictionary<string, string>();
                }
            }
        }

        private void OnConfigFileRenamed(object sender, RenamedEventArgs e)
        {
            // 配置文件被重命名
            if (e.FullPath == Path.GetFullPath(ConfigPath))
            {
                Thread.Sleep(100); // 等待文件操作完成
                LoadConfig();
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            // 防止多次触发
            _watcher.EnableRaisingEvents = false;

            try
            {
                // 等待文件释放
                Thread.Sleep(100);
                LoadConfig();
            }
            finally
            {
                _watcher.EnableRaisingEvents = true;
            }
        }

        public void SaveConfig()
        {
            lock (_lock)
            {
                try
                {
                    var config = new AliasConfig { Aliases = _aliases };
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(config, options);
                    File.WriteAllText(ConfigPath, json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Save Config Fatal: {0}", ex.Message));
                }
            }
        }

        public void AddAlias(string name, string path)
        {
            lock (_lock)
            {
                _aliases[name] = path;
                SaveConfig();

                // 触发添加事件
                AliasesUpdated?.Invoke(this, new AliasUpdatedEventArgs(
                    AliasUpdateType.Added,
                    name,
                    new Dictionary<string, string> { [name] = path }
                ));
            }
        }

        public void UpdateAlias(string name, string newPath)
        {
            lock (_lock)
            {
                if (_aliases.ContainsKey(name))
                {
                    var oldPath = _aliases[name];
                    _aliases[name] = newPath;
                    SaveConfig();

                    // 触发更新事件
                    AliasesUpdated?.Invoke(this, new AliasUpdatedEventArgs(
                        AliasUpdateType.Updated,
                        name,
                        new Dictionary<string, string> { [name] = newPath }
                    ));
                }
            }
        }

        public void RemoveAlias(string name)
        {
            lock (_lock)
            {
                if (_aliases.Remove(name))
                {
                    SaveConfig();

                    // 触发删除事件
                    AliasesUpdated?.Invoke(this, new AliasUpdatedEventArgs(
                        AliasUpdateType.Removed,
                        name,
                        null
                    ));
                }
            }
        }

        public string? ResolveAlias(string name)
        {
            lock (_lock)
            {
                return _aliases.TryGetValue(name, out var path) ? path : null;
            }
        }

        public Dictionary<string, string> GetAllAliases()
        {
            lock (_lock)
            {
                return new Dictionary<string, string>(_aliases);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _watcher?.Dispose();
                _disposed = true;
            }
        }
    }

    public class AliasConfig
    {
        public Dictionary<string, string> Aliases { get; set; } = new();
    }

    public enum AliasUpdateType
    {
        InitialLoad,
        Added,
        Updated,
        Removed
    }

    public class AliasUpdatedEventArgs : EventArgs
    {
        public AliasUpdateType UpdateType { get; }
        public string? AliasName { get; }
        public Dictionary<string, string>? UpdatedAliases { get; }

        public AliasUpdatedEventArgs(
            AliasUpdateType updateType,
            string? aliasName,
            Dictionary<string, string>? updatedAliases)
        {
            UpdateType = updateType;
            AliasName = aliasName;
            UpdatedAliases = updatedAliases;
        }
    }
}
