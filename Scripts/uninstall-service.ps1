param(
    [string]$InstallDir = "C:\Program Files\LianLiProfileWatcher",
    [string]$ServiceName = "LianLiProfileWatcher"
)

# 1. Arrêter le service
sc.exe stop $ServiceName

# 2. Supprimer le service
sc.exe delete $ServiceName

# 3. Optionnel : supprimer les fichiers
if (Test-Path $InstallDir) {
    Remove-Item -Recurse -Force $InstallDir
    Write-Host "Répertoire supprimé : $InstallDir"
}

Write-Host "Service '$ServiceName' désinstallé."
