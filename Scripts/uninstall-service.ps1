param(
    [string]$InstallDir = "C:\<MON_PATH>\LianLiProfileWatcher",
    [string]$ServiceName = "LianLiProfileWatcher-Agent"
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
