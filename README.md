# Lian Li Profile Watcher

[![CI](https://github.com/d0lur1g/LianLiProfileWatcher/actions/workflows/ci.yml/badge.svg)](https://github.com/d0lur1g/LianLiProfileWatcher/actions/workflows/ci.yml)
[![Coverage](https://img.shields.io/codecov/c/github/<TonCompte>/LianLiProfileWatcher.svg)](https://codecov.io/gh/d0lur1g/LianLiProfileWatcher)
[![Release](https://img.shields.io/github/v/release/d0lur1g/LianLiProfileWatcher)](https://github.com/d0lur1g/LianLiProfileWatcher/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 🧩 **Objectif général**

Le service `LianLiProfileWatcher` s’adresse aux utilisateurs et possesseurs de systèmes de watercooling, de ventilateurs RGB (*FAN control*) de la marque **Lian Li**, ainsi qu’aux utilisateurs du logiciel **L-Connect 3**.

 Ce service a pour objectif de :

> Un **agent Windows léger** qui détecte l’application au premier plan (via un **`hook WinEvent`**) et applique automatiquement un profil prédéfini (fichiers de configuration, dossiers, services) en fonction de l’application active et en temps réel.

---

## Table des matières

1. [🧱 Architecture et structure du projet](#-architecture-et-structure-du-projet)  
    1. [📂 Architecture](#-architecture)
    2. [📦 Structure du projet](#-structure-du-projet)
2. [⚙️ Prérequis](#️-prérequis)  
3. [🛠️ Installation et build](#️-installation-et-build)  
    1. [Cloner le dépôt](#cloner-le-dépôt)  
    2. [Restaurer et compiler](#restaurer-et-compiler)  
    3. [Publier l’agent](#publier-lagent)  
4. [🔧 Configuration](#-configuration)
5. [🗺️ Fonctionnement](#️-fonctionnement)  
    1. [🗺️ Résolution du profil à appliquer](#️-résolution-du-profil-à-appliquer)  
    2. [📁 Application du profil](#-application-du-profil)  
    3. [🔁 Détection et debounce](#-détection-et-debounce)  
6. [🚀 Exécution & debug](#-exécution--debug)  
    1. [En mode console](#en-mode-console)  
    2. [Logs](#logs)  
7. [✅ Tests unitaires](#-tests-unitaires)  
8. [🛡️ Intégration Continue (CI)](#️-intégration-continue-ci)  
9. [📦 Packaging & déploiement](#-packaging--déploiement)  
    1. [Script PowerShell d’installation](#script-powershell-dinstallation)  
    2. [Script PowerShell de désinstallation](#script-powershell-de-désinstallation)  
10. [🔄 Lancement automatique au logon](#-lancement-automatique-au-logon)  
    1. [Tâche planifiée “At logon”](#tâche-planifiée-at-logon-recommandé)  
    2. [Clé de registre Run](#clé-de-registre-run-alternative)  
11. [❓ Dépannage](#-dépannage)  

---

## 🧱 Architecture et structure du projet

### 📂 Architecture

| Dossier / Composant                                  | Rôle                                                                                                              |
| -----------------------------------------------------| ----------------------------------------------------------------------------------------------------------------- |
| `Program.cs`                                         | Configure le Generic Host (.NET), Serilog, la DI et enregistre le `Worker`                                        |
| `Worker.cs`                                          | HostedService principal : installe le hook WinEvent, détecte le changement de fenêtre active et appelle `ProfileApplier` |
| `Services/ConfigurationService.cs`                   | Lit et parse `Config/appProfiles.json` (désérialisation case-insensitive) et expose le POCO `AppProfileConfig`    |
| `Models/AppProfileConfig.cs`                         | Déclare la classe C# correspondant à la structure JSON de configuration                                          |
| `Infrastructure/Appliers/ProfileApplier.cs`          | Logique d’application d’un profil : nettoyage des anciens dossiers, copie des nouveaux, et redémarrage du service  |

---

## 📦 Structure du projet

```bash
LianLiProfileWatcher/
├─ .git/
├─ .github/
│  └─ workflows/
│     └─ ci.yml
├─ Application/
│  └─ Interfaces/
│     ├─ IConfigurationService.cs
│     ├─ IForegroundProcessService.cs
│     └─ IProfileApplier.cs
├─ bin/
├─ Config/
│  └─ appProfiles.json
├─ Infrastructure/
│  └─ Appliers/
│     └─ ProfileApplier.cs
├─ Models/
│  └─ AppProfileConfig.cs
├─ obj/
├─ Properties/
│  └─ launchSettings.json
├─ publish/
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
├─ LianLiProfileWatcher.csproj
├─ LianLiProfileWatcher.sln
├─ Program.cs
├─ README.md
└─ Worker.cs
```

## ⚙️ Prérequis

- **Windows 10/11 x64**  
- **.NET 9.0 SDK** installé ([télécharger](https://dotnet.microsoft.com/download))  
- **PowerShell 5+** (intégré)  
- **Accès en écriture** sur `%LOCALAPPDATA%` pour les logs et sur le dossier d’installation (ex. `C:\Program Files\…`)  

---

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
dotnet publish .\LianLiProfileWatcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o .\publish

> Exemple : 'dotnet publish .\LianLiProfileWatcher.csproj -c Release -o publish'
```

Le dossier **`publish/`** contient l’exécutable, les **`DLLs`** et **`Config/appProfiles.json`**.

## 🔧 Configuration

Placez votre fichier **`Config/appProfiles.json`** à la racine du dossier **`publish/`**.

Exemple :

```json
{
  "baseFolder": "E:\\Programmes\\Lian Li - L-Connect 3\\Profiles",
  "destination": "C:\\Users\\<User>\\AppData\\Local\\LianLiProfileWatcher\\ActiveProfile",
  "scriptPath": "E:\\Scripts\\lian_li_import.ps1",
  "default": "profile-default",
  "profiles": {
    "chrome": "profile-chrome",
    "notepad": "profile-notion",
    "code": "profile-vscode"
  }
}
```

- **`baseFolder`** : racine contenant les sous-dossiers de chaque profil.

- **`destination`** : dossier où copier les fichiers du profil actif.

- **`scriptPath`** : chemin vers un éventuel script PowerShell à exécuter après copie.

- **`default`** : profil à appliquer si aucun process n’est reconnu.

- **`profiles`** : map de processName → nomDuProfil.

## 🗺️ Fonctionnement

## 🗺️ Résolution du profil à appliquer

- À chaque détection d’une nouvelle fenêtre :
  1. On extrait le nom du processus (sans extension, en minuscules).
  2. On cherche ce nom dans le dictionnaire **`profiles`** :
      - Si trouvé → appliquer le profil associé.
      - Sinon → appliquer le profil **`default`**.

✅ **But** : lier chaque application à un **profil visuel personnalisé** (ou mode générique).

## 📁 Application du profil

- **Principe** :
  - Un **profil** est un dossier sous **`baseFolder`** : **`baseFolder\<profil>`**.
  - Ce dossier contient les fichiers de configuration spécifiques à **L-Connect 3**.
- **Pour appliquer** :
    1. Supprimer les anciens fichiers dans destination.
    2. Copier récursivement **`baseFolder\<profil>\`** vers **`destination\`**.
    3. *Optionnel* : exécuter un script (PowerShell, relancer un service, etc.).

✅ **But** : rendre actif le style lumineux défini par l’utilisateur.

## 🔁 Détection et debounce

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

Ajoutez dans README.md :

```markdown
[![CI](https://github.com/<votre-compte>/LianLiProfileWatcher/actions/workflows/ci.yml/badge.svg)](https://github.com/<VOTRE-COMPTE>/LianLiProfileWatcher/actions/workflows/ci.yml)
```

## 📦 Packaging & déploiement

### Script PowerShell d’installation

Le script **`Scripts/install-service.ps1`** :

```powershell
param($InstallDir="C:\Program Files\LianLiProfileWatcher",$ServiceName="LianLiProfileWatcher")

$ScriptDir = Split-Path $MyInvocation.MyCommand.Definition
$PublishDir = Join-Path $ScriptDir '..\publish'

# Nettoyage
if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

# Copie
Copy-Item (Join-Path $PublishDir '*') $InstallDir -Recurse -Force

# Créer et démarrer le service (optionnel si vous ne l’utilisez plus)
sc.exe create $ServiceName binPath= "`"$InstallDir\LianLiProfileWatcher.exe`"" start= auto
sc.exe start $ServiceName
Write-Host "Service installé et démarré."
```

### Script PowerShell de désinstallation

Le script **`Scripts/uninstall-service.ps1`** :

```powershell
param($InstallDir="C:\Program Files\LianLiProfileWatcher",$ServiceName="LianLiProfileWatcher")
sc.exe stop $ServiceName
sc.exe delete $ServiceName
if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }
Write-Host "Service désinstallé et fichiers supprimés."
```

## 🔄 Lancement automatique au logon

### Tâche planifiée “At logon” (recommandé)

1. Ouvrez Planificateur de tâches (***taskschd.msc***).
2. Créer une tâche…
    1. **Général** : nom **`LianLiProfileWatcher-Agent`**, cocher « Masquer », « Exécuter que l’utilisateur soit connecté ou non ».
    2. **Déclencheurs** : nouveau déclencheur « *À l’ouverture de session* ».
    3. **Actions** :
        - Démarrer un programme → « *Cible* » vers **`publish\LianLiProfileWatcher.exe`**,
        - « *Démarrer dans* » = dossier **`publish`**.
        - Enregistrez.

L’agent tournera en arrière-plan (pas de console à l’écran).

### Clé de registre Run (alternative)

1. Ouvrez regedit.
2. Allez à : **`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`**
3. Créez une Valeur chaîne **`LianLiProfileWatcher`** dont la donnée est :

```arduino
"C:\Program Files\LianLiProfileWatcher\LianLiProfileWatcher.exe"
```

À la prochaine connexion, l’agent démarrera.

## ❓ Dépannage

- **Aucun log** dans **`agent.log`**
  - Vérifiez le chemin **`%LOCALAPPDATA%`**, les droits NTFS.
  - Lancez manuellement en console pour voir les erreurs immédiates.

- **Le hook ne détecte pas les fenêtres**
  - Assurez-vous d’être sur une session interactive (pas un service).
  - Vérifiez que **`WinEventProc`** logge bien les processus (test en console).
- **Service Windows vs agent**
  - Les services Windows ne peuvent pas hooker des sessions utilisateurs.
  - Utilisez exclusivement l’agent en session utilisateur.

Pour plus de détails, voir la documentation complète :

- [CHANGELOG](CHANGELOG.md)  
- [CONTRIBUTING](CONTRIBUTING.md)  
- [CODE OF CONDUCT](CODE_OF_CONDUCT.md)  
- [Guide de déploiement détaillé](docs/DEPLOYMENT.md)  
- [Schéma d’architecture (PlantUML)](docs/architecture.puml)
