using System.Runtime.Versioning;
using System.ServiceProcess;
using LianLiProfileWatcher.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LianLiProfileWatcher.Infrastructure.Appliers
{
    /// <summary>
    /// Implémentation de IProfileApplier : applique un profil en nettoyant le dossier de destination,
    /// copiant récursivement les répertoires "device" et "profile" depuis BaseFolder\profileName,
    /// puis redémarrant le service LConnectService.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ProfileApplier : IProfileApplier
    {

        private readonly IConfigurationService _configService;
        private readonly ILogger<ProfileApplier> _logger;

        public ProfileApplier(IConfigurationService configService, ILogger<ProfileApplier> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public void Apply(string profileName)
        {
            _logger.LogInformation("ProfileApplier: nettoyage et copie pour « {Profile} »", profileName);

            var config = _configService.Config;
            string originPath = Path.Combine(config.BaseFolder, profileName);
            string destinationPath = config.Destination;

            _logger.LogInformation("Application du profil {Profile}", profileName);

            if (!Directory.Exists(originPath))
            {
                _logger.LogWarning("Le dossier du profil '{Profile}' n'existe pas : {Path}", profileName, originPath);
                return;
            }

            try
            {
                // 1. Nettoyage des répertoires cibles
                _logger.LogDebug("  Suppression dossier {Dir}", Path.Combine(destinationPath, "device"));
                DeleteIfExists(Path.Combine(destinationPath, "device"));
                DeleteIfExists(Path.Combine(destinationPath, "profile"));

                // 2. Copie des fichiers
                _logger.LogDebug("  Copie device → {Target}", Path.Combine(destinationPath, "device"));
                DirectoryCopy(Path.Combine(originPath, "device"), Path.Combine(destinationPath, "device"), overwrite: true);

                _logger.LogDebug("  Copie profile → {Target}", Path.Combine(destinationPath, "profile"));
                DirectoryCopy(Path.Combine(originPath, "profile"), Path.Combine(destinationPath, "profile"), overwrite: true);

                // 3. Redémarrage du service LConnectService
                _logger.LogDebug("  Redémarrage du service LConnectService");
                RestartServicePair(config.ServiceName, config.WatcherServiceName);

                _logger.LogInformation("Profil '{Profile}' appliqué avec succès.", profileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'application du profil '{Profile}'", profileName);
            }
        }

        #region Helpers

        private void DeleteIfExists(string path)
        {
            if (!Directory.Exists(path))
                return;

            try
            {
                // 1. Parcours récursif pour normaliser tous les attributs
                var di = new DirectoryInfo(path);
                foreach (var entry in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        entry.Attributes = FileAttributes.Normal;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Impossible de réinitialiser les attributs pour {Entry}", entry.FullName);
                    }
                }

                // 2. Suppression récursive
                Directory.Delete(path, recursive: true);
                _logger.LogDebug("Dossier supprimé : {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Échec de la suppression du dossier {Path}, on passe à la suite", path);
            }
        }


        private void DirectoryCopy(string sourceDir, string destDir, bool overwrite)
        {
            var di = new DirectoryInfo(sourceDir);
            if (!di.Exists)
            {
                _logger.LogWarning("Source introuvable pour la copie : {Source}", sourceDir);
                return;
            }

            Directory.CreateDirectory(destDir);
            foreach (var file in di.GetFiles())
            {
                string targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, overwrite);
                _logger.LogDebug("Copie du fichier {File} vers {Target}", file.FullName, targetFilePath);
            }
            foreach (var subDir in di.GetDirectories())
            {
                DirectoryCopy(subDir.FullName, Path.Combine(destDir, subDir.Name), overwrite);
            }
        }

        private void RestartServicePair(string serviceName, string watcherServiceName)
        {
            ServiceController? watcher = null;
            ServiceController? main = null;

            try
            {
                // 1. Arrêter le watcher EN PREMIER pour éviter qu'il interfère
                if (!string.IsNullOrEmpty(watcherServiceName))
                {
                    try
                    {
                        watcher = new ServiceController(watcherServiceName);
                        var _ = watcher.Status; // lève exception si inexistant — ignoré volontairement
                        if (watcher.Status == ServiceControllerStatus.Running)
                        {
                            _logger.LogDebug("Arrêt du watcher {Watcher}", watcherServiceName);
                            watcher.Stop();
                            watcher.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogDebug("Watcher service '{Watcher}' absent, ignoré", watcherServiceName);
                        watcher?.Dispose();
                        watcher = null;
                    }
                }

                // 2. Arrêter le service principal
                main = new ServiceController(serviceName);
                if (main.CanStop)
                {
                    _logger.LogDebug("Arrêt du service {Service}", serviceName);
                    main.Stop();
                    main.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }

                // 3. Redémarrer le service principal
                _logger.LogDebug("Démarrage du service {Service}", serviceName);
                main.Start();
                main.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                _logger.LogInformation("Service {Service} redémarré avec succès", serviceName);

                // 4. Redémarrer le watcher EN DERNIER
                if (watcher != null)
                {
                    _logger.LogDebug("Redémarrage du watcher {Watcher}", watcherServiceName);
                    watcher.Start();
                    watcher.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                    _logger.LogInformation("Watcher {Watcher} redémarré", watcherServiceName);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex,
                    "Service '{Service}' introuvable. Vérifiez 'serviceName' dans appProfiles.json.",
                    serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du redémarrage des services {Service}/{Watcher}",
                    serviceName, watcherServiceName);
            }
            finally
            {
                watcher?.Dispose();
                main?.Dispose();
            }
        }

        #endregion
    }
}
