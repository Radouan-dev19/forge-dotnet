# Incrément 06B — SQL/EF initial

## 1. Statut

Validé le 29 juillet 2026.

## 2. Objectif

Livrer 12 scénarios SQL/EF Core initiaux de haute qualité couvrant les fondamentaux prévus S8–S10.

## 3. Contexte à lire

S8–S10 de `CURRICULUM.md`, SQL de `CONTENT_GUIDE.md`, `SECURITY.md`, `ROADMAP.md`, fiches `02A` et `06A`.

## 4. Prérequis

`06A` validé ; runner SQL/reset/validation stables.

## 5. Périmètre inclus

12 scénarios couvrant jointures, agrégations, sous-requête/CTE, transaction/isolation, index/plan, pagination, EF DbContext/migrations pédagogiques, tracking/AsNoTracking, IQueryable, chargement/N+1 et concurrence basique.

## 6. Périmètre explicitement exclu

40 exercices finaux, contenu S11+, modification structurelle du SqlLab et SQL exotique rare.

## 7. Fichiers ou projets principalement concernés

`content/sql/`, datasets/seeds versionnés, starters EF, solutions/tests, matrice de couverture.

## 8. Étapes d'implémentation

Planifier couverture/difficulté ; écrire par lots ; datasets minimaux déterministes ; résultats/effets attendus ; solutions expliquées ; assertions stables de plan ; tests EF réels ; reset entre scénarios ; revue contenu.

## 9. Règles d'architecture

Scénarios en fichiers ; aucune exception codée dans le moteur ; SQL Lab séparé de progression ; code EF pédagogique isolé.

## 10. Règles de sécurité

Datasets non sensibles, permissions minimales, requêtes bornées, solutions cachées, pas de chaîne de connexion réelle dans contenu/logs.

## 11. Tests à écrire

Solution correcte et variantes équivalentes, colonnes/ordre/valeurs, effets transactionnels, reset, index propriété stable, N+1 détecté, tracking/concurrence et tests négatifs par scénario.

## 12. Tests manuels à effectuer

Résoudre un scénario de chaque famille, inspecter schéma/plan, reset, exécuter starter EF et vérifier explications/temps.

## 13. Commandes de vérification

```powershell
docker compose up -d <service-sql-lab>
dotnet test --no-build
# Valider et exécuter les 12 scénarios et solutions.
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
docker compose down
```

## 14. Critères d'acceptation

12 scénarios complets, S8–S10 couverts sans duplication, solutions/tests/reset verts et contenu conforme au guide.

## 15. Conditions d'arrêt

Scénario ambigu, résultat dépendant d'un coût exact, secret exposé, reset instable, solution fausse ou besoin de modifier 06A hors correction séparée.

## 16. Mise à jour attendue de la roadmap

Cocher `06B` seulement ; prochaine fiche `07A`.

## 17. Format obligatoire du rapport final

Matrice des 12 ; datasets ; résultats/tests ; commandes ; plans/EF ; revue éditoriale ; défauts ; confirmation d'absence de Mastery.
