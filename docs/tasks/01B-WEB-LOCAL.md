# Incrément 01B — Web local

## 1. Statut

Validé. Le profil reste volontairement en mémoire jusqu'à `01C`.

## 2. Objectif

Livrer l'application Blazor locale navigable avec accueil, tableau de bord factuel, profil, paramètres, à-propos, erreurs et health check.

## 3. Contexte à lire

`AGENTS.md`, `PRODUCT_SPEC.md`, `ARCHITECTURE.md`, `SECURITY.md`, `ROADMAP.md`, fiches `00` et `01A`.

## 4. Prérequis

`01A` validé ; build et tests verts.

## 5. Périmètre inclus

Routes `/`, `/dashboard`, `/profile`, `/settings`, `/about`, `/health` ; navigation responsive/clavier ; profil et contrat ; dépôt mémoire abstrait ; logs structurés ; erreurs sans détails sensibles.

## 6. Périmètre explicitement exclu

EF Core, SQLite, migration, sauvegarde, contenu, diagnostic, scores, runners, SQL Server, Docker et Azure.

## 7. Fichiers ou projets principalement concernés

Modules `IdentityLocal` de Domain/Application/Infrastructure, `ForgeDotNet.Web`, tests Unit/Integration/EndToEnd et `README.md`.

## 8. Étapes d'implémentation

Créer le modèle validé ; définir repository et cas d'usage ; implémenter stockage mémoire ; composer DI/health ; créer pages/navigation ; traiter 404/500 ; tester au navigateur ; documenter la volatilité.

## 9. Règles d'architecture

Validation dans Domain, orchestration dans Application, stockage dans Infrastructure, rendu/composition dans Web ; l'UI ne calcule aucun faux indicateur.

## 10. Règles de sécurité

Mono-utilisateur sans mot de passe ; aucune donnée sensible dans les logs ; antiforgery Blazor ; erreurs corrélées ; pas de télémétrie externe.

## 11. Tests à écrire

Création/défauts/validation du profil, contrat accepté/refusé, dépôt et cas d'usage partagés, démarrage, cinq pages, health, navigation et 404 utile.

## 12. Tests manuels à effectuer

Lancer l'app ; parcourir toutes les routes à la souris/clavier ; saisir/enregistrer le profil ; vérifier dashboard, contrat refusé/accepté, focus, mobile raisonnable, console et `/health`.

## 13. Commandes de vérification

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## 14. Critères d'acceptation

Toutes les routes correctes, profil temporaire fonctionnel, contrat honnête, dashboard sans métrique fictive, health 200, tests verts et accessibilité de base démontrée.

## 15. Conditions d'arrêt

Socle 01A cassé, persistance nécessaire pour finir, erreur console bloquante, donnée fictive, détail sensible exposé ou vérification en échec.

## 16. Mise à jour attendue de la roadmap

Cocher uniquement `01B`; désigner `01C` comme prochain incrément.

## 17. Format obligatoire du rapport final

Fichiers ; fonctionnalités ; cas d'usage ; stockage ; routes ; tests ; résultats exacts ; test manuel ; avertissements ; limites ; confirmation d'absence de SQLite/01C.
