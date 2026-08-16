# Lian Li Profile Watcher

> [!WARNING]
> ## 🚧 Projet non maintenu — remplacé par **[lconnect-auto-profiler](https://github.com/d0lur1g/lconnect-auto-profiler)**
>
> Ce dépôt reste en ligne à titre de référence, mais le développement s'est déplacé vers un
> successeur plus abouti et plus fonctionnel :
>
> ### 👉 https://github.com/d0lur1g/lconnect-auto-profiler
>
> **Ce qui change :**
>
> | | Lian Li Profile Watcher (ici) | lconnect-auto-profiler |
> | --- | --- | --- |
> | Méthode | Copie les dossiers `device\` / `profile\` sur disque, puis **redémarre `LConnectService`** | Pilote `LConnectService` via son **API HTTP locale** (`http://127.0.0.1:11021/`) — aucune copie de fichiers, aucun redémarrage de service |
> | Portée | Éclairage uniquement | Éclairage, **courbes de ventilation** et **contenu de l'écran GA II LCD** |
> | Profils | Dossiers capturés à la main depuis `ProgramData` | Exports natifs L-Connect (*Profile → Export*) importés par script, **rechargés à chaud** |
> | Installation | Tâche planifiée `/RL HIGHEST` (droits élevés requis pour redémarrer le service) | Tâche planifiée à l'ouverture de session, **sans droits administrateur** |
> | Protocole | — | Documenté et couvert par des tests basés sur des captures Wireshark du logiciel officiel |
>
> Les utilisateurs de ce dépôt sont invités à migrer. Aucune correction ni nouvelle
> fonctionnalité n'est prévue ici.

