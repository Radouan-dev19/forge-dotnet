# Incrément 01A — Solution

## 1. Statut

Validé. Ne pas modifier son résultat hors correction nécessaire à un incrément autorisé.

## 2. Objectif

Créer le squelette .NET 10 compilable et testable, ses projets, dépendances, conventions et script de vérification, sans fonctionnalité métier.

## 3. Contexte à lire

`AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/tasks/00-CONCEPTION.md`.

## 4. Prérequis

`00` validé ; SDK .NET 10 installé ; dépôt propre ou changements utilisateur identifiés.

## 5. Périmètre inclus

Solution et huit projets prévus, Blazor minimal, xUnit, références autorisées, `.editorconfig`, props centralisées, `global.json`, `.gitignore`, README et `scripts/verify.ps1`.

## 6. Périmètre explicitement exclu

Profil, navigation métier, EF Core/SQLite, contenu, diagnostic, runners, SQL Server et Docker Compose.

## 7. Fichiers ou projets principalement concernés

`ForgeDotNet.sln`, `src/*/*.csproj`, `tests/*/*.csproj`, fichiers racine de conventions, `scripts/verify.ps1`, `README.md`.

## 8. Étapes d'implémentation

Contrôler le SDK ; générer les projets sans restauration implicite si nécessaire ; ajouter à la solution ; poser les références ; centraliser les packages ; ajouter des marqueurs/test de frontières ; documenter les commandes.

## 9. Règles d'architecture

Domain sans référence ; Application seulement Domain ; Infrastructure Application+Domain ; CodeRunner sans référence tant qu'aucun contrat ne la justifie ; Web composition ; aucune boucle.

## 10. Règles de sécurité

Aucun secret ou configuration locale suivie ; analyseurs activés ; dépendances explicites et versions centralisées ; aucune exécution de code soumis.

## 11. Tests à écrire

Tests structurels vérifiant découverte xUnit, absence de dépendance Domain et références autorisées des assemblages ; aucun test trivial.

## 12. Tests manuels à effectuer

Inspecter `dotnet sln list`, toutes les références projet et l'absence de fichiers de fonctionnalités anticipées.

## 13. Commandes de vérification

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet sln ForgeDotNet.sln list
```

## 14. Critères d'acceptation

Huit projets listés, références conformes, build/test/format verts, zéro avertissement non justifié et script propageant tout code d'échec.

## 15. Conditions d'arrêt

.NET 10 absent, restauration impossible après vérification réseau autorisée, référence circulaire, changement utilisateur incompatible ou commande finale en échec.

## 16. Mise à jour attendue de la roadmap

Cocher uniquement `01A` après preuves ; laisser `01B` et suivants ouverts.

## 17. Format obligatoire du rapport final

Fichiers ; structure ; références par projet ; commandes et sorties ; tests réussis/échoués ; avertissements ; décisions ; écarts ; confirmation que `01B` n'a pas commencé.
