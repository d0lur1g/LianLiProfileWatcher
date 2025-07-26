# Guide de déploiement

## 1. Prérequis

- Windows 10/11 x64
- .NET 9.0 Runtime

## 2. Publication

```powershell
dotnet publish .\LianLiProfileWatcher.csproj -c Release -r win-x64 --self-contained false -o publish
```

## 3. Installation

> ### Avant de démarrer
>
>1. Renommez `Config/appProfiles.example.json` en `Config/appProfiles.json`.
>2. Adaptez les valeurs selon votre installation locale:
>    - `baseFolder`
>    - `destination`
>    - `scriptPath`
>    - `default`
>    - `profiles\apps`
>3. Ne commit jamais `Config/appProfiles.json` — il est ignoré par Git.

### 3.1 Copier les fichiers

1. Ouvrez un Explorateur de fichiers et naviguez jusqu’à `publish/`.
2. Copiez tous les fichiers et dossiers dans :

```makefile
C:\Program Files\LianLiProfileWatcher
```

### 3.2 Configurer la tâche planifiée

1. Lancez Planificateur de tâches (`taskschd.msc`).
2. Cliquez sur Créer une tâche….
3. Dans l’onglet Général :
    - Nom : `LianLiProfileWatcher-Agent`
    - Cochez « **Masquer** »
    - Sélectionnez « **Exécuter que l’utilisateur soit connecté ou non** »
4. Dans l’onglet Déclencheurs :
    - Nouveau… → Début de la tâche : À l’ouverture de session
5. Dans l’onglet Actions :
    - Nouvelle…
        - Programme/script :

            ```makefile
            C:\Program Files\LianLiProfileWatcher\LianLiProfileWatcher.exe
            ```

        - Démarrer dans (optionnel) :

            ```makefile
            C:\Program Files\LianLiProfileWatcher
            ```

6. Cliquez sur OK pour enregistrer la tâche.

## 4. Vérification

1. Ouvrez (ou reconnectez) votre session Windows.
2. Consultez le fichier de log :

    ```lua
    %LOCALAPPDATA%\LianLiProfileWatcher\Logs\agent.log
    ```

3. Changez de fenêtre (Notepad, Chrome, etc.) et vérifiez que des lignes comme :

    ```csharp
    [INFO] Worker   : Fenêtre active détectée : notepad
    [INFO] Worker   : → Application du profil « profile-notion »
    ```

sont bien ajoutées.

## 5. Désinstallation

1. Ouvrez Planificateur de tâches.
2. Sélectionnez la tâche `LianLiProfileWatcher-Agent`, puis Supprimer.
3. Supprimez le dossier :

    ```makefile
    C:\Program Files\LianLiProfileWatcher
    ```

## 6. Architecture du déploiement

Le schéma PlantUML détaillant la logique interne se trouve dans :

```bash
docs/architecture.puml
```

Vous pouvez le visualiser via une extension PlantUML (VSCode) ou un serveur en ligne.
