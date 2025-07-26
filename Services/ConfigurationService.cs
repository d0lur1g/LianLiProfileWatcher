using System.Text.Json;
using LianLiProfileWatcher.Models;
using LianLiProfileWatcher.Application.Interfaces;

namespace LianLiProfileWatcher.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public AppProfileConfig Config { get; private set; }

        public ConfigurationService(string configFilePath)
        {
            if (!File.Exists(configFilePath))
                throw new FileNotFoundException("Le fichier de configuration est introuvable.", configFilePath);

            var json = File.ReadAllText(configFilePath);

            // Désérialisation case-insensitive pour mapper tes clés camelCase sur tes propriétés PascalCase
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            Config = JsonSerializer.Deserialize<AppProfileConfig>(json, options)
                     ?? throw new InvalidOperationException("Le fichier de configuration est invalide ou vide.");
        }
    }
}
