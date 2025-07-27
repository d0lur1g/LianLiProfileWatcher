# Guide de déploiement

Ce document décrit pas à pas comment publier, installer, configurer et désinstaller l’agent **LianLiProfileWatcher**, en intégrant le mécanisme de configuration externe.

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

Pour générer l’archive ZIP de la version v<major>.<minor>.<patch> :

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

L’agent peut charger **une seule** configuration JSON, dont l’emplacement est résolu dans cet ordre :

1. **Argument CLI**

    ```powershell
    .\LianLiProfileWatcher.exe --config "D:\Configs\appProfiles.json"
    ```

2. **Variable d’environnement**

    ```powershell
    setx LIANLI_CONFIG_PATH "D:\Configs\appProfiles.json"
    ```

3. **Fichier local**

    ```shell
    %LOCALAPPDATA%\LianLiProfileWatcher\Config\appProfiles.json
    ```

4. **Template intégré (fallback)**

    ```bash
    Config/appProfiles.example.json
    ```

    >⚙️ **Remarque** :  
    > - Ne pas modifier ``appProfiles.example.json`` dans le dossier ``publish/`` → C’est un template versionné.  
    > - Créez et éditez uniquement votre propre ``appProfiles.json`` selon l’une des méthodes ci-dessous.

### Créer ou pointer votre fichier de config

- **Via CLI** – pas de copie nécessaire :

    ```powershell
    .\LianLiProfileWatcher.exe --config "D:\MesConfigs\appProfiles.json"
    ```

- **Via variable d’environnement** – sans déplacer de fichiers :

    ```powershell
    setx LIANLI_CONFIG_PATH "D:\MesConfigs\appProfiles.json"
    ```

- **Via LocalAppData** – copier le template pour éditer :

    ```powershell
    mkdir "$Env:LOCALAPPDATA\LianLiProfileWatcher\Config" -Force
    copy .\publish\Config\appProfiles.example.json `
        "$Env:LOCALAPPDATA\LianLiProfileWatcher\Config\appProfiles.json"
    ```

- **Sinon**, éditez directement publish\Config\appProfiles.example.json puis copiez-le à l’un des emplacements ci-dessus.

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
>3. Ne commit jamais `Config/appProfiles.json` — il est ignoré par Git.

### 5.1 Copier les fichiers

1. Créez le dossier d’installation, par exemple :

    ```makefile
    C:\Program Files\LianLiProfileWatcher
    ```

2. Copiez **tout** le contenu de `publish/` (mais pas votre `appProfiles.json` perso) dans ce dossier.

### 5.2 Configurer une tâche planifiée (recommandé)

1. Ouvrez ***Planificateur de tâches*** (`askschd.msc`).
2. Cliquez sur ***Créer une tâche…***.
3. ***Général*** :
    - Nom : `LianLiProfileWatcher-Agent`
    - Cochez ***Masquer***
    - Sélectionnez ***Exécuter que l’utilisateur soit connecté ou non***
4. ***Déclencheurs*** → ***Nouveau…*** :
    - ***À l’ouverture de session***
5. ***Actions*** → ***Nouvelle…*** :
    - ***Programme/script*** :

    ```makefile
    C:\Program Files\LianLiProfileWatcher\LianLiProfileWatcher.exe
    ```

    - ***Démarrer dans*** (optionnel) :

    ```makefile
    C:\Program Files\LianLiProfileWatcher
    ```

6. ***OK*** pour enregistrer.

L’agent se lancera invisible à chaque logon.

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

1. Ouvrez ***Planificateur de tâches***, supprimez la tâche ***LianLiProfileWatcher-Agent***.
2. Supprimez le dossier :

    ```makefile
    C:\Program Files\LianLiProfileWatcher
    ```

## 8. Architecture déployée

Le schéma d’architecture interne est disponible dans :

```bash
docs/architecture.puml
```

Vous pouvez le visualiser via une extension PlantUML (VSCode) ou un serveur en ligne.

Pour tout autre détail, reportez-vous au [README](README.md) principal.
