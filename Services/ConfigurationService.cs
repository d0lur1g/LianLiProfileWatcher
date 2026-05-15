using System.Text.Json;
using LianLiProfileWatcher.Models;
using LianLiProfileWatcher.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LianLiProfileWatcher.Services
{
    public class ConfigurationService : IConfigurationService, IDisposable
    {
        private readonly string _configFilePath;
        private readonly ILogger<ConfigurationService> _logger;
        private readonly FileSystemWatcher _watcher;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly object _lock = new();

        // Timer de debounce — FileSystemWatcher peut déclencher plusieurs événements
        // pour une seule sauvegarde (ex : éditeurs qui écrivent en deux passes)
        private Timer? _debounceTimer;
        private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(500);

        public AppProfileConfig Config { get; private set; }
        public event Action<AppProfileConfig>? ConfigReloaded;

        public ConfigurationService(string configFilePath, ILogger<ConfigurationService> logger)
        {
            _configFilePath = configFilePath;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Chargement initial
            Config = LoadFromDisk()
                     ?? throw new InvalidOperationException("Le fichier de configuration est invalide ou vide.");

            // Surveillance du fichier
            var directory = Path.GetDirectoryName(configFilePath)!;
            var fileName = Path.GetFileName(configFilePath);

            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged; // cas d'un remplacement de fichier
            _logger.LogInformation("Hot-reload activé sur : {Path}", configFilePath);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce : on reporte le rechargement de 500ms après le dernier événement
            lock (_lock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(_ => Reload(), null, DebounceDelay, Timeout.InfiniteTimeSpan);
            }
        }

        private void Reload()
        {
            var newConfig = LoadFromDisk();
            if (newConfig is null) return;

            lock (_lock)
            {
                Config = newConfig;
            }

            _logger.LogInformation("Configuration rechargée depuis {Path}", _configFilePath);
            ConfigReloaded?.Invoke(newConfig);
        }

        private AppProfileConfig? LoadFromDisk()
        {
            // Retry court : l'éditeur peut encore avoir le fichier verrouillé
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    if (!File.Exists(_configFilePath))
                    {
                        _logger.LogWarning("Fichier de config introuvable : {Path}", _configFilePath);
                        return null;
                    }

                    var json = File.ReadAllText(_configFilePath);
                    return JsonSerializer.Deserialize<AppProfileConfig>(json, _jsonOptions);
                }
                catch (IOException) when (attempt < 3)
                {
                    Thread.Sleep(200 * attempt); // attendre que le fichier soit libéré
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du chargement de la config (tentative {N})", attempt);
                    return null;
                }
            }
            return null;
        }

        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            lock (_lock) { _debounceTimer?.Dispose(); }
        }
    }
}