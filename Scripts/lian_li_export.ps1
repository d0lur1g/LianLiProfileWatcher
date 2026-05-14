<#
.SYNOPSIS
    Exporte la configuration courante de L-Connect 3.

.DESCRIPTION
    Copie les dossiers "device" et "profile" depuis le répertoire de données
    de L-Connect 3 vers le répertoire courant, afin de sauvegarder un profil
    d'éclairage. Le résultat peut ensuite être importé via "lian_li_import.ps1".

.PARAMETER origin
    Chemin du dossier de données L-Connect 3
    (par défaut : C:\ProgramData\Lian-Li\L-Connect 3)

.EXAMPLE
    .\lian_li_export.ps1 -origin "C:\ProgramData\Lian-Li\L-Connect 3"
#>

param (
    [string]$origin
)

# Vérifie que le paramètre obligatoire -origin a bien été fourni
if (-not $origin) {
    Write-Host "ERREUR : Vous devez fournir le chemin du dossier source."
    exit 1
}

Write-Host "Chemin source : $origin"

# Vérifie que le chemin fourni existe et est bien un dossier
if (Test-Path $origin -PathType Container) {

    # Liste des sous-dossiers à exporter depuis le dossier de données L-Connect
    $targetFolders = @("device", "profile")

    foreach ($folderName in $targetFolders) {
        $sourcePath = Join-Path -Path $origin   -ChildPath $folderName
        $destination = Join-Path -Path $PWD.Path -ChildPath $folderName

        if (Test-Path $sourcePath -PathType Container) {
            # Copie récursive du sous-dossier vers le répertoire courant
            Write-Host "Copie de '$folderName' vers '$destination'..."
            Copy-Item -Path $sourcePath -Destination $destination -Recurse -Force
        }
        else {
            # Avertissement si le sous-dossier attendu est absent (installation non standard)
            Write-Host "ATTENTION : Le dossier '$folderName' n'existe pas dans '$origin'."
        }
    }

}
else {
    # Le chemin source est inexistant ou n'est pas un dossier
    Write-Host "ERREUR : Le chemin source spécifié n'existe pas ou n'est pas un dossier."
    exit 1
}