[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InputPath,
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference='Stop'
$data=Import-PowerShellDataFile -LiteralPath $InputPath
foreach($required in @('DisplayName','Project','Action','Evidence','Limitation')){
    if([string]::IsNullOrWhiteSpace([string]$data[$required])){throw "Champ requis absent : $required"}
}
$text=($data.Values -join ' ')
if($text -match '[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}' -or $text -match '(?:\+33|0)[1-9](?:[ .-]?\d{2}){4}'){
    throw 'Export refusé : une coordonnée directe semble présente. Minimisez les données avant de recommencer.'
}
$parent=Split-Path -Parent ([System.IO.Path]::GetFullPath($OutputPath))
if([string]::IsNullOrWhiteSpace($parent)){throw 'Le chemin de sortie doit avoir un dossier explicite.'}
[System.IO.Directory]::CreateDirectory($parent) | Out-Null
$markdown="# Preuve de carrière — données à vérifier avant partage`n`n> Document local contenant des données personnelles potentielles. Inspecter contenu, métadonnées et historique avant publication.`n`n- Nom dʼaffichage : $($data.DisplayName)`n- Projet : $($data.Project)`n- Action : $($data.Action)`n- Preuve : $($data.Evidence)`n- Limite : $($data.Limitation)`n`nCe document ne promet ni emploi ni salaire.`n"
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($OutputPath),$markdown,(New-Object System.Text.UTF8Encoding($true)))
Write-Output "EXPORT CRÉÉ : $([System.IO.Path]::GetFullPath($OutputPath))"
