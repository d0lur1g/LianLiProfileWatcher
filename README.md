
# Contexte et Besoin Fonctionnel

## 🧩 **Objectif général**

Le service `LianLiProfileWatcher` a pour objectif de :

> Surveiller en continu l'application Windows actuellement active (focus utilisateur) et appliquer dynamiquement un profil de configuration personnalisé pour L-Connect 3 en copiant des fichiers de profil dans un dossier spécifique et en temps réel.
> 

---

## 🛠️ **Fonctionnalités détaillées**

### 1. 🧠 **Chargement de la configuration**

- **Fichier :** `Config/appProfiles.json`
- **Contenu :**
    - `baseFolder`: Répertoire où sont stockés les profils (`E:\Programmes\Lian Li - L-Connect 3`)
    - `destination`: Dossier dans lequel copier le profil actif (`C:\Program Files\Lian-Li\L-Connect 3\appdata`)
    - `default`: Nom du profil par défaut à appliquer si aucun profil spécifique n’est défini pour l’application
    - `profiles`: Dictionnaire `application -> profil`

✅ **But** : charger une seule fois ce fichier au démarrage, via un service dédié (`ConfigurationService`), et rendre les données disponibles en lecture seule.

---

### 2. 🪟 **Détection de changement de fenêtre active**

- **Technologie utilisée :** `SetWinEventHook` (API Win32) avec l’événement `EVENT_SYSTEM_FOREGROUND`
- **Principe :**
    - Hook global installé sur un **thread STA** (Single Threaded Apartment)
    - Lorsqu’une fenêtre devient active, la **callback `WinEventProc`** est appelée
    - Le nom du **processus** de la fenêtre est récupéré via `GetForegroundWindow` → `GetWindowThreadProcessId` → `Process.GetProcessById(...)`

✅ **But** : capturer en temps réel le changement d’application active (focus), tout en **filtrant les répétitions inutiles** (cf. debounce).

---

### 3. 🗺️ **Résolution du profil à appliquer**

- À chaque détection d’une nouvelle fenêtre :
    1. On extrait le **nom du processus**, sans extension (`chrome`, `winword`, etc.)
    2. On vérifie s’il existe une **entrée dans le dictionnaire `profiles`**
        - Si oui → appliquer le profil associé
        - Si non → appliquer le profil `default`

✅ **But** : lier chaque application à un **profil visuel personnalisé**, ou revenir à un mode générique (`rainbow`, `default`, etc.)

---

### 4. 📁 **Application du profil**

- **Principe :**
    - Un **profil** est un dossier situé dans `baseFolder`
    - Ce dossier contient **les fichiers de configuration spécifiques à L-Connect 3**
    - Pour appliquer un profil :
        - Supprimer ou écraser les fichiers présents dans `destination`
        - Copier récursivement les fichiers depuis `baseFolder\<profile>` vers `destination`
    - Optionnellement, si L-Connect 3 a besoin d’un "trigger", tu peux l’ajouter plus tard (ex : relancer un service, modifier un fichier `flag`, etc.)

✅ **But** : rendre actif le style lumineux défini par l’utilisateur pour l’application en cours.

---

### 5. 🔁 **Détection et debounce**

- **Comportement attendu :**
    - Ne pas réappliquer un profil si l’utilisateur revient sur la même fenêtre (éviter de flooder la copie)
    - Ne pas répondre à des fenêtres système parasites ou invisibles

✅ **But** : éviter les traitements inutiles et optimiser les performances du service.

---

## 🧱 **Architecture et structure du projet**

| Dossier / Composant | Rôle |
| --- | --- |
| `Models/AppProfileConfig.cs` | Représente la structure du fichier JSON de configuration |
| `Services/ConfigurationService.cs` | Lit, parse et expose la configuration de manière centralisée |
| `Services/ProfileManager.cs` | Contient la logique de copie du profil actif vers le dossier cible |
| `Worker.cs` | Point d’entrée principal du service. Installe le hook, déclenche la détection et appelle `ProfileManager` |

---

## 🚦 **Contraintes et exigences techniques**

- ✅ Le service doit être **exclusivement Windows** (API Win32)
- 🔐 Il doit avoir les droits d’écriture dans le dossier `destination`
- 💡 Il doit **résister à une erreur d’accès disque ou à une application inconnue**
- 🧪 Il doit être **testable** (via logs, configuration isolée, logs de changement de profil)

---

## 🌲 **TREE FOLDER**

```
LianLiProfileWatcher/
│
├── Application/
│   ├── Interfaces/
│       ├── IConfigurationService.cs
│       ├── IProfileApplier.cs
├── Config/
│   ├── appProfiles.json
├── Infrastructure/
│   ├── Appliers/
│       ├── ProfileApplier.cs
├── Models/
│   ├── AppProfileConfig.cs
├── Services/
│   ├── ConfigurationService.cs
├── Program.cs
└── Worker.cs
```
