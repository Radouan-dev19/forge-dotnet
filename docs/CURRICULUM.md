# Parcours pédagogique — 24 semaines

## Cadre

Charge cible : 10–15 h/semaine, au moins quatre séances. Chaque semaine combine cours, pratique autonome, débogage, révision espacée, anglais et examen sans aide. Les prérequis sont validés par compétence, pas uniquement par calendrier.

## Progression

| Période | Compétences | Livrable/preuve |
|---|---|---|
| Jours 1–3 | diagnostic, terminal/IDE, IA, lecture de ticket, découpage, premier debug | bilan initial et contrat |
| S1 | types, conversions, contrôle, méthodes, I/O, breakpoints | petits programmes sans aide |
| S2 | tableaux, collections, chaînes, dates, cas limites | exercices de transformation |
| S3 | classes, encapsulation, interfaces, composition, exceptions, nullable | modèle métier testé |
| S4 | génériques, delegates, lambdas, LINQ, fichiers, JSON | import CSV vers rapport JSON |
| S5 | reformulation, pseudocode, complexité, recherche, tris simples | série algorithmique commentée |
| S6 | piles, files, dictionnaires, récursivité utile, arbres minimaux | exercices et explications |
| S7 | stack traces, breakpoints avancés, données, async simple | 12 bugs avec causes racines |
| S8 | modèle relationnel, contraintes, SELECT, filtres, jointures | requêtes vérifiées |
| S9 | agrégations, sous-requêtes, CTE, transactions, isolation | scénarios produits/commandes |
| S10 | index, plans, pagination, EF Core, tracking, N+1, concurrence | module données mini-ERP |
| S11 | HTTP, REST pragmatique, routing, contrôleurs, DTO | API lisible |
| S12 | validation, DI, configuration, secrets, erreurs | tranche API robuste |
| S13 | async, annulation, pagination, filtres, tri, OpenAPI | API exploitable |
| S14 | authentification, rôles, OWASP utile | API sécurisée |
| S15 | xUnit, AAA, règles métier, cas limites | tests unitaires pertinents |
| S16 | doubles, intégration, base/API de test | suite d'intégration |
| S17 | non-régression, analyse statique, refactoring, review/diff | revue argumentée |
| S18 | Git, commits, branches, conflits, PR, versions | historique propre |
| S19 | Docker, images, volumes, réseaux, Compose | démarrage en une commande |
| S20 | CI build/test, artefacts, variables, secrets, déploiement simple | pipeline vert |
| S21 | Azure App Service/Container Apps, SQL, Storage, Key Vault, identité | déploiement documenté |
| S22 | logs, métriques, corrélation, alertes, coûts, performance, sécurité | incident simulé résolu |
| S23 | projet final guidé, architecture, implémentation manuelle | jalons et preuves |
| S24 | défense, entretiens, anglais, CV, candidatures, négociation | démonstration et plan post-parcours |

## Distribution minimale du contenu

Le catalogue final contient au moins 70 leçons, 80 exercices C#/algo, 25 labs debug, 40 exercices SQL/EF, 35 API/tests/sécurité, 15 Git/Docker/CI/Azure, 50 cartes d'anglais, 190 questions d'entretien selon la répartition demandée, 8 mini-projets, 1 projet final et 8 examens. Ces volumes sont livrés par lots décrits dans la roadmap, jamais dans un commit unique.

À la validation de lʼincrément 10, S1–S24 sont matérialisées : 70 leçons, 135 exercices dont 35 activités API/tests/sécurité et 15 activités Git/Docker/CI/Azure, 25 DebugLabs, 40 scénarios SQL/EF, 190 questions dʼentretien (120 junior, 50 intermédiaire, 20 avancé), 50 cartes dʼanglais plus une activité historique, 8 mini-projets, 1 projet final guidé et 8 examens. Les détails, limites du mode Azure simulé et preuves de volume figurent dans `CONTENT_S21_S24.md`.

## Structure d'une semaine

- Diagnostic court des acquis et révisions dues.
- Deux à quatre leçons avec pratique guidée puis autonome.
- Une séance entièrement sans IA (minimum hebdomadaire total : 2 h).
- Un laboratoire de débogage ou d'analyse adapté au thème.
- Une activité d'anglais professionnel et une explication orale.
- Un examen sans aide ; rattrapage planifié, pas de simple répétition immédiate.
- Rétrospective : preuves, lacunes et plan de la semaine suivante.

## Pré-requis et adaptation

Le diagnostic peut permettre de raccourcir une leçon, jamais de supprimer son test de maîtrise. Un score ancien ou uniquement fondé sur quiz ne débloque pas un module critique. Une faiblesse détectée insère des activités de remédiation sans déplacer artificiellement tout le calendrier.

## Examens proposés

1. Fondamentaux C# (fin S2).
2. C# moderne et mini-projet (fin S4).
3. Algorithmique/débogage (fin S7).
4. SQL/EF Core (fin S10).
5. API et sécurité (fin S14).
6. Tests et qualité (fin S17).
7. Livraison Docker/CI/Azure (fin S22).
8. Projet final et entretien intégré (S24).

## Projets

Mini-projets progressifs : import de commandes, bibliothèque de collections, analyseur de logs, moteur de promotions, base commandes, API mini-ERP, stratégie de tests, livraison conteneurisée. Le projet final assemble API, SQL Server, EF Core, règles, validation, auth, tests, Docker, CI, logs et déploiement ; la plateforme fournit jalons et grille, jamais la remise complète avant soumission.

## Anglais et carrière

L'anglais est transversal : ticket, commit, PR, bug, architecture, clarification, désaccord, incident et entretiens de 5 puis 15 minutes. Les candidatures commencent après la Porte A ou au plus tard en S12. Les preuves restent honnêtes et attribuent l'assistance.

## Après les 24 semaines

Parcours distinct de 12–24 mois : systèmes distribués, messaging, cache, résilience, observabilité avancée, architecture, mentoring, estimation, incidents, anglais B2/C1, allemand A2/B1 et leadership. Il ne fait pas partie du MVP.
