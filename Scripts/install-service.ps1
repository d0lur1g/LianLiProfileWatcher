param(
    [string]$InstallDir = "C:\<MON_PATH>\LianLiProfileWatcher",
    [string]$ServiceName = "LianLiProfileWatcher-Agent",
    [string]$ConfigPath = "D:\<PATH_CONFIG>\appProfiles.json"
)

# 1. Déterminer les dossiers
$ScriptDir = Split-Path $MyInvocation.MyCommand.Definition -Parent
$PublishDir = Join-Path $ScriptDir '..\publish'

# 2. Nettoyage de l’ancien installDir
if (Test-Path $InstallDir) {
    Remove-Item $InstallDir -Recurse -Force
    Write-Host "Nettoyage de l'ancien dossier d'installation '$InstallDir' effectué."
}
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Write-Host "Nouveau dossier d'installation créé à '$InstallDir'."

# 3. Copier les binaires
Copy-Item (Join-Path $PublishDir '*') $InstallDir -Recurse -Force
Write-Host "Binaries copiés de '$PublishDir' vers '$InstallDir'."

# 4. Construire la chaîne de lancement avec --config
$exePath = Join-Path $InstallDir 'LianLiProfileWatcher.exe'
$binPath = "`"$exePath`" --config `"$ConfigPath`""
Write-Host "Chaîne de lancement construite : $binPath"

# 5. Créer le service Windows (PowerShell) — gère mieux le quoting
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Le service '$ServiceName' existe déjà, suppression..."
    Stop-Service   -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Write-Host "Service '$ServiceName' arrêté."
    sc.exe delete  $ServiceName
    # Attendre que le service soit totalement supprimé :
    Write-Host "Attente de la suppression définitive du service..."
    $maxWait = 30   # secondes
    $elapsed = 0
    while (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
        Start-Sleep -Seconds 1
        $elapsed++
        if ($elapsed -ge $maxWait) {
            Throw "Le service est toujours marqué pour suppression après $maxWait s. Veuillez redémarrer ou attendre."
        }
    }
    Write-Host "Service supprimé, on peut recréer maintenant."
}

New-Service `
    -Name        $ServiceName `
    -BinaryPathName $binPath `
    -DisplayName "LianLiProfileWatcher-Agent" `
    -Description "Hook WinEvent & application de profils LianLi selon l'appli active" `
    -StartupType Automatic
    
Write-Host "Service '$ServiceName' créé avec la chaîne de lancement '$binPath'."

# 6. Démarrer le service
Start-Service -Name $ServiceName

Write-Host "Service '$ServiceName' installé et démarré avec config '$ConfigPath'."
