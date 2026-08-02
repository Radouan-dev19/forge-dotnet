# Incrément 02B — Catalogue

## 1. Statut

Validé le 25 juillet 2026. Snapshot immuable et rechargement atomique démontrés ; 54 tests .NET verts, catalogue de référence de 6 documents chargé et recherches, références et cycles vérifiés.

## 2. Objectif

Charger atomiquement un catalogue validé, résoudre prérequis/références, détecter les cycles et offrir une recherche en mémoire.

## 3. Contexte à lire

`AGENTS.md`, `ARCHITECTURE.md`, `CONTENT_GUIDE.md`, `SECURITY.md`, `ROADMAP.md`, fiche `02A` et ses schémas réels.

## 4. Prérequis

`02A` validé ; schéma v1 gelé ; fixtures disponibles.

## 5. Périmètre inclus

Lecture de fichiers, validation avant publication, snapshot immuable, index par ID/type/compétence, graphe de prérequis acyclique, recherche titre/résumé/glossaire et rechargement atomique contrôlé.

## 6. Périmètre explicitement exclu

Lecteur de cours, notes/signets, progression, diagnostic, contenu massif et exécution d'exercices.

## 7. Fichiers ou projets principalement concernés

Curriculum Application/Infrastructure, `content/` de référence minimal, tests Unit/Integration, composition Web sans nouvelle page complète.

## 8. Étapes d'implémentation

Définir `ContentCatalog` immuable ; charger dans zone temporaire logique ; valider tous les documents/références ; construire graphe/index ; publier seulement après succès ; définir tri/recherche déterministes ; journaliser métadonnées sans contenu sensible.

## 9. Règles d'architecture

Contenu en fichiers versionnés ; progression hors catalogue ; snapshot en lecture seule ; aucune mutation partielle ou dépendance Web dans le chargeur.

## 10. Règles de sécurité

Confinement des chemins, limites nombre/taille, Markdown non exécuté, solutions/tests cachés séparés des projections publiques et erreurs sans contenu secret.

## 11. Tests à écrire

Chargement valide ; référence absente ; ID dupliqué ; cycle direct/indirect ; ordre stable ; recherche accents/casse ; échec de reload conservant l'ancien snapshot ; concurrence lecture/reload.

## 12. Tests manuels à effectuer

Charger catalogue minimal, rechercher par titre/compétence, casser un fichier, relancer et vérifier refus total avec ancien snapshot intact.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
# Exécuter la validation et le chargement du catalogue minimal documentés.
```

## 14. Critères d'acceptation

Catalogue atomique, immuable, sans cycle ni référence cassée, recherche déterministe et tests négatifs utiles.

## 15. Conditions d'arrêt

Schéma 02A doit changer de façon incompatible, fuite de contenu caché, snapshot partiel ou cycle non détecté.

## 16. Mise à jour attendue de la roadmap

Cocher `02B` seulement ; prochaine fiche `02C`.

## 17. Format obligatoire du rapport final

Contrats/index ; flux de chargement ; contenus de référence ; tests/performances de base ; erreurs démontrées ; sécurité ; confirmation d'absence de lecteur/progression.
