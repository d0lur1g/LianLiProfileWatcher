# Lian Li Profile Watcher

[![CI](https://github.com/d0lur1g/LianLiProfileWatcher/actions/workflows/ci.yml/badge.svg)](https://github.com/d0lur1g/LianLiProfileWatcher/actions/workflows/ci.yml)
[![Coverage](https://img.shields.io/codecov/c/github/<TonCompte>/LianLiProfileWatcher.svg)](https://codecov.io/gh/d0lur1g/LianLiProfileWatcher)
[![Release](https://img.shields.io/github/v/release/d0lur1g/LianLiProfileWatcher)](https://github.com/d0lur1g/LianLiProfileWatcher/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 🧩 **Objectif général**

Le service `LianLiProfileWatcher` s’adresse aux utilisateurs et possesseurs de systèmes de watercooling, de ventilateurs RGB (*FAN control*) de la marque **Lian Li**, ainsi qu’aux utilisateurs du logiciel **L-Connect 3**.

 Ce service a pour objectif de :

> [!NOTE]
> Un **agent Windows léger** qui détecte l’application au premier plan (via un **`hook WinEvent`**) et applique automatiquement un profil prédéfini (fichiers de configuration, dossiers, services) en fonction de l’application active et en temps réel.

---

- [Lian Li Profile Watcher](#lian-li-profile-watcher)
  - [🧩 **Objectif général**](#-objectif-général)
  - [🧱 Architecture et structure du projet](#-architecture-et-structure-du-projet)
    - [📂 Architecture](#-architecture)
    - [📦 Structure du projet](#-structure-du-projet)
  - [⚙️ Prérequis](#️-prérequis)
  - [🛠️ Installation et build](#️-installation-et-build)
    - [Cloner le dépôt](#cloner-le-dépôt)
    - [Restaurer et compiler](#restaurer-et-compiler)
    - [Publier l’agent](#publier-lagent)
  - [🔧 Configuration](#-configuration)
  - [🗺️ Fonctionnement](#️-fonctionnement)
    - [🗺️ Résolution du profil à appliquer](#️-résolution-du-profil-à-appliquer)
    - [📁 Application du profil](#-application-du-profil)
    - [🔁 Détection et debounce](#-détection-et-debounce)
  - [🚀 Exécution \& debug](#-exécution--debug)
    - [En mode console](#en-mode-console)
    - [Logs](#logs)
  - [✅ Tests unitaires](#-tests-unitaires)
  - [🛡️ Intégration Continue (CI)](#️-intégration-continue-ci)
  - [📦 Packaging \& déploiement](#-packaging--déploiement)
    - [Script PowerShell d’installation](#script-powershell-dinstallation)
    - [Script PowerShell de désinstallation](#script-powershell-de-désinstallation)
  - [🔄 Lancement automatique au logon](#-lancement-automatique-au-logon)
    - [⏲️ Tâche planifiée “At logon” (recommandé)](#️-tâche-planifiée-at-logon-recommandé)
    - [🗝️ Clé de registre Run (alternative)](#️-clé-de-registre-run-alternative)
  - [❓ Dépannage](#-dépannage)

---

## 🧱 Architecture et structure du projet

### 📂 Architecture

| Dossier / Composant                                  | Rôle                                                                                                              |
| -----------------------------------------------------| ----------------------------------------------------------------------------------------------------------------- |
| `Program.cs`                                         | Configure le Generic Host (.NET), Serilog, les services DI, les sources de config et enregistre le `Worker`                                        |
| `Worker.cs`                                          | HostedService principal : installe le hook WinEvent, détecte le changement de fenêtre active et appelle `ProfileApplier` |
| `ConfigurationService / IOptionsMonitor`             | Charge et surveille le JSON de config (CLI, env var, LocalAppData, template) et expose le POCO `AppProfileConfig`    |
| `Models/AppProfileConfig.cs`                         | Déclare la classe C# correspondant à la structure JSON de configuration                                          |
| `Infrastructure/Appliers/ProfileApplier.cs`          | Logique d’application d’un profil : nettoyage des anciens dossiers, copie des nouveaux, et redémarrage du service  |
| `ForegroundProcessService.cs`                        | Extrait le nom du processus au premier plan |

### 📦 Structure du projet

```bash
LianLiProfileWatcher/
├─ .git/
├─ .github/
│  └─ workflows/
│     └─ ci.yml
├─ .vscode/
│  └─ extensions.json
├─ Application/
│  └─ Interfaces/
│     ├─ IConfigurationService.cs
│     ├─ IForegroundProcessService.cs
│     └─ IProfileApplier.cs
├─ bin/
├─ Config/
│  └─ appProfiles.example.json
├─ docs/
│  └─ architecture.puml
├─ Infrastructure/
│  └─ Appliers/
│     └─ ProfileApplier.cs
├─ Models/
│  └─ AppProfileConfig.cs
├─ obj/
├─ Properties/
│  └─ launchSettings.json
├─ Scripts/
│  ├─ install-service.ps1
│  └─ uninstall-service.ps1
├─ Services/
│  ├─ ConfigurationService.cs
│  ├─ ForegroundProcessService.cs
│  └─ NativeMethods.cs
├─ tests/
│  └─ LianLiProfileWatcher.Tests/
│     ├─ bin/
│     ├─ obj/
│     ├─ ConfigurationServiceTests.cs
│     ├─ LianLiProfileWatcher.Tests.csproj
│     └─ ProfileApplierTests.cs
├─ .gitignore
├─ CHANGELOG.md
├─ CODE_OF_CONDUCT.md
├─ CONTRIBUTING.md
├─ DEPLOYMENT.md
├─ LianLiProfileWatcher.csproj
├─ LianLiProfileWatcher.sln
├─ LICENSE
├─ Program.cs
├─ README.md
└─ Worker.cs

```

## ⚙️ Prérequis

- **Windows 10/11 x64**  
- **.NET 9.0 SDK** installé ([télécharger](https://dotnet.microsoft.com/download))  
- **PowerShell 5+** (intégré)  
- **Accès en écriture** sur :
  - `%LOCALAPPDATA%` pour les logs et sur le dossier d’installation (ex. `C:\Program Files\…`)  
  - le dossier d’installation et/ou l’emplacement de votre configuration personnelle

## 🛠️ Installation et build

### Cloner le dépôt

```bash
git clone https://github.com/d0lur1g/LianLiProfileWatcher.git
cd LianLiProfileWatcher
```

### Restaurer et compiler

```bash
dotnet restore
dotnet build --configuration Release
```

### Publier l’agent

```powershell
dotnet publish .\src\LianLiProfileWatcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o .\publish

> Exemple : 'dotnet publish .\src\LianLiProfileWatcher.csproj -c Release -o publish'
```

Le dossier **`publish/`** contient l’exécutable, les **`DLLs`** et **`Config/appProfiles.json`**.

## 🔧 Configuration

> ### 📢 Avant de démarrer
>
>1. Choisissez la manière de définir votre fichier de config sans **JAMAIS** toucher au fichier  `Config/appProfiles.example.json`.  
> Voir fichier [DEPLOYMENT.md > Créer ou pointer votre fichier de config](DEPLOYMENT.md)
>2. Adaptez les valeurs selon votre installation locale:
>    - ***`_COMMENT` → A SUPPRIMER DANS VOTRE FICHIER DE CONFIGURATION PERSONNEL***
>    - `baseFolder`
>    - `destination`
>    - `scriptPath`
>    - `default`
>    - `profiles\apps`
>3. Ne commit jamais `Config/appProfiles.json` — il est ignoré par Git.

*Exemple de fichier de configuration* :

```makefile
D:\Configs\appProfiles.json
```

```json
{
  "baseFolder": "<ADD_YOUR_PATH_HERE>\\profiles",
  "destination": "<ADD_YOUR_PATH_HERE>\\Lian-Li\\L-Connect 3\\appdata",
  "scriptPath": "<ADD_YOUR_PATH_HERE>\\lian_li_import.ps1",
  "default": "profile-default",
  "profiles": {
    "chrome": "profile-chrome",
    "notepad": "profile-notepad",
    "code": "profile-vscode",
    "explorer":"profile-explorer"
  }
}
```

## 🗺️ Fonctionnement

### 🗺️ Résolution du profil à appliquer

- À chaque détection d’une nouvelle fenêtre :
  1. On extrait le nom du processus (sans extension, en minuscules).
  2. On cherche ce nom dans le dictionnaire **`profiles`** :
      - Si trouvé → appliquer le profil associé.
      - Sinon → appliquer le profil **`default`**.

✅ **But** : lier chaque application à un **profil visuel personnalisé** (ou mode générique).

### 📁 Application du profil

- **Principe** :
  - Un **profil** est un dossier sous **`baseFolder`** : **`baseFolder\<profil>`**.
  - Ce dossier contient les fichiers de configuration spécifiques à **L-Connect 3**.
- **Pour appliquer** :
    1. Supprimer les anciens fichiers dans destination.
    2. Copier récursivement **`baseFolder\<profil>\`** vers **`destination\`**.
    3. Relancer un service dédié à L-Connect.

✅ **But** : rendre actif le style lumineux défini par l’utilisateur.

### 🔁 Détection et debounce

- **Comportement attendu** :
  - Ne pas réappliquer un profil si l’utilisateur revient sur la même fenêtre.
  - Ignorer les fenêtres système ou invisibles.
- **Implémentation** :
  - Le hook WinEvent déclenche uniquement sur focus.
  - On conserve **`_lastProfile`** et n’appelle **`Apply`** que si **`profile != _lastProfile`**.

✅ **But** : éviter les traitements inutiles et optimiser les performances.

## 🚀 Exécution & debug

### En mode console

Pour développer ou debugger, lancez :

```bash
cd publish
.\LianLiProfileWatcher.exe
```

La console affiche :

```markdown
[Démarrage de l’agent …]
Config chargée : BaseFolder=…, Default=…, Profiles=[chrome,notepad,code]
Hook WinEvent installé.
Fenêtre active détectée : chrome
→ Application du profil « profile-chrome »
...
```

### Logs

Un fichier agent.log est créé dans :

```lua
%LOCALAPPDATA%\LianLiProfileWatcher\Logs\agent.log
```

Toutes les **entrées console** et **info/erreur** y sont consignées, avec rotation quotidienne et rétention 7 jours.

## ✅ Tests unitaires

Les tests sont dans tests/LianLiProfileWatcher.Tests. Pour exécuter :

```bash
dotnet test --configuration Release
```

- ConfigurationServiceTests : chargement JSON & erreurs.

- ProfileApplierTests : copie/suppression de dossiers.

## 🛡️ Intégration Continue (CI)

Un workflow GitHub Actions **`(.github/workflows/ci.yml)`** déclenche sur push/PR vers main :

1. `dotnet restore`
2. `dotnet build --configuration Release`
3. `dotnet test --configuration Release`
4. (*optionnel*) collecte de couverture via Coverlet

## 📦 Packaging & déploiement

### Script PowerShell d’installation

\+ de détails dans le fichier [DEPLOYMENT.md > Copier les fichiers + Installation du service](DEPLOYMENT.md#51---copier-les-fichiers)

Le script se trouve dans **`Scripts/install-service.ps1`**.

Exécute ce script ainsi (**depuis le dossier Scripts\\**) :

```powershell
.\install-service.ps1 `
-InstallDir "C:\<MON_PATH>\LianLiProfileWatcher" `
-ServiceName "LianLiProfileWatcher-Agent" `
-ConfigPath  "D:\<PATH_CONFIG>\appProfiles.json"
```

### Script PowerShell de désinstallation

\+ de détails dans le fichier [DEPLOYMENT.md > Désinstallation du service](DEPLOYMENT.md#72---désinstallation-du-service-windows)

Le script **`Scripts/uninstall-service.ps1`** :

Exécute ce script ainsi (**depuis le dossier Scripts\\**) :

```powershell
param($InstallDir="C:\Program Files\LianLiProfileWatcher",$ServiceName="LianLiProfileWatcher")
sc.exe stop $ServiceName
sc.exe delete $ServiceName
if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }
Write-Host "Service désinstallé et fichiers supprimés."
```

## 🔄 Lancement automatique au logon

### ⏲️ Tâche planifiée “At logon” (recommandé)

\+ de détails dans le fichier [DEPLOYMENT.md > Créer une tâche planifiée](DEPLOYMENT.md#52---configurer-une-tâche-planifiée-recommandé)

### 🗝️ Clé de registre Run (alternative)

\+ de détails dans le fichier [DEPLOYMENT.md > Créer une clé de registre](DEPLOYMENT.md#53---clé-de-registre-run-alternative)

## ❓ Dépannage

- **Aucun log** dans **`agent.log`**
  - Vérifiez le chemin **`%LOCALAPPDATA%`**, les droits NTFS.
  - Lancez manuellement en console pour voir les erreurs immédiates.

- **Le hook ne détecte pas les fenêtres**
  - Assurez-vous d’être sur une session interactive (pas un service).
  - Vérifiez que **`WinEventProc`** logge bien les processus (test en console).
- **Service Windows vs agent**
  - Les services Windows ne peuvent pas hooker des sessions utilisateurs.
  - Utilisez **exclusivement l’agent** (Tâche planifiée) en session utilisateur.

Pour plus de détails, voir la documentation complète :

- [DEPLOYMENT](DEPLOYMENT.md)
- [CHANGELOG](CHANGELOG.md)  
- [CONTRIBUTING](CONTRIBUTING.md)  
- [CODE OF CONDUCT](CODE_OF_CONDUCT.md)  
- [Guide de déploiement détaillé](docs/DEPLOYMENT.md)  
- [Schéma d’architecture (PlantUML)](docs/architecture.puml)
