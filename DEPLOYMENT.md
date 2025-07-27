# Guide de déploiement

> [!NOTE]
> Ce document décrit pas à pas comment publier, installer, configurer et désinstaller l’agent **LianLiProfileWatcher**, en intégrant le mécanisme de configuration externe.

- [Guide de déploiement](#guide-de-déploiement)
  - [1. Prérequis](#1-prérequis)
  - [2. Publication](#2-publication)
  - [3. Packaging](#3-packaging)
  - [4. Configuration de l’agent](#4-configuration-de-lagent)
  - [5. Installation de l’agent](#5-installation-de-lagent)
    - [5.1 - Copier les fichiers](#51---copier-les-fichiers)
      - [Méthode Automatique (RECOMMANDEE)](#méthode-automatique-recommandee)
      - [Méthode manuelle](#méthode-manuelle)
    - [5.2 - Configurer une tâche planifiée (recommandé)](#52---configurer-une-tâche-planifiée-recommandé)
    - [5.3 - Clé de registre Run (alternative)](#53---clé-de-registre-run-alternative)
  - [6. Vérification](#6-vérification)
  - [7. Désinstallation](#7-désinstallation)
    - [7.1 - Désinstallation de la tâche planifiée](#71---désinstallation-de-la-tâche-planifiée)
    - [7.2 - Désinstallation du service Windows](#72---désinstallation-du-service-windows)
  - [8. Architecture déployée](#8-architecture-déployée)

## 1. Prérequis

- **Windows 10/11 x64**  
- **.NET 9.0 Runtime** installé  
- **PowerShell 5+** (intégré à Windows)  
- Droits en écriture sur le dossier d’installation (ex. `C:\Program Files\…`) et, si nécessaire, sur le dossier où vous placez votre config perso

## 2. Publication

Depuis la racine du projet (là où se trouve `LianLiProfileWatcher.csproj`), exécutez :

```powershell
# Supprimer un ancien dossier publish
if (Test-Path .\publish) { Remove-Item .\publish -Recurse -Force }

# Publier en Release pour Windows x64, framework-dependent
dotnet publish .\LianLiProfileWatcher.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o .\publish
```

Le dossier `publish/` contient désormais :

- `LianLiProfileWatcher.exe`
- `*.deps.json`, `*.runtimeconfig.json`
- `Config/appProfiles.example.json` (template)
- Toutes les `DLL` nécessaires

## 3. Packaging

Pour générer l’archive ZIP de la version v\<major>.\<minor>.\<patch> :

```powershell
# Supprimer un ancien zip
if (Test-Path .\lian-li-profile-watcher-v<major>.<minor>.<patch>.zip) {
  Remove-Item .\lian-li-profile-watcher-v<major>.<minor>.<patch>.zip -Force
}

# Créer le zip à partir du contenu de publish/
Compress-Archive `
  -Path (Join-Path $PWD 'publish\*') `
  -DestinationPath (Join-Path $PWD 'lian-li-profile-watcher-v<major>.<minor>.<patch>.zip') `
  -Force
```

→ Vous obtenez `lian-li-profile-watcher-v<major>.<minor>.<patch>.zip`, prêt à être attaché à une Release GitHub.

## 4. Configuration de l’agent

L’agent peut charger **une seule** configuration JSON, dont l’emplacement défini sera recherché dans cet ordre :

1. **Argument CLI**

    ```powershell
    .\LianLiProfileWatcher.exe --config "D:\<PATH_CONFIG>\appProfiles.json"
    ```

2. **Variable d’environnement**

    ```powershell
    setx LIANLI_CONFIG_PATH "D:\<PATH_CONFIG>\appProfiles.json"
    ```

3. **Fichier local**

    ```shell
    mkdir "$Env:LOCALAPPDATA\LianLiProfileWatcher\Config" -Force
    copy .\publish\Config\appProfiles.example.json `
        "$Env:LOCALAPPDATA\LianLiProfileWatcher\Config\appProfiles.json"
    ```

4. **Template intégré (fallback)**

    > [!CAUTION]
    > 📢 Ne pas modifier ``appProfiles.example.json`` dans le dossier ``publish/`` → C’est un template versionné.

    1. Créez un nouveau fichier basé sur le fichier exemple de configuration publish\Config\appProfiles.example.json  
    2. Editez uniquement votre propre ``appProfiles.json`` selon l’une des méthodes cités ci-dessus.

        ```bash
        Config/appProfiles.example.json
        ```

## 5. Installation de l’agent

> ### Avant de démarrer
>
>1. Choisissez la manière de définir votre fichier de config sans **JAMAIS** toucher au fichier  `Config/appProfiles.example.json`.
>2. Adaptez les valeurs selon votre installation locale:
>    - ***`_COMMENT` → A SUPPRIMER DANS VOTRE FICHIER DE CONFIGURATION PERSONNEL***
>    - `baseFolder`
>    - `destination`
>    - `scriptPath`
>    - `default`
>    - `profiles\apps`
>3. Ne jamais commit `Config/appProfiles.json` — il est ignoré par Git.

### 5.1 - Copier les fichiers

#### Méthode Automatique (RECOMMANDEE)

Le **Script PowerShell d'installation** se trouve dans **`Scripts/install-service.ps1`**.

Exécute ce script ainsi (**depuis le dossier Scripts\\**) :

```powershell
.\install-service.ps1 `
-InstallDir "C:\<MON_PATH>\LianLiProfileWatcher" `
-ServiceName "LianLiProfileWatcher-Agent" `
-ConfigPath  "D:\<PATH_CONFIG>\appProfiles.json"
```

#### Méthode manuelle

1. Créez le dossier d’installation, par exemple :

    ```makefile
    C:\Program Files\LianLiProfileWatcher
    ```

2. Copiez **tout** le contenu de `publish/` (mais pas votre `appProfiles.json` perso) dans ce dossier.

### 5.2 - Configurer une tâche planifiée (recommandé)

1. Ouvrez ***Planificateur de tâches*** (`askschd.msc`).
2. Cliquez sur ***Créer une tâche…***.
3. Onglet ***Général*** :
    - Nom : `LianLiProfileWatcher-Agent`
    - Cochez :  
    [**X**] ***N'exécuter que si l'utilisateur est connecté***  
    [**X**] ***Exécuter avec les autorisations maximales***
    - Configurer pour ***Windows 10***
4. Onglet ***Déclencheurs*** → ***Nouveau…*** :
    - ***À l’ouverture de session***
    - ***Utilisateur spécifique*** : Saisir vos identifiants de la session dans laquelle vous souhaitez faire fonctionner l'agent.
    - Cochez :  
    [**X**] ***Activée***
5. Onglet ***Actions*** → ***Nouvelle…*** :
    - ***Démarrer un programme*** :

    ```makefile
    C:\Program Files\LianLiProfileWatcher\LianLiProfileWatcher.exe
    ```

    - ***Ajouter des arguments*** (optionnel selon votre méthode utilisée) :

    ```makefile
    --config "D:\<PATH_CONFIG>\appProfiles.json"
    ```

6. Onglet ***Actions***

    - Cochez :  
    [**X**] Autoriser l'exécution de la tâche à la demande  
    [**X**] Arrêter la tâche si elle s'exécute plus de : (3 jours (par défaut))  
    [**X**] Si la tâche en cours ne se termine pas sur demande, forcer son arrêt

7. ***OK*** pour enregistrer.

L’agent se lancera invisible à chaque logon.

### 5.3 - Clé de registre Run (alternative)

1. Ouvrez regedit.
2. Allez à : **`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`**
3. Créez une Valeur chaîne **`LianLiProfileWatcher`** dont la donnée est :

    ```arduino
    "C:\Program Files\LianLiProfileWatcher\LianLiProfileWatcher.exe"
    ```

    À la prochaine connexion, l’agent démarrera.

## 6. Vérification

- Ouvrez ou reconnectez votre session Windows.
- Ouvrez ce fichier de log :

    ```lua
    %LOCALAPPDATA%\LianLiProfileWatcher\Logs\agent.log
    ```

- Changez de fenêtre (Notepad, Chrome, etc.) et vérifiez que le log enregistre :

    ```csharp
    [INFO] Worker    : Fenêtre active détectée : notepad
    [INFO] Worker    : → Application du profil « profile-notion »
    ```

## 7. Désinstallation

### 7.1 - Désinstallation de la tâche planifiée

1. Ouvrez ***Planificateur de tâches***, supprimez la tâche ***LianLiProfileWatcher-Agent***.
2. Supprimez le dossier :

    ```makefile
    C:\Program Files\LianLiProfileWatcher
    ```

### 7.2 - Désinstallation du service Windows

Le Script **PowerShell de désinstallation** ***`Scripts/uninstall-service.ps1`*** :

Exécute ce script ainsi (**depuis le dossier Scripts\\**) :

```powershell
.\uninstall-service.ps1 `
-InstallDir "C:\<MON_PATH>\LianLiProfileWatcher" `
-ServiceName "LianLiProfileWatcher-Agent" `
```

## 8. Architecture déployée

Le schéma d’architecture interne est disponible dans :

```bash
docs/architecture.puml
```

Vous pouvez le visualiser via une extension PlantUML (VSCode) ou un serveur en ligne.

Pour tout autre détail, reportez-vous au [README](README.md) principal.
