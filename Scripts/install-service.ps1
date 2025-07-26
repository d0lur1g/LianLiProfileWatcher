param(
    [string]$InstallDir = "C:\Program Files\LianLiProfileWatcher",
    [string]$ServiceName = "LianLiProfileWatcher"
)

# 1. Copier les fichiers
if (Test-Path $InstallDir) {
    Write-Host "Répertoire existe déjà, on écrase : $InstallDir"
    Remove-Item -Recurse -Force $InstallDir
}
Copy-Item -Path ".\publish\*" -Destination $InstallDir -Recurse

# 2. Créer le service
sc.exe create $ServiceName `
    binPath= "`"$InstallDir\LianLiProfileWatcher.exe`"" `
    DisplayName= "Lian Li Profile Watcher" `
    start= auto

# 3. Démarrer le service
sc.exe start $ServiceName

Write-Host "Service '$ServiceName' installé et démarré."
