<#
.SYNOPSIS
    Applique un profil L-Connect 3 en remplaçant la configuration active.

.DESCRIPTION
    Supprime les dossiers "device" et "profile" dans le répertoire de données
    de L-Connect 3, les remplace par ceux du profil source, puis redémarre
    le service LConnectService pour que les changements prennent effet.

    Si le script n'est pas exécuté en administrateur, il se relance
    automatiquement avec élévation de privilèges (UAC).

.PARAMETER origin
    Chemin du dossier contenant le profil à appliquer
    (doit contenir les sous-dossiers "device" et "profile")

.PARAMETER destination
    Chemin du dossier de données L-Connect 3
    (par défaut : C:\ProgramData\Lian-Li\L-Connect 3)

.EXAMPLE
    .\lian_li_import.ps1 -origin "C:\LianLiProfiles\gaming" -destination "C:\ProgramData\Lian-Li\L-Connect 3"
#>

# Les chemins source et destination sont reçus en paramètres :
#   -origin      : dossier contenant le profil à appliquer
#   -destination : dossier de données L-Connect 3 (cible du remplacement)
param (
    [string]$origin,
    [string]$destination
)

# Vérifie que les deux paramètres obligatoires ont bien été fournis
if (-not $origin -or -not $destination) {
    Write-Host "ERREUR : Vous devez fournir les chemins source et destination."
    Write-Host "Usage : .\lian_li_import.ps1 -origin <chemin_profil> -destination <chemin_lconnect>"
    Read-Host "Appuyez sur Entrée pour quitter"
    exit 1
}

# Vérifie si le script est exécuté avec des droits administrateur.
# Si non, le relance silencieusement avec élévation UAC en passant les mêmes paramètres.
if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -origin `"$origin`" -destination `"$destination`"" -Verb RunAs -WindowStyle Hidden
    exit
}

# Supprime les dossiers "device" et "profile" dans le répertoire de destination
# (nécessaire avant la copie pour éviter les conflits de fichiers résiduels)
Remove-Item -Path "$destination\device"  -Recurse -Force
Remove-Item -Path "$destination\profile" -Recurse -Force

# Copie les dossiers "device" et "profile" du profil source vers la destination
Copy-Item -Path "$origin\device"  -Destination "$destination\" -Recurse -Force
Copy-Item -Path "$origin\profile" -Destination "$destination\" -Recurse -Force

# Redémarre le service Lian Li Connect pour recharger la configuration
Restart-Service -Name LConnectService