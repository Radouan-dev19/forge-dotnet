# Incrément 01C — Persistance

## 1. Statut

Validé le 25 juillet 2026. Migration, persistance, health check et sauvegarde/restauration démontrés ; 35 tests automatisés verts.

## 2. Objectif

Remplacer le stockage volatile du profil par SQLite/EF Core, fournir migration contrôlée, petite donnée de démonstration non trompeuse et sauvegarde/restauration de base.

## 3. Contexte à lire

`AGENTS.md`, `PRODUCT_SPEC.md`, sections Persistance de `ARCHITECTURE.md`, `SECURITY.md`, `ROADMAP.md`, code `IdentityLocal`, fiches `01A` et `01B`.

## 4. Prérequis

`00`, `01A`, `01B` validés ; application et tests verts ; décision confirmée sur le chemin local de données.

## 5. Périmètre inclus

Packages EF Core SQLite compatibles .NET 10 ; `DbContext` Infrastructure ; mapping du profil ; migration initiale ; repository SQLite ; création contrôlée ; sauvegarde cohérente et restauration validée sur copie ; health check SQLite ; test seed explicitement démo si utile.

## 6. Périmètre explicitement exclu

Docker Compose, SQL Server, contenu pédagogique, tentatives, diagnostic, scores et nouvelles pages produit hors gestion nécessaire de la persistance.

## 7. Fichiers ou projets principalement concernés

`ForgeDotNet.Infrastructure`, composition/configuration Web, tests Integration/EndToEnd, `Directory.Packages.props`, `README.md`, futur dossier de migrations Infrastructure.

## 8. Étapes d'implémentation

1. Définir options/chemin hors répertoire publié. 2. Ajouter DbContext et mapping sans contaminer Domain. 3. Créer migration initiale. 4. Remplacer l'inscription mémoire. 5. Appliquer migration explicitement en développement. 6. Implémenter backup via API SQLite/checkpoint, manifeste/checksum et restauration sur copie. 7. Étendre health et documentation.

## 9. Règles d'architecture

Interfaces dans Application, entités/règles dans Domain, EF/migrations dans Infrastructure, Web seulement composition ; pas de modèle EF exposé à l'UI.

## 10. Règles de sécurité

Chemin canonicalisé et non servi ; pas de donnée sensible dans logs ; restauration refuse traversal, version/checksum invalides et base corrompue ; écriture atomique ; migration automatique seulement en développement contrôlé.

## 11. Tests à écrire

Mapping aller-retour, profil persistant après nouveau scope/processus de test, migration sur base vide, idempotence, concurrence simple, health, backup cohérent, restauration valide et refus archive/checksum/corruption invalides.

## 12. Tests manuels à effectuer

Créer profil, arrêter/redémarrer, vérifier conservation ; sauvegarder, modifier, restaurer ; lancer sur installation vide ; inspecter qu'aucun fichier DB n'est suivi ou servi publiquement.

## 13. Commandes de vérification

```powershell
dotnet restore
dotnet ef migrations list --project src/ForgeDotNet.Infrastructure --startup-project src/ForgeDotNet.Web
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

## 14. Critères d'acceptation

Base créée par migration, profil durable, migration reproductible, health pertinent, backup/restauration démontrés, installation vide réussie et aucune perte silencieuse.

## 15. Conditions d'arrêt

Migration destructive non approuvée, chemin de données ambigu, corruption non détectée, test de restauration en échec, secret/donnée suivi ou besoin d'entamer `01D`.

## 16. Mise à jour attendue de la roadmap

Après toutes les preuves, cocher `01C` seulement et mettre `NEXT_TASK.md` sur `01D`.

## 17. Format obligatoire du rapport final

Schéma/migrations ; fichiers ; chemin DB ; stratégie backup/restore ; tests et commandes exactes ; preuve redémarrage ; avertissements ; risques ; confirmation que `01D` n'a pas commencé.
