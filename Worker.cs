using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using LianLiProfileWatcher.Application.Interfaces;

namespace LianLiProfileWatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfigurationService _configService;
        private readonly IProfileApplier _profileApplier;
        private WinEventDelegate _winEventDelegate = null!;

        // Délégué WinEvent
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        // Constantes du hook
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        public Worker(
            ILogger<Worker> logger,
            IConfigurationService configService,
            IProfileApplier profileApplier)
        {
            _logger = logger;
            _configService = configService;
            _profileApplier = profileApplier;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = _configService.Config;

            _logger.LogInformation("Config chargée : BaseFolder={Base}, Default={Def}, Profiles=[{Keys}]", config.BaseFolder, config.Default, string.Join(',', config.Profiles.Keys));

            if (config is null)
            {
                _logger.LogError("Configuration introuvable.");
                return Task.CompletedTask;
            }

            // Préparer le délégué pour ne pas qu'il soit collecté
            _winEventDelegate = WinEventProc;

            // Créer un thread STA pour installer le hook et la boucle de messages
            Thread hookThread = new(() =>
            {
                IntPtr hook = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND,
                    EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0, 0,
                    WINEVENT_OUTOFCONTEXT);

                if (hook == IntPtr.Zero)
                {
                    _logger.LogError("Échec de l’installation du hook WinEvent.");
                    return;
                }

                _logger.LogInformation("Hook WinEvent installé.");

                // Boucle de messages Windows
                NativeMessage msg;
                while (!stoppingToken.IsCancellationRequested
                       && GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                // Nettoyage
                UnhookWinEvent(hook);
                _logger.LogInformation("Hook WinEvent désinstallé et thread arrêté.");
            });

            hookThread.SetApartmentState(ApartmentState.STA);
            hookThread.IsBackground = true;
            hookThread.Start();

            return Task.CompletedTask;
        }

        [SupportedOSPlatform("windows")]
        private void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero)
                return;

            // Récupérer le PID
            GetWindowThreadProcessId(hwnd, out uint pid);
            string processName;
            try
            {
                processName = Process.GetProcessById((int)pid)
                                     .ProcessName
                                     .ToLowerInvariant();
            }
            catch
            {
                _logger.LogWarning("Processus introuvable pour HWND={Handle}", hwnd);
                return;
            }

            _logger.LogInformation("Fenêtre active détectée : {Process}", processName);

            // Résolution du profil
            var cfg = _configService.Config;
            string profile = cfg.Profiles.TryGetValue(processName, out var p)
                                ? p
                                : cfg.Default;
            _logger.LogInformation("→ Application du profil « {Profile} »", profile);

            // Application du profil
            _profileApplier.Apply(profile);
        }

        #region PInvoke et structures

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(
            out NativeMessage lpMsg,
            IntPtr hWnd,
            uint wMsgFilterMin,
            uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref NativeMessage lpMsg);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint lpdwProcessId);

        #endregion
    }
}
