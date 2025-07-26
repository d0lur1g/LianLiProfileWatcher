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
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    // On construit la liste des fichiers à charger, dans l’ordre :
                    // CLI → path_config_perso → Variable d'ENV → LocalAppData → example (obligatoire)
                    // 1) Support CLI --config
                    var switchMappings = new Dictionary<string, string>
                    {
                        { "--config", "ConfigPath" }
                    };
                    builder.AddCommandLine(args, switchMappings);

                    // 2) Si --config a été passé
                    var cmdPath = ctx.Configuration["ConfigPath"];
                    if (!string.IsNullOrEmpty(cmdPath))
                    {
                        builder.AddJsonFile(cmdPath!, optional: false, reloadOnChange: true);
                    }

                    // 3) Ou si variable d’env LIANLI_CONFIG_PATH
                    var envPath = Environment.GetEnvironmentVariable("LIANLI_CONFIG_PATH");
                    if (!string.IsNullOrEmpty(envPath))
                    {
                        builder.AddJsonFile(envPath!, optional: true, reloadOnChange: true);
                    }

                    // 4) Sinon on regarde dans LocalAppData
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)!;
                    var userCfg = Path.Combine(localAppData,
                                                    "LianLiProfileWatcher",
                                                    "Config",
                                                    "appProfiles.json");
                    builder.AddJsonFile(userCfg, optional: true, reloadOnChange: true);

                    // 5) Enfin, le template embarqué
                    var exampleCfg = Path.Combine(AppContext.BaseDirectory,
                                                 "Config",
                                                 "appProfiles.example.json");
                    builder.AddJsonFile(exampleCfg, optional: false, reloadOnChange: false);
                })
                .UseSerilog()
                .UseConsoleLifetime()
                .ConfigureServices((ctx, services) =>
                {
                    // Binding POCO + injection
                    services.Configure<AppProfileConfig>(ctx.Configuration);
                    services.AddSingleton<IProfileApplier, ProfileApplier>();
                    services.AddSingleton<IForegroundProcessService, ForegroundProcessService>();
                    services.AddHostedService<Worker>();
                });

    }
}
