using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using LianLiProfileWatcher.Application.Interfaces;
using LianLiProfileWatcher.Infrastructure.Appliers;
using LianLiProfileWatcher.Services;
using LianLiProfileWatcher.Models;

namespace LianLiProfileWatcher
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // 1) Préparer le fichier de log dans %LOCALAPPDATA%
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LianLiProfileWatcher",
                "Logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "agent.log");

            // 2) Configurer Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true,
                    restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();

            // ❶ Déterminer le chemin du fichier de config
            var configPath = ResolveConfigPath(args);
            Log.Information("Config path resolved to: {Path}", configPath);

            try
            {
                Log.Information("Démarrage de l'agent LianLiProfileWatcher");
                // ❷ Construire et lancer l’hôte
                CreateHostBuilder(args, configPath).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Échec inattendu de l'agent");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, string configPath) =>
            Host
                .CreateDefaultBuilder(args)
                .UseSerilog()
                .UseWindowsService() // Pour exécuter en tant que service Windows
                                     //.UseConsoleLifetime() // Optionnellement pour debug
                .ConfigureServices((ctx, services) =>
                {
                    // Binding POCO + injection
                    services.AddSingleton<IProfileApplier, ProfileApplier>();
                    services.AddSingleton<IForegroundProcessService, ForegroundProcessService>();
                    services.AddSingleton<IConfigurationService>(_ =>
                        new ConfigurationService(configPath));
                    services.AddHostedService<Worker>();
                });

        // On construit la liste des fichiers à charger, dans l’ordre :
        // CLI → path_config_perso → Variable d'ENV → LocalAppData → example (obligatoire)
        static string ResolveConfigPath(string[] args)
        {
            // 1) CLI --config
            var cliIndex = Array.IndexOf(args, "--config");
            if (cliIndex >= 0 && cliIndex < args.Length - 1)
                return args[cliIndex + 1];

            // 2) Env var
            var env = Environment.GetEnvironmentVariable("LIANLI_CONFIG_PATH");
            if (!string.IsNullOrEmpty(env))
                return env;

            // 3) LocalAppData
            var localDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LianLiProfileWatcher",
                "Config");
            var localFile = Path.Combine(localDir, "appProfiles.json");
            if (File.Exists(localFile))
                return localFile;

            // 4) Template fallback (toujours présent dans publish/Config)
            return Path.Combine(AppContext.BaseDirectory, "Config", "appProfiles.example.json");
        }

    }
}