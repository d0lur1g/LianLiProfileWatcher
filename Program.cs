using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using LianLiProfileWatcher.Application.Interfaces;
using LianLiProfileWatcher.Infrastructure.Appliers;
using LianLiProfileWatcher.Services;

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

            try
            {
                Log.Information("Démarrage de l'agent LianLiProfileWatcher");
                CreateHostBuilder(args).Build().Run();
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                .UseSerilog()                   // remplace le logging par Serilog
                .UseConsoleLifetime()           // ne bloque pas le shutdown
                .ConfigureServices((ctx, services) =>
                {
                    // 1. Chemins des deux fichiers de config
                    var cfgDir = Path.Combine(AppContext.BaseDirectory, "Config");
                    var userCfg = Path.Combine(cfgDir, "appProfiles.json");
                    var exampleCfg = Path.Combine(cfgDir, "appProfiles.example.json");

                    // 2. Choix : si le fichier perso existe, on l'utilise, sinon l'exemple
                    var cfgPath = File.Exists(userCfg) ? userCfg : exampleCfg;

                    Log.Information("Chargement de la config depuis : {Path}", cfgPath);

                    // 3. Injection de la config via ton service existant
                    services.AddSingleton<IConfigurationService>(_ =>
                        new ConfigurationService(cfgPath));

                    // Reste des services
                    services.AddSingleton<IProfileApplier, ProfileApplier>();
                    services.AddSingleton<IForegroundProcessService, ForegroundProcessService>();
                    services.AddHostedService<Worker>();  // votre hook WinEvent
                });
    }
}
