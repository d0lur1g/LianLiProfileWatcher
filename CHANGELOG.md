# Changelog

Toutes les modifications notables de ce projet seront documentées dans ce fichier.

## [Unreleased]

- Amélioration de la config pour faciliter l'ajout de profils

## [2.0.0] – 2026-05-15

### Changed

- **`Worker.cs`** — `WinEventProc` ne fait plus qu'un `TryWrite()` non-bloquant dans un `Channel<string>` ; le thread STA de la boucle de messages Windows n'est plus jamais bloqué
- **`Worker.cs`** — Ajout d'un debounce de 2000 ms : seul le focus stable depuis 2 secondes déclenche l'application d'un profil ; les changements de fenêtre rapides (alt-tab) sont absorbés sans déclencher de restart
- **`Worker.cs`** — Pipeline asynchrone via `Channel<string>` + `Task.Run` : la détection (thread STA) et le traitement (thread pool) sont désormais découplés
- **`ProfileApplier.cs`** — `Apply(string)` synchrone remplacé par `ApplyAsync(string, CancellationToken)` : toutes les opérations bloquantes (I/O fichiers, restart service) sont désormais asynchrones et interruptibles
- **`ProfileApplier.cs`** — Ajout d'une garde `_lastAppliedProfile` : si le profil résolu est identique au dernier appliqué, aucune copie de fichiers ni restart de service ne sont déclenchés
- **`ProfileApplier.cs`** — `Thread.Sleep(3s)` remplacé par `await Task.Delay(5s, cancellationToken)` : le délai post-démarrage de L-Connect est désormais interruptible par annulation
- **`ProfileApplier.cs`** — `ServiceController.WaitForStatus()` bloquant remplacé par `WaitForStatusAsync()` en polling toutes les 200 ms, respectant le `CancellationToken`
- **`IProfileApplier.cs`** — Interface mise à jour pour exposer `Task ApplyAsync(string, CancellationToken)` en lieu et place de `void Apply(string)`

### Fixed

- Ralentissement de l'affichage des profils à chaque changement de fenêtre applicative (thread STA bloqué par `Apply()` synchrone)
- Interruptions et redémarrages incessants de `LConnectService` lors d'alt-tabs rapides
- Instabilité sur la durée causée par `Thread.Sleep` non interruptible et accumulation de travail sur le thread de messages
- `OperationCanceledException` non propagée correctement lors de l'arrêt du service

## [1.0.0] – 2025-07-26

### Added

- Hook WinEvent pour détection en session utilisateur
- Agent sans console (WinExe + tâche planifiée)
- CI GitHub Actions (restore, build, test)
- Tests unitaires pour ConfigurationService et ProfileApplier
- Logging Serilog + rotation journalière

### Changed

- Migration au .NET 9.0  
- Lecture JSON case-insensitive

### Removed

- Polling interval
- Service Windows interactif

## [0.0.1] – 2025-07-23 - initial setup