[![CI](https://github.com/d0lur1g/LianLiProfileWatcher/actions/workflows/ci.yml/badge.svg)](https://github.com/d0lur1g/LianLiProfileWatcher/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/d0lur1g/LianLiProfileWatcher)](https://github.com/d0lur1g/LianLiProfileWatcher/releases)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

Agent Windows léger qui détecte l'application au premier plan via un **hook WinEvent** (`SetWinEventHook`) et applique automatiquement un profil L-Connect 3 prédéfini en fonction de l'application active, en temps réel.

> **Compatibilité** : L-Connect 3 **v2.1.20+** — Windows 10/11 x64

---

## Table des matières

- [Prérequis](#️-prérequis)
- [Architecture](#-architecture)
- [Installation et build](#️-installation-et-build)
- [Configuration](#-configuration)
- [Fonctionnement](#️-fonctionnement)
- [Exécution et logs](#-exécution--logs)
- [Lancement automatique](#-lancement-automatique-au-démarrage)
- [Tests unitaires](#-tests-unitaires)
- [Intégration Continue](#️-intégration-continue-ci)
- [Dépannage](#-dépannage)

---

## ⚙️ Prérequis

- **Windows 10 / 11 x64**
- **.NET 9.0 SDK** — [télécharger](https://dotnet.microsoft.com/download)
- **L-Connect 3 v2.1.20+** installé et en cours d'exécution
- **PowerShell 5+** (intégré à Windows)
- Droits d'écriture sur `C:\ProgramData\Lian-Li\L-Connect 3\` (requis pour appliquer les profils)

---

## 🧱 Architecture

### Composants

| Fichier | Rôle |
| --- | --- |
| `Program.cs` | Configure le Generic Host .NET, Serilog, l'injection de dépendances et enregistre le `Worker` |
| `Worker.cs` | Service principal : installe le hook WinEvent sur thread STA dédié, détecte le changement de fenêtre active, appelle `ProfileApplier` |
| `Services/ConfigurationService.cs` | Charge et surveille le JSON de config (CLI → ENV → LocalAppData → template) ; expose le POCO `AppProfileConfig` |
| `Models/AppProfileConfig.cs` | Modèle C# correspondant à la structure JSON de configuration |
| `Infrastructure/Appliers/ProfileApplier.cs` | Applique un profil : nettoyage des dossiers cibles, copie récursive, redémarrage du service `LConnectService` |
| `Services/ForegroundProcessService.cs` | Extrait le nom du processus au premier plan via les API Win32 |
| `Services/NativeMethods.cs` | P/Invoke : `SetWinEventHook`, `UnhookWinEvent`, `GetWindowThreadProcessId` |

### Structure du projet

```text
LianLiProfileWatcher/
├── .github/workflows/ci.yml
├── Application/Interfaces/
│   ├── IConfigurationService.cs
│   ├── IForegroundProcessService.cs
│   └── IProfileApplier.cs
├── Config/
│   └── appProfiles.example.json
├── Infrastructure/Appliers/
│   └── ProfileApplier.cs
├── Models/
│   └── AppProfileConfig.cs
├── Scripts/
│   ├── install-service.ps1
│   └── uninstall-service.ps1
├── Services/
│   ├── ConfigurationService.cs
│   ├── ForegroundProcessService.cs
│   └── NativeMethods.cs
├── tests/LianLiProfileWatcher.Tests/
│   ├── ConfigurationServiceTests.cs
│   └── ProfileApplierTests.cs
├── docs/architecture.puml
├── Program.cs
├── Worker.cs
└── LianLiProfileWatcher.csproj
```

---

## 🛠️ Installation et build

### 1. Cloner le dépôt

```bash
git clone https://github.com/d0lur1g/LianLiProfileWatcher.git
cd LianLiProfileWatcher
```

### 2. Restaurer, compiler et publier

```powershell
# Nettoyage
dotnet clean

# Restauration des dépendances
dotnet restore

# Vérification build
dotnet build -c Release

# Publication — exécutable autonome Windows x64
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -o .\publish\
```

L'exécutable final se trouve dans `.\publish\LianLiProfileWatcher.exe`.

---

## 🔧 Configuration

### Fichier `appProfiles.json`

Copie `Config/appProfiles.example.json` vers un emplacement personnel (ne jamais modifier l'exemple, ne jamais committer ton fichier personnel — il est dans `.gitignore`).

```json
{
  "baseFolder": "C:\\LianLiProfiles",
  "destination": "C:\\ProgramData\\Lian-Li\\L-Connect 3",
  "serviceName": "LConnectService",
  "default": "default",
  "profiles": {
    "cyberpunk2077": "gaming",
    "devenv":        "work",
    "vlc":           "media",
    "chrome":        "browsing"
  }
}
```

| Champ | Description |
| --- | --- |
| `baseFolder` | Dossier racine contenant tes profils sources |
| `destination` | Chemin de données L-Connect 3 — **`C:\ProgramData\Lian-Li\L-Connect 3`** en v2.1.20+ |
| `serviceName` | Nom du service Windows L-Connect — **`LConnectService`** |
| `default` | Profil appliqué si l'application active n'est pas dans le mapping |
| `profiles` | Dictionnaire `nom_processus (sans .exe, minuscules) → nom_profil` |

> [!IMPORTANT]
> Le champ `destination` a changé en v2.1.20. L'ancien chemin `%APPDATA%\LianLi\LConnect3\` n'est plus valide. Utilise impérativement `C:\ProgramData\Lian-Li\L-Connect 3`.

### Préparer les profils sources

Un profil est un dossier sous `baseFolder` contenant les sous-dossiers `device\` et `profile\` tels que L-Connect 3 les structure en interne. Pour capturer l'état courant :

```powershell
$profileName = "gaming"  # nom de ton choix
$src = "C:\ProgramData\Lian-Li\L-Connect 3"
$dst = "C:\LianLiProfiles\$profileName"

Copy-Item "$src\device"  "$dst\device"  -Recurse -Force
Copy-Item "$src\profile" "$dst\profile" -Recurse -Force
Write-Host "Profil '$profileName' sauvegardé."
```

Répète pour chaque profil après avoir configuré l'éclairage souhaité dans L-Connect 3.

Structure attendue dans `baseFolder` :

```text
C:\LianLiProfiles\
├── gaming\
│   ├── device\
│   └── profile\
├── work\
│   ├── device\
│   └── profile\
└── default\
    ├── device\
    └── profile\
```

### Résolution du fichier de config

L'agent recherche `appProfiles.json` dans cet ordre :

1. Argument CLI : `--config "D:\Configs\appProfiles.json"`
2. Variable d'environnement : `LIANLI_CONFIG=D:\Configs\appProfiles.json`
3. `%LOCALAPPDATA%\LianLiProfileWatcher\Config\appProfiles.json`
4. `Config\appProfiles.json` (dossier de l'exécutable — fallback)

---

## 🗺️ Fonctionnement

### Détection de la fenêtre active

`Worker.cs` installe `SetWinEventHook(EVENT_SYSTEM_FOREGROUND, WINEVENT_OUTOFCONTEXT)` sur un thread STA dédié avec boucle `GetMessage` / `DispatchMessage`. Le callback `WinEventProc` se déclenche en moins de 50 ms à chaque changement de focus.

### Anti-flood

La variable `_lastProcessName` empêche toute réapplication superflue : si le processus actif n'a pas changé depuis le dernier événement, `ProfileApplier.Apply()` n'est pas appelé.

### Filtrage des fenêtres parasites

Les fenêtres sans titre, les processus système (`dwm`, `winlogon`, etc.) et les handles invalides sont ignorés avant toute résolution de profil.

### Application d'un profil

Quand un nouveau processus est détecté :

1. Recherche du nom du processus (sans `.exe`, en minuscules) dans le dictionnaire `profiles`
2. Si absent → utilisation du profil `default`
3. Suppression des dossiers `device\` et `profile\` dans `destination`
4. Copie récursive depuis `baseFolder\<profil>\device` et `baseFolder\<profil>\profile`
5. Redémarrage du service `LConnectService` pour que L-Connect 3 recharge sa configuration

---

## 🚀 Exécution & logs

### Mode console (debug)

```powershell
cd .\publish\
.\LianLiProfileWatcher.exe
```

Sortie attendue :

```text
[INF] Config chargée : BaseFolder=C:\LianLiProfiles, Default=default, Profiles=[gaming,work,media]
[INF] Hook WinEvent installé.
[INF] Fenêtre active détectée : cyberpunk2077
[INF] → Application du profil « gaming »
[INF] Service LConnectService redémarré avec succès.
```

### Fichiers de log

```text
%LOCALAPPDATA%\LianLiProfileWatcher\Logs\agent-YYYYMMDD.log
```

- Rotation quotidienne, rétention 7 jours
- Niveaux : `DEBUG` / `INFO` / `WARN` / `ERROR`
- Format JSON structuré (Serilog)

Surveiller en temps réel :

```powershell
Get-Content "$env:LOCALAPPDATA\LianLiProfileWatcher\Logs\agent*.log" -Wait -Tail 20
```

---

## 🔄 Lancement automatique au démarrage

### Option 1 — Tâche planifiée AtLogon (recommandé si droits élevés requis)

Droits élevés nécessaires pour redémarrer `LConnectService` :

```powershell
$exe = "$env:LOCALAPPDATA\LianLiProfileWatcher\LianLiProfileWatcher.exe"
schtasks /Create /TN "LianLiProfileWatcher" /TR $exe /SC ONLOGON /RL HIGHEST /F
```

### Option 2 — Clé de registre Run (sans droits élevés)

```powershell
$exe = "$env:LOCALAPPDATA\LianLiProfileWatcher\LianLiProfileWatcher.exe"
Set-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" `
  -Name "LianLiProfileWatcher" -Value $exe
```

> [!NOTE]
> Si la tâche planifiée est utilisée, supprime la clé de registre Run pour éviter un double démarrage.

### Scripts d'installation / désinstallation

```powershell
# Installation
.\Scripts\install-service.ps1 `
  -InstallDir "C:\Users\<USER>\AppData\Local\LianLiProfileWatcher" `
  -ConfigPath  "D:\Configs\appProfiles.json"

# Désinstallation
.\Scripts\uninstall-service.ps1
```

---

## ✅ Tests unitaires

```powershell
dotnet test -c Release
```

Couverture :

- `ConfigurationServiceTests` — chargement JSON, valeurs manquantes, config absente
- `ProfileApplierTests` — copie/suppression de dossiers, service introuvable, profil inexistant

---

## 🛡️ Intégration Continue (CI)

Le workflow `.github/workflows/ci.yml` se déclenche sur chaque push et PR vers `main` :

1. `dotnet restore`
2. `dotnet build -c Release`
3. `dotnet test -c Release`

---

## ❓ Dépannage

**Aucun log dans `agent.log`**
→ Lance l'exécutable en console pour voir les erreurs immédiates. Vérifie les droits NTFS sur `%LOCALAPPDATA%`.

**Le hook ne détecte pas les changements de fenêtre**
→ L'agent doit s'exécuter en session utilisateur interactive, pas en service Windows pur. Utilise la tâche planifiée AtLogon.

**`Service 'LConnectService' introuvable`**
→ Vérifie que L-Connect 3 est installé et que le service existe : `Get-Service LConnectService`. Vérifie le champ `serviceName` dans `appProfiles.json`.

**Le profil est appliqué mais l'éclairage ne change pas**
→ Vérifie que `destination` pointe bien vers `C:\ProgramData\Lian-Li\L-Connect 3` et que les sous-dossiers `device\` et `profile\` existent dans ton profil source.

**Double application du même profil au démarrage**
→ Vérifie qu'il n'y a pas à la fois une clé de registre Run ET une tâche planifiée actives.

---

## 📚 Documentation complémentaire

- [DEPLOYMENT.md](DEPLOYMENT.md) — déploiement détaillé pas à pas
- [CHANGELOG.md](CHANGELOG.md) — historique des versions
- [CONTRIBUTING.md](CONTRIBUTING.md) — contribuer au projet
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- [docs/architecture.puml](docs/architecture.puml) — schéma d'architecture PlantUML
