# Laboratoire Azure et observabilité — mode local de référence

Le laboratoire se réussit sans compte Azure. Le mode local construit le starter, inspecte le plan Bicep et résout un incident à partir dʼune télémétrie entièrement factice. Il ne prétend pas prouver quʼAzure accepterait un déploiement, ni mesurer disponibilité, coût ou performance réels.

## Avertissement coût et confidentialité

Un déploiement Azure réel est facultatif, manuel et peut être facturé. Avant toute création, utilisez un groupe de ressources dédié, choisissez des tailles minimales adaptées, fixez un budget et une heure de suppression, puis vérifiez après suppression quʼaucune ressource facturable ne subsiste. Une alerte de budget ne garantit pas lʼarrêt des dépenses.

Ne placez aucun identifiant Azure, donnée personnelle ou valeur sensible dans ces fichiers, paramètres enregistrés, sorties de commande, captures ou journaux. Les noms de paramètres décrivent uniquement les valeurs à fournir hors du dépôt. Managed Identity et les rôles minimaux remplacent les identifiants applicatifs durables lorsque le service les prend en charge.

## Preuve hors ligne

```powershell
dotnet build content/labs/azure-operations/starter/DeploymentPlan.csproj
powershell -ExecutionPolicy Bypass -File content/labs/azure-operations/Verify-LocalMode.ps1
powershell -ExecutionPolicy Bypass -File content/labs/azure-operations/Resolve-SimulatedIncident.ps1
```

Le plan compare App Service et Container Apps ; il décrit Azure SQL, Storage, Key Vault, Managed Identity, Log Analytics et Application Insights. `main.bicep` est un support dʼinfrastructure inspectable, pas une commande exécutée par Forge.NET. Toute validation réelle doit être annoncée comme manuelle et consigner région, taille, heure de création, propriétaire de suppression et résultat final.

## Suppression dʼune répétition réelle facultative

Vérifiez dʼabord le nom exact du groupe dédié et son contenu. Lancez ensuite la suppression depuis votre propre procédure contrôlée, attendez sa fin et interrogez à nouveau Azure. Ne réutilisez jamais un groupe contenant une ressource personnelle ou partagée.
