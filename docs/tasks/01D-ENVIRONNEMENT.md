# Incrément 01D — Environnement

## 1. Statut

Validé le 25 juillet 2026. Construction et cycle Compose démontrés ; 36 tests .NET, 26 assertions de configuration et 23 assertions runtime verts.

## 2. Objectif

Rendre l'installation locale reproductible et préparer l'environnement complet avec Docker Compose sans introduire les runners ni SqlLab.

## 3. Contexte à lire

`AGENTS.md`, `ARCHITECTURE.md`, `SECURITY.md`, `ROADMAP.md`, README et fiches `01A` à `01C`.

## 4. Prérequis

`01C` validé ; installation locale sans Docker fonctionnelle ; Docker Desktop/Compose disponibles pour la vérification.

## 5. Périmètre inclus

`docker-compose.yml` initial, configuration de l'application, volumes/ports minimaux, variables documentées, healthchecks de services présents, commandes démarrage/arrêt et procédure d'installation vierge.

## 6. Périmètre explicitement exclu

Runner Docker, SQL Server de laboratoire, exercices, contenu, CI/CD et déploiement Azure.

## 7. Fichiers ou projets principalement concernés

`docker-compose.yml`, éventuel `Dockerfile` Web minimal si réellement requis, `.dockerignore`, configuration, `README.md`, futur `docs/RUNBOOK.md`, tests smoke.

## 8. Étapes d'implémentation

Inventorier dépendances ; définir mode CLI et Compose ; créer images/config minimales ; ne pas embarquer données/secrets ; ajouter health ; documenter installation vierge et nettoyage récupérable ; exécuter sur poste cible.

## 9. Règles d'architecture

Un seul monolithe ; Compose orchestre sans créer de service métier supplémentaire ; SQLite reste stockage de progression local.

## 10. Règles de sécurité

Utilisateur non-root si conteneurisé, ports limités à localhost, secrets hors dépôt, image SDK absente du runtime si possible, volumes explicites, aucune socket Docker montée.

## 11. Tests à écrire

Smoke de configuration, health après démarrage, absence de secrets/ports inattendus, démarrage avec configuration minimale et conservation des tests existants.

## 12. Tests manuels à effectuer

Sur installation vierge : cloner, configurer, démarrer, ouvrir l'app, vérifier health, arrêter puis relancer ; vérifier les commandes de diagnostic et l'absence de conteneur orphelin.

## 13. Commandes de vérification

```powershell
docker compose config
docker compose build
docker compose up -d
docker compose ps
Invoke-WebRequest -UseBasicParsing http://localhost:5012/health
docker compose down
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```

## 14. Critères d'acceptation

Démarrage documenté en une commande, health vert, arrêt propre, installation vierge reproductible, aucun secret et aucune fonction future anticipée.

## 15. Conditions d'arrêt

Docker indisponible sur cible, image vulnérable critique non acceptée, port public involontaire, secret détecté ou besoin d'ajouter SqlLab/runner.

## 16. Mise à jour attendue de la roadmap

Cocher `01D` seulement ; prochaine fiche `02A`.

## 17. Format obligatoire du rapport final

Fichiers ; architecture runtime ; commandes/sorties Compose ; installation vierge ; tests ; ports/volumes ; vulnérabilités ; écarts ; confirmation d'absence de runner et SqlLab.
