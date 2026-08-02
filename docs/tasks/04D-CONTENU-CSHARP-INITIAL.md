# Incrément 04D — Contenu C# initial

## 1. Statut

Validé le 28 juillet 2026.

## 2. Objectif

Livrer 10 exercices C# de haute qualité pour S1–S2, entièrement validés et exécutables par le runner sécurisé.

## 3. Contexte à lire

`CONTENT_GUIDE.md`, S1–S2 de `CURRICULUM.md`, `SECURITY.md`, `ROADMAP.md`, fiches `02A`, `04A` à `04C`.

## 4. Prérequis

`04C` validé ; schéma, Practice et runner stables.

## 5. Périmètre inclus

10 exercices répartis types/conversions/conditions/boucles/méthodes/tableaux/listes/dictionnaires/chaînes/dates/cas limites ; chacun avec réflexion, visibles/cachés, quatre indices, solution, explication, complexité, erreurs, variante, cartes et entretien.

## 6. Périmètre explicitement exclu

Semaines 3+, DebugLab, SQL, modification du moteur/schéma sauf défaut bloquant traité séparément, contenu placeholder.

## 7. Fichiers ou projets principalement concernés

`content/exercises/`, éventuels starters/solutions/tests, matrice de couverture et tests de validation de contenu.

## 8. Étapes d'implémentation

Établir matrice S1–S2 ; écrire par lots de 5 ; vérifier autonomie/ambiguïté ; créer solution indépendante ; tests visibles pédagogiques et cachés anti-contournement ; compiler chaque starter/solution ; exécuter runner ; revue éditoriale et licence.

## 9. Règles d'architecture

Contenu uniquement en fichiers conformes ; aucune règle spécifique à un exercice dans le moteur ; IDs/version stables.

## 10. Règles de sécurité

Tests cachés jamais publics ; code exemple sans secret/réseau ; entrées bornées ; solutions verrouillées ; licence/source explicite.

## 11. Tests à écrire

Pour chaque exercice : cas nominal, limites, entrée invalide si prévue, anti-hardcode et solution ; validation globale IDs/références/variantes/indices ; compilation starter et solution.

## 12. Tests manuels à effectuer

Résoudre au moins un exercice par thème comme apprenant, demander indices, consulter solution selon règles, exécuter variante et vérifier clarté/temps.

## 13. Commandes de vérification

```powershell
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
# Exécuter le validateur de contenu et tous les tests des 10 exercices.
```

## 14. Critères d'acceptation

10 exercices complets sans placeholder, couverture S1–S2, toutes solutions/tests verts, tests cachés protégés et revue éditoriale réussie.

## 15. Conditions d'arrêt

Runner/schéma défectueux, exercice ambigu, solution incorrecte, test caché exposé, volume atteint par duplication ou validation en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `04D` seulement ; prochaine fiche `05`.

## 17. Format obligatoire du rapport final

Liste/matrice des 10 exercices ; fichiers ; tests par exercice ; validation/runner ; revue éditoriale ; défauts ; commandes ; confirmation d'absence de contenu S3+.
