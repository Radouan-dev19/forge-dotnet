# Laboratoire API mini-ERP S11–S16

Ce laboratoire de référence démontre une tranche HTTP réelle sans devenir le projet final de l’apprenant. Il couvre DTO, validation automatique, Problem Details, pagination bornée, annulation, authentification par clés factices injectées et autorisation par politique.

```powershell
dotnet test content/labs/api-mini-erp/tests/ForgeApiLab.Tests/ForgeApiLab.Tests.csproj
```

Les clés d’exécution viennent uniquement de la configuration de test ou de fichiers montés hors Git. Elles ne sont jamais journalisées. Le contrat `openapi.json` est comparé aux routes et statuts testés.
