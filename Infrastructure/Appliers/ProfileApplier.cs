using System.Runtime.Versioning;
using System.ServiceProcess;
using LianLiProfileWatcher.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LianLiProfileWatcher.Infrastructure.Appliers
{
    [SupportedOSPlatform("windows")]
    public class ProfileApplier : IProfileApplier
    {
        private readonly IConfigurationService _configService;
        private readonly ILogger<ProfileApplier> _logger;

        // Dernier profil réellement appliqué — évite les restarts inutiles
        private string _lastAppliedProfile = string.Empty;

        public ProfileApplier(IConfigurationService configService, ILogger<ProfileApplier> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public async Task ApplyAsync(string profileName, CancellationToken cancellationToken = default)
        {
            // ── GARDE : ne rien faire si le profil n'a pas changé ──────────────
            if (profileName == _lastAppliedProfile)
            {
                _logger.LogDebug("Profil '{Profile}' déjà actif, aucune action.", profileName);
                return;
            }

            var config = _configService.Config;
            string originPath = Path.Combine(config.BaseFolder, profileName);
            string destinationPath = config.Destination;

            _logger.LogInformation("ProfileApplier: application du profil « {Profile} »", profileName);

            if (!Directory.Exists(originPath))
            {
                _logger.LogWarning("Le dossier du profil '{Profile}' n'existe pas : {Path}", profileName, originPath);
                return;
            }

            try
            {
                // 1. Nettoyage des répertoires cibles
                DeleteIfExists(Path.Combine(destinationPath, "device"));
                DeleteIfExists(Path.Combine(destinationPath, "profile"));

                // 2. Copie des fichiers (opération I/O — sur thread pool via Task.Run)
                await Task.Run(() =>
                {
                    DirectoryCopy(Path.Combine(originPath, "device"),
                                  Path.Combine(destinationPath, "device"), overwrite: true);
                    DirectoryCopy(Path.Combine(originPath, "profile"),
                                  Path.Combine(destinationPath, "profile"), overwrite: true);
                }, cancellationToken);

                // 3. Redémarrage des services (async + interruptible)
                await RestartServicePairAsync(config.ServiceName, config.WatcherServiceName, cancellationToken);

                _lastAppliedProfile = profileName;
                _logger.LogInformation("Profil '{Profile}' appliqué avec succès.", profileName);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Application du profil '{Profile}' annulée.", profileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'application du profil '{Profile}'", profileName);
            }
        }

        #region Helpers

        private void DeleteIfExists(string path)
        {
            if (!Directory.Exists(path)) return;
            try
            {
                var di = new DirectoryInfo(path);
                foreach (var entry in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    try { entry.Attributes = FileAttributes.Normal; }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Impossible de réinitialiser les attributs pour {Entry}", entry.FullName);
                    }
                }
                Directory.Delete(path, recursive: true);
                _logger.LogDebug("Dossier supprimé : {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec de la suppression du dossier {Path}", path);
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
                string target = Path.Combine(destDir, file.Name);
                file.CopyTo(target, overwrite);
            }
            foreach (var subDir in di.GetDirectories())
                DirectoryCopy(subDir.FullName, Path.Combine(destDir, subDir.Name), overwrite);
        }

        private async Task RestartServicePairAsync(
            string serviceName,
            string watcherServiceName,
            CancellationToken cancellationToken)
        {
            ServiceController? watcher = null;
            ServiceController? main = null;

            try
            {
                // 1. Arrêter le watcher en premier
                if (!string.IsNullOrEmpty(watcherServiceName))
                {
                    try
                    {
                        watcher = new ServiceController(watcherServiceName);
                        var _ = watcher.Status;
                        if (watcher.Status == ServiceControllerStatus.Running)
                        {
                            _logger.LogDebug("Arrêt du watcher {Watcher}", watcherServiceName);
                            watcher.Stop();
                            await WaitForStatusAsync(watcher, ServiceControllerStatus.Stopped,
                                                     TimeSpan.FromSeconds(15), cancellationToken);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogDebug("Watcher '{Watcher}' absent, ignoré.", watcherServiceName);
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
                    await WaitForStatusAsync(main, ServiceControllerStatus.Stopped,
                                             TimeSpan.FromSeconds(30), cancellationToken);
                }

                // 3. Démarrer le service principal
                _logger.LogDebug("Démarrage du service {Service}", serviceName);
                main.Start();
                await WaitForStatusAsync(main, ServiceControllerStatus.Running,
                                         TimeSpan.FromSeconds(30), cancellationToken);
                _logger.LogInformation("Service {Service} démarré.", serviceName);

                // 4. Laisser L-Connect charger sa config — délai INTERRUPTIBLE
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                // 5. Redémarrer le watcher
                if (watcher != null)
                {
                    _logger.LogDebug("Redémarrage du watcher {Watcher}", watcherServiceName);
                    watcher.Start();
                    await WaitForStatusAsync(watcher, ServiceControllerStatus.Running,
                                             TimeSpan.FromSeconds(15), cancellationToken);
                    _logger.LogInformation("Watcher {Watcher} redémarré.", watcherServiceName);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service '{Service}' introuvable.", serviceName);
            }
            catch (OperationCanceledException) { throw; } // remonte au caller
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur restart {Service}/{Watcher}", serviceName, watcherServiceName);
            }
            finally
            {
                watcher?.Dispose();
                main?.Dispose();
            }
        }

        /// <summary>
        /// Équivalent async de ServiceController.WaitForStatus — poll toutes les 200ms.
        /// </summary>
        private static async Task WaitForStatusAsync(
            ServiceController svc,
            ServiceControllerStatus desired,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            while (true)
            {
                svc.Refresh();
                if (svc.Status == desired) return;
                await Task.Delay(200, cts.Token);
            }
        }

        #endregion
    }
}