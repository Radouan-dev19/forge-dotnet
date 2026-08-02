# Incrément 07A — Mastery

## 1. Statut

Validé le 29 juillet 2026.

## 2. Objectif

Calculer une maîtrise versionnée et explicable, appliquer plafonds d'aide, preuves et quatre portes sans permettre les contournements évidents.

## 3. Contexte à lire

Règles de maîtrise d'`ARCHITECTURE.md`, portes/mesures de `PRODUCT_SPEC.md`, `SECURITY.md`, `ROADMAP.md`, fiches `04A`, `05`, `06B`.

## 4. Prérequis

`06B` validé ; observations Practice/Debug/SQL persistées et typées ; seuils humains confirmés.

## 5. Périmètre inclus

MasteryScore projection, composantes 45/25/15/10/5, plafonds H1–H4/solution, récence/variété, rendement décroissant, preuves, seuil 80/85, compétences critiques et portes A–D avec motifs de blocage.

## 6. Périmètre explicitement exclu

Planification des cartes, moteur d'examen, dashboard final et contenu massif.

## 7. Fichiers ou projets principalement concernés

Mastery Domain/Application/Infrastructure, migrations, projections Web minimales, tests Unit adversariaux.

## 8. Étapes d'implémentation

Définir observations immuables ; versionner politique ; calculer composantes sans imputation ; appliquer aide/récence/variété ; exiger preuve autonome/examen ; évaluer portes par prérequis ; expliquer score et lacunes ; recalcul idempotent.

## 9. Règles d'architecture

Score dérivé, jamais muté directement ; calcul pur Domain ; événements/observations source conservés ; politique versionnée ; Web lecture seule.

## 10. Règles de sécurité

Intégrité des observations, aucune API de modification directe, aide/solution non effaçable, calcul serveur, audit des versions et pas de compensation d'une compétence critique.

## 11. Tests à écrire

Anti-contournement obligatoire : quiz faciles seuls, solution consultée, H1–H4, tentatives aléatoires répétées, même item en boucle, preuve ancienne, compétence critique faible, moyenne élevée, composante absente, faux examen, portes sans livrable, bornes/arrondis/idempotence/version.

## 12. Tests manuels à effectuer

Construire profils adversariaux, inspecter explications, tenter de modifier/rejouer observations, vérifier portes fermées et absence de message « prêt » injustifié.

## 13. Commandes de vérification

```powershell
dotnet build --no-restore
dotnet test --no-build --filter "Category=MasteryAntiGaming"
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 14. Critères d'acceptation

Politique conforme aux documents, score explicable, tous tests adversariaux verts, portes exactes, faiblesse critique jamais compensée.

## 15. Conditions d'arrêt

Seuil non validé, score mutable, aide perdue, cas adversarial réussissant à tricher, ancien score réinterprété ou besoin de commencer Reviews/Examens.

## 16. Mise à jour attendue de la roadmap

Cocher `07A` uniquement après revue contradictoire des tests ; prochaine fiche `07B`.

## 17. Format obligatoire du rapport final

Politique/formule/version ; observations ; portes ; matrice anti-contournement et résultats ; commandes ; profils manuels ; risques ; confirmation d'absence de révisions/examens.
