# Incrément 11 — Finition MVP

## 1. Statut

Non commencé.

## 2. Objectif

Auditer et corriger sécurité, dépendances, UX, accessibilité, performance, sauvegarde, erreurs, documentation et installation jusqu'à démontrer les 16 critères MVP.

## 3. Contexte à lire

Tous les documents racine et `docs/*.md`, toutes les fiches `00` à `10`, rapports des incréments et code/contenu complets.

## 4. Prérequis

`10` validé ; tous les incréments antérieurs verts ; environnement Windows/Docker vierge disponible.

## 5. Périmètre inclus

Correction des défauts MVP, audit dépendances/images, responsive/clavier/contraste, performance, backup/restore, messages/logs, E2E, installation vierge, cohérence/liens/exercices, runbook et matrice d'acceptation.

## 6. Périmètre explicitement exclu

Nouvelle fonctionnalité non exigée par le MVP, refonte esthétique gratuite, sujet post-24 semaines et audit pédagogique indépendant de `12`.

## 7. Fichiers ou projets principalement concernés

Toute la solution, `content/`, `docs/`, Compose/images, tests et nouveau/complété `docs/RUNBOOK.md` plus rapport d'acceptation.

## 8. Étapes d'implémentation

Inventorier critères ; établir preuve actuelle ; prioriser P0/P1 ; corriger avec non-régression ; scanner dépendances/images ; audit accessibilité/performance ; installation vierge ; backup/restore/corruption ; E2E ; valider contenu/liens ; remplir matrice avec artefacts exacts.

## 9. Règles d'architecture

Préserver monolithe et frontières ; toute refactorisation doit résoudre un défaut mesuré ; aucun nouveau projet/service sans justification validée.

## 10. Règles de sécurité

Rejouer revues CodeRunner/SqlLab, inspect Docker, secret scan, dépendances/images, XSS/CSRF, fuite tests/solutions, logs/redaction, restauration hostile et exposition réseau.

## 11. Tests à écrire

Non-régressions de chaque défaut ; E2E diagnostic/leçon/exercice C#/SQL/debug/examen ; installation/health ; backup/restore/corruption ; accessibilité automatisée ; liens/contenu ; abus runners.

## 12. Tests manuels à effectuer

Installation vierge Windows, navigation mobile/clavier, parcours MVP complet, Docker absent, panne service, sauvegarde/restauration, inspection logs/console/réseau et temps de démarrage.

## 13. Commandes de vérification

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
docker compose config
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
# Exécuter scans dépendances/images, validateur de contenu et suite E2E documentée.
```

## 14. Critères d'acceptation

Matrice obligatoire :

| # | Critère MVP | Preuve exigée |
|---:|---|---|
| 1 | Application démarre localement | installation vierge + health 200 |
| 2 | Diagnostic fonctionne | E2E complet et reprise |
| 3 | Parcours personnalisé créé | test diagnostic→plan accepté |
| 4 | Leçon complète consultable | E2E lecteur + conformité guide |
| 5 | Exercice C# soumis/testé | runner sécurisé + résultat |
| 6 | Exercice SQL exécuté | isolation + validation résultat |
| 7 | Lab de débogage suivi | cycle complet + journal/test |
| 8 | Indices progressifs | tests ordre/verrouillage |
| 9 | Solution déclenche révision | intégration Practice→Reviews |
| 10 | Examen sans aide | E2E verrouillage/temps/rapport |
| 11 | Maîtrise calculée | tests formule/anti-contournement |
| 12 | Révisions dues visibles | test horloge + dashboard |
| 13 | Dashboard honnête | audit données et absence de faux indicateur |
| 14 | Sauvegarde/restauration | exercice valide + corruption refusée |
| 15 | Tests automatisés passent | sorties complètes vertes |
| 16 | Installation neuve documentée | exécution du runbook sur machine propre |

Chaque ligne doit être « conforme » avec lien vers test/artefact, jamais seulement déclarative.

## 15. Conditions d'arrêt

Un critère sans preuve, vulnérabilité critique non acceptée, test flaky/rouge, installation non reproductible, contenu invalide ou nécessité d'une nouvelle fonctionnalité hors MVP.

## 16. Mise à jour attendue de la roadmap

Cocher `11` seulement si les 16 lignes sont conformes ; prochaine fiche `12`.

## 17. Format obligatoire du rapport final

Matrice 16 complète ; corrections/fichiers ; commandes/sorties ; tests/échecs/ignorés ; scans ; accessibilité/performance ; installation ; risques résiduels/dérogations humaines ; confirmation que `12` reste indépendant.
