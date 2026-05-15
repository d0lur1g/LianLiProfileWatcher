using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LianLiProfileWatcher.Application.Interfaces;

namespace LianLiProfileWatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfigurationService _configService;
        private readonly IProfileApplier _profileApplier;

        // Délai de debounce : on attend que le focus se stabilise avant d'agir
        private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(2000);

        // Channel non-borné : le thread STA écrit, le worker async lit
        private readonly Channel<string> _processChannel =
            Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

        private WinEventDelegate _winEventDelegate = null!;

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

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
            var cfg = _configService.Config;
            _logger.LogInformation(
                "Config chargée : BaseFolder={Base}, Default={Default}, Profiles=[{Keys}]",
                cfg.BaseFolder, cfg.Default, string.Join(',', cfg.Profiles.Keys));

            // ── Thread STA : uniquement le hook WinEvent et la boucle de messages ──
            _winEventDelegate = WinEventProc;
            var staThread = new Thread(() =>
            {
                IntPtr hook = SetWinEventHook(
                    EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

                if (hook == IntPtr.Zero)
                {
                    _logger.LogError("Échec de l'installation du hook WinEvent");
                    _processChannel.Writer.Complete();
                    return;
                }

                _logger.LogInformation("Hook WinEvent installé");

                NativeMessage msg;
                while (!stoppingToken.IsCancellationRequested
                       && GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                UnhookWinEvent(hook);
                _processChannel.Writer.Complete();
                _logger.LogInformation("Hook WinEvent désinstallé");
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();

            // ── Pipeline async : consomme le channel avec debounce ──
            return ProcessChannelAsync(stoppingToken);
        }

        /// <summary>
        /// Consomme les noms de processus depuis le channel.
        /// Applique un debounce : seul le dernier événement dans la fenêtre de 2000ms est traité.
        /// </summary>
        private async Task ProcessChannelAsync(CancellationToken stoppingToken)
        {
            CancellationTokenSource? debounceCts = null;

            await foreach (var processName in _processChannel.Reader.ReadAllAsync(stoppingToken))
            {
                // Annuler l'éventuel Apply en cours ou en attente
                debounceCts?.Cancel();
                debounceCts?.Dispose();
                debounceCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                var localCts = debounceCts;
                var localName = processName;

                // Lancer la tâche debouncée sans l'awaiter ici,
                // pour continuer à vider le channel sans bloquer
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(DebounceDelay, localCts.Token);

                        // Résoudre le profil
                        var profile = _configService.Config.Profiles
                            .TryGetValue(localName, out var p) ? p
                            : _configService.Config.Default;

                        _logger.LogInformation("Focus stable sur '{Process}' → profil « {Profile} »",
                            localName, profile);

                        await _profileApplier.ApplyAsync(profile, localCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("Debounce annulé pour '{Process}' (focus changé)", localName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement du profil pour '{Process}'", localName);
                    }
                }, stoppingToken);
            }

            debounceCts?.Dispose();
        }

        /// <summary>
        /// Callback WinEvent — DOIT être ultra-rapide, ne fait qu'écrire dans le channel.
        /// </summary>
        [SupportedOSPlatform("windows")]
        private void WinEventProc(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                var processName = Process
                    .GetProcessById((int)pid)
                    .ProcessName
                    .ToLowerInvariant();

                // Écriture non-bloquante dans le channel — retour immédiat
                _processChannel.Writer.TryWrite(processName);
            }
            catch
            {
                // Processus déjà terminé : on ignore silencieusement
            }
        }

        #region P/Invoke

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
        }

        [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")] private static extern bool GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")] private static extern bool TranslateMessage(ref NativeMessage lpMsg);
        [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref NativeMessage lpMsg);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion
    }
}