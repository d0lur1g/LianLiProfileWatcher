param(
    [string]$InstallDir = "C:\Program Files\LianLiProfileWatcher",
    [string]$ServiceName = "LianLiProfileWatcher"
)

# Répertoire où le script se trouve
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Chemin absolu vers le dossier publish (assumé un niveau au-dessus de Scripts/)
$PublishDir = Join-Path $ScriptDir '..\publish'

if (-not (Test-Path $PublishDir)) {
    Throw "Le dossier publish est introuvable : $PublishDir"
}

# 1. Copier les fichiers publiés
# 1.1 Supprimer l’ancien dossier s’il existe
if (Test-Path $InstallDir) {
    Write-Host "Effacement de l’ancien dossier : $InstallDir"
    Remove-Item -Recurse -Force $InstallDir
}

# 1.2 Créer proprement le dossier d’installation
Write-Host "Création du dossier : $InstallDir"
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

# 1.3 Copier tous les fichiers et sous-dossiers de publish
Write-Host "Copie des fichiers de $PublishDir vers $InstallDir"
Copy-Item -Path (Join-Path $PublishDir '*') `
    -Destination $InstallDir `
    -Recurse -Force

# 2. Créer le service Windows
Write-Host "Création du service $ServiceName"
sc.exe create $ServiceName `
    binPath= "`"$InstallDir\LianLiProfileWatcher.exe`"" `
    DisplayName= "Lian Li Profile Watcher" `
    start= auto

# 3. Démarrer le service
Write-Host "Démarrage du service $ServiceName"
sc.exe start $ServiceName

Write-Host "Installation terminée."
