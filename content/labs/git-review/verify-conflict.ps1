[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) ("ForgeDotNet-GitLab-" + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($sandbox) | Out-Null
try {
    Push-Location $sandbox
    & git init --initial-branch=main
    & git config user.name 'Forge Lab'
    & git config user.email 'forge-lab@example.invalid'
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=true`nauthorization=true`n")
    & git add policy.txt
    & git commit -m 'Ajoute la politique de base'
    & git switch -c validation
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=bounded`nauthorization=true`n")
    & git commit -am 'Borne la validation'
    & git switch main
    & git switch -c authorization
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=true`nauthorization=policy`n")
    & git commit -am 'Applique la politique d’autorisation'
    & git merge validation
    if ($LASTEXITCODE -eq 0) { throw 'Le conflit attendu n’a pas été produit.' }
    $conflicted = [System.IO.File]::ReadAllText((Join-Path $sandbox 'policy.txt'))
    if (-not $conflicted.Contains('<<<<<<<') -or -not $conflicted.Contains('>>>>>>>')) { throw 'Les marqueurs de conflit sont absents.' }
    [System.IO.File]::WriteAllText((Join-Path $sandbox 'policy.txt'), "validation=bounded`nauthorization=policy`n")
    & git add policy.txt
    & git commit -m 'Résout les politiques sans perte'
    $resolved = [System.IO.File]::ReadAllText((Join-Path $sandbox 'policy.txt'))
    if ($resolved -ne "validation=bounded`nauthorization=policy`n") { throw 'La résolution a perdu une exigence.' }
}
finally {
    Pop-Location
    if (Test-Path -LiteralPath $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
}
