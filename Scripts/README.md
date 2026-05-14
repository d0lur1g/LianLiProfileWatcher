# Scripts — Export & Import de profils L-Connect 3

Deux scripts PowerShell pour sauvegarder et appliquer des profils d'éclairage
[L-Connect 3](https://www.lian-li.com/l-connect3/) via la ligne de commande ou un
[Stream Deck](https://www.elgato.com/fr/stream-deck).

---

## Vidéo de démonstration

▶️ [https://www.youtube.com/watch?v=U2O3vEDqYT0](https://www.youtube.com/watch?v=U2O3vEDqYT0)

---

## Scripts disponibles

| Script | Rôle |
|---|---|
| `lian_li_export.ps1` | Sauvegarde la configuration L-Connect 3 courante dans un dossier |
| `lian_li_import.ps1` | Applique un profil sauvegardé et redémarre le service L-Connect 3 |

---

## Prérequis

- **Windows 10 / 11**
- **L-Connect 3** installé et en cours d'exécution
- **PowerShell 5+** (intégré à Windows)
- Droits administrateur (requis par `lian_li_import.ps1` pour redémarrer le service)

---

## Utilisation

### `lian_li_export.ps1` — Exporter un profil

Copie les dossiers `device\` et `profile\` depuis le répertoire de données
L-Connect 3 vers le dossier courant.

```powershell
.\lian_li_export.ps1 -origin 'C:\ProgramData\Lian-Li\L-Connect 3'
```

| Paramètre | Description |
|---|---|
| `-origin` | Chemin du dossier de données L-Connect 3 |

---

### `lian_li_import.ps1` — Appliquer un profil

Remplace les dossiers `device\` et `profile\` dans le répertoire de données
L-Connect 3 par ceux du profil source, puis redémarre `LConnectService`.

Le script se relance automatiquement en administrateur via UAC si nécessaire.

```powershell
.\lian_li_import.ps1 -origin '<dossier-profil>' -destination 'C:\ProgramData\Lian-Li\L-Connect 3'
```

| Paramètre | Description |
|---|---|
| `-origin` | Dossier contenant le profil à appliquer (avec `device\` et `profile\`) |
| `-destination` | Chemin du dossier de données L-Connect 3 |

---

## Guide pas à pas — Utilisation avec Stream Deck

### Étape 1 — Activer l'exécution des scripts PowerShell

Ouvre PowerShell en administrateur et exécute :

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Étape 2 — Télécharger les scripts

Télécharge les fichiers `lian_li_export.ps1` et `lian_li_import.ps1` depuis
ce dépôt (ou le lien Google Drive mentionné dans la vidéo).

### Étape 3 — Configurer l'éclairage dans L-Connect 3

Configure les couleurs et effets de ton boîtier PC dans L-Connect 3 comme
tu le souhaites.

### Étape 4 — Créer un dossier de profil et y placer le script d'export

Crée un dossier dédié (ex. `C:\LianLiProfiles\gaming\`), puis copies-y
le script `lian_li_export.ps1`.

### Étape 5 — Exporter le profil courant

Depuis ce dossier, exécute :

```powershell
.\lian_li_export.ps1 -origin 'C:\ProgramData\Lian-Li\L-Connect 3'
```

Les sous-dossiers `device\` et `profile\` sont copiés dans ton dossier de profil.

### Étape 6 — (Optionnel) Créer un second profil

Répète les étapes 3 à 5 avec une autre configuration d'éclairage et un dossier
différent (ex. `C:\LianLiProfiles\work\`).

### Étape 7 — Ajouter une action dans Stream Deck

Ouvre le logiciel **Stream Deck** et ajoute l'action **Open** (catégorie *System*)
sur un bouton.

### Étape 8 — Préparer la commande

Copie la commande ci-dessous dans un bloc-notes et remplace les valeurs
entre `< >` :

```
cmd /c start /min "" powershell -WindowStyle Hidden -executionpolicy bypass -noninteractive <chemin-du-script>\lian_li_import.ps1 -origin '<dossier-où-le-profil-a-été-enregistré>' -destination 'C:\ProgramData\Lian-Li\L-Connect 3'
```

**Exemple concret :**

```
cmd /c start /min "" powershell -WindowStyle Hidden -executionpolicy bypass -noninteractive C:\LianLiProfiles\gaming\lian_li_import.ps1 -origin 'C:\LianLiProfiles\gaming' -destination 'C:\ProgramData\Lian-Li\L-Connect 3'
```

### Étape 9 — Coller la commande dans Stream Deck

Colle la commande complète dans le champ **App / File** de l'action Open.

### Étape 10 — Configurer un second bouton

Pour un deuxième profil, duplique le bouton et change uniquement le chemin
`-origin` pour pointer vers le dossier du second profil.

### Étape 11 — Tester

Appuie sur les boutons du Stream Deck pour vérifier que l'éclairage change
correctement.

> **Note :** le changement de profil prend quelques secondes — le script
> copie les fichiers puis redémarre le service L-Connect 3.

---

## Structure attendue des dossiers de profils

```
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

---

## Dépannage

**`lian_li_import.ps1` ne fait rien après le double-clic**
→ Le script se relance en administrateur (fenêtre masquée). Attends 5-10 secondes.

**L'éclairage ne change pas après exécution**
→ Vérifie que `-destination` pointe bien vers `C:\ProgramData\Lian-Li\L-Connect 3`
  et que les sous-dossiers `device\` et `profile\` existent dans ton dossier `-origin`.

**Erreur `UnauthorizedAccess` ou `Access denied`**
→ Exécute PowerShell en tant qu'administrateur, ou laisse le script déclencher
  l'élévation UAC automatiquement.

**Le service `LConnectService` est introuvable**
→ Vérifie que L-Connect 3 est installé et en cours d'exécution :
  `Get-Service LConnectService`
