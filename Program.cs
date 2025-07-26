using LianLiProfileWatcher;
using LianLiProfileWatcher.Services;
using LianLiProfileWatcher.Application.Interfaces;
using LianLiProfileWatcher.Infrastructure.Appliers;

var host = Host.CreateDefaultBuilder(args)
    //.UseWindowsService()    // ou .UseConsoleLifetime() si tu veux debug en console
    .ConfigureServices((context, services) =>
    {
        // Chargement de la config JSON
        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "appProfiles.json");
        services.AddSingleton<IConfigurationService>(_ => new ConfigurationService(configPath));

        // Injection du ProfileApplier
        services.AddSingleton<IProfileApplier, ProfileApplier>();

        // Enregistrement du Worker hook Win32
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();
