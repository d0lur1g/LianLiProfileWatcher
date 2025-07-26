# Changelog

Toutes les modifications notables de ce projet seront documentées dans ce fichier.

## [Unreleased]

- improve config file for add profile easily and speedly

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
