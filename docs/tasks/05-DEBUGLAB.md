# Incrément 05 — DebugLab

## 1. Statut

Validé le 28 juillet 2026.

## 2. Objectif

Implémenter le cycle de débogage méthodique, le journal des bugs et 8 scénarios initiaux avec tests de non-régression.

## 3. Contexte à lire

`PRODUCT_SPEC.md`, sections Débogage de `CONTENT_GUIDE.md`/`CURRICULUM.md`, `SECURITY.md`, `ROADMAP.md`, fiches `04A`–`04D`.

## 4. Prérequis

`04D` validé ; Practice, contenu et runner disponibles.

## 5. Périmètre inclus

Étapes reproduire→prévenir, observations breakpoint/Watch/Locals/Call Stack, BugJournalEntry, validation cause/correction/test, scénarios NullReference, condition, boucle, conversion, date, LINQ, async, DI.

## 6. Périmètre explicitement exclu

SQL Lab, score de maîtrise, contenu complet des 25 labs et intégration IDE distante.

## 7. Fichiers ou projets principalement concernés

DebugLab Domain/Application/Infrastructure/Web, `content/debugging/`, projets cassés/tests, migrations et tests E2E.

## 8. Étapes d'implémentation

Modéliser journal/étapes ; charger scénario ; imposer hypothèse+preuve avant correction ; soumettre correctif via runner si applicable ; exiger non-régression ; valider cause par rubrique ; écrire 8 scénarios ; revue pédagogique.

## 9. Règles d'architecture

Méthode/états Domain, orchestration Application, contenu fichier, journal progression SQLite, exécution uniquement runner.

## 10. Règles de sécurité

Mini-solutions confinées ; aucun dépôt arbitraire ; solutions protégées ; logs initiaux assainis ; pas de chemin/secret hôte dans stack traces.

## 11. Tests à écrire

Ordre des étapes, hypothèse/preuve requises, correction sans test refusée, rubrique cause, persistance journal, chaque scénario cassé puis réparé, non-régression et accès solution.

## 12. Tests manuels à effectuer

Parcourir les 8 scénarios, utiliser la checklist, rédiger cause/preuve, soumettre correctif/test et vérifier journal exportable/lisible.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
# Valider le contenu et exécuter les 8 scénarios cassés/corrigés.
```

## 14. Critères d'acceptation

Cycle complet imposé, 8 scénarios réels, causes et preuves évaluées, tests non-régression verts, aucun simple quiz présenté comme debug.

## 15. Conditions d'arrêt

Scénario non reproductible, correction sans méthode validée, runner non sûr, solution exposée ou test d'un scénario en échec.

## 16. Mise à jour attendue de la roadmap

Cocher `05` seulement ; prochaine fiche `06A`.

## 17. Format obligatoire du rapport final

Modèle/cycle ; liste scénarios ; rubriques ; runner ; tests/commandes ; parcours manuel ; sécurité ; limites ; confirmation d'absence de SqlLab.
