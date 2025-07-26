# Changelog

## [1.0.0] – 2025-07-26

### Added

- Hook WinEvent pour détection en session utilisateur
- Agent sans console (WinExe + tâche planifiée)
- Service de copie de profils (`ProfileApplier`)
- CI GitHub Actions (restore, build, test)

### Changed

- Migration au .NET 9.0  
- Logging Serilog + fichier `agent.log`

### Removed

- Polling précédent  
- Service Windows interactif
