[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$bicep=Get-Content -Raw (Join-Path $root 'infra/main.bicep')
foreach($proof in @('Microsoft.Web/sites@','Microsoft.App/containerApps@','Microsoft.Sql/servers@','Microsoft.Storage/storageAccounts@','Microsoft.KeyVault/vaults@','Microsoft.Insights/components@','SystemAssigned','allowSharedKeyAccess: false','publicNetworkAccess: ''Disabled''')){
    if($bicep.IndexOf($proof,[System.StringComparison]::Ordinal) -lt 0){throw "Preuve IaC absente : $proof"}
}
foreach($forbidden in @('BEGIN PRIVATE KEY','AccountKey=','SharedAccessSignature=','ghp_','sk-live-','sk-proj-')){
    if($bicep.IndexOf($forbidden,[System.StringComparison]::OrdinalIgnoreCase) -ge 0){throw "Valeur interdite détectée : $forbidden"}
}
dotnet build (Join-Path $root 'starter/DeploymentPlan.csproj') --nologo
if($LASTEXITCODE -ne 0){throw "Le starter local ne construit pas : code $LASTEXITCODE."}
dotnet run --project (Join-Path $root 'starter/DeploymentPlan.csproj') --no-build -- simulate
if($LASTEXITCODE -ne 0){throw "Le mode local simulé échoue : code $LASTEXITCODE."}
& (Join-Path $root 'Resolve-SimulatedIncident.ps1')
if($LASTEXITCODE -ne 0){throw "La résolution dʼincident simulé échoue : code $LASTEXITCODE."}
Write-Output 'MODE LOCAL VALIDÉ : aucune ressource Azure créée.'
