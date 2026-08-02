# Incrément 08 — Contenu semaines 1 à 10

## 1. Statut

Validé le 30 juillet 2026.

## 2. Objectif

Écrire et intégrer le contenu complet S1–S10 : fondamentaux C#, algorithmique/debug, SQL et EF Core, par lots vérifiables.

## 3. Contexte à lire

Intégralité de `CURRICULUM.md` et `CONTENT_GUIDE.md`, `PRODUCT_SPEC.md`, `SECURITY.md`, `ROADMAP.md`, fiches `04D`, `05`, `06B`, `07C`.

## 4. Prérequis

`07C` validé ; tous moteurs et validateurs disponibles ; volumes/IDs existants inventoriés.

## 5. Périmètre inclus

Leçons S1–S10 complètes, exercices C#/algo supplémentaires, 25 labs debug à terme de couverture prévue, exercices SQL/EF, mini-projets associés, cartes, entretiens et examens 1–4 nécessaires ; matrice de couverture.

## 6. Périmètre explicitement exclu

ASP.NET/API S11+, Git/Docker/CI, Azure, carrière, modification opportuniste des moteurs et placeholders.

## 7. Fichiers ou projets principalement concernés

`content/curriculum`, `exercises`, `debugging`, `sql`, `interviews`, `projects`, matrice de couverture et tests de contenu.

## 8. Étapes d'implémentation

Geler matrice S1–S10 ; produire lots de 3–6 leçons ou 5–10 exercices ; valider chaque lot ; compiler/exécuter starters/solutions ; équilibrer difficultés ; revue technique/pédagogique ; suivre volumes sans duplication ; exécuter parcours hebdomadaires.

## 9. Règles d'architecture

Contenu versionné uniquement ; aucune logique par exercice dans le code ; changements de schéma/moteur traités comme défauts séparés et non glissés dans le lot.

## 10. Règles de sécurité

Solutions/tests cachés séparés, code/SQL bornés, aucune dépendance externe obligatoire, licences/sources, aucun secret/donnée personnelle et validation de tous chemins.

## 11. Tests à écrire

Validation de chaque fichier, références/volumes/cycles, compilation starters/solutions, tests visibles/cachés, solutions SQL, scénarios debug cassés/réparés, examens sans aide et liens internes.

## 12. Tests manuels à effectuer

Échantillonner chaque semaine et type d'activité ; mesurer durée/clarté ; suivre un mini-projet ; vérifier indices, solutions, variantes, cartes et progression sans faux signal.

## 13. Commandes de vérification

```powershell
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
# Exécuter validateur, tous tests C#/SQL/debug et contrôle des volumes S1-S10.
```

## 14. Critères d'acceptation

S1–S10 autonomes et sans trou, lots revus, tous contenus/tests/solutions verts, volumes cumulés traçables et aucune leçon S11 anticipée.

## 15. Conditions d'arrêt

Lot trop grand, placeholder, solution fausse, exercice ambigu, moteur instable, validation rouge ou volume obtenu par duplication.

## 16. Mise à jour attendue de la roadmap

Cocher `08` seulement après validation de tous les lots S1–S10 ; prochaine fiche `09`.

## 17. Format obligatoire du rapport final

Matrice/volumes par semaine ; lots/commits proposés ; tests/commandes ; défauts corrigés ; échantillonnage manuel ; écarts ; confirmation d'absence de S11+.
