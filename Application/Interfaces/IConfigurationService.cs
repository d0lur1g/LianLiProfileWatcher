using LianLiProfileWatcher.Models;

namespace LianLiProfileWatcher.Application.Interfaces
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Config courante — toujours à jour après un rechargement.
        /// </summary>
        AppProfileConfig Config { get; }

        /// <summary>
        /// Déclenché à chaque rechargement réussi du fichier de config.
        /// </summary>
        event Action<AppProfileConfig> ConfigReloaded;
    }
}