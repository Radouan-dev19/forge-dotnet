# Roadmap de réalisation

## Règle de passage

Chaque incrément est une tranche démontrable. Il exige critères observables, tests applicables verts, documentation mise à jour, risques revus et aucune erreur masquée. Une phase refusée reste en cours ; la suivante ne sert pas à contourner ses défauts.

## Incréments

| # | Incrément | Résultat démontrable | Validation principale |
|---:|---|---|---|
| 0 | Conception | documents cohérents et décisions explicites | revue documentaire |
| 1A | Solution | projets, dépendances, conventions, CI locale | restore/build/format |
| 1B | Web local | Blazor, navigation, profil, health check | smoke/integration test |
| 1C | Persistance | SQLite, migration, seed démo, sauvegarde de base | migration aller/retour testée |
| 1D | Environnement | Compose initial et installation | démarrage vierge documenté |
| 2A | Schémas contenu | JSON v1 et validateur | fixtures valides/invalides |
| 2B | Catalogue | chargement atomique, prérequis, recherche | tests de graphes/références |
| 2C | Lecteur | leçon, quiz, notes, signets, progression | leçon de référence E2E |
| 3A | Diagnostic | session, banque, minuterie, reprise | diagnostic réduit E2E |
| 3B | Évaluation | score, incertitude, carte, rapport | cas limites unitaires |
| 3C | Plan | recommandations et plan accepté | test d'intégration complet |
| 4A | Pratique | réflexion, tentatives, indices, solutions | règles anti-contournement |
| 4B | Runner local | contrat, orchestration, résultats | tests avec double |
| 4C | Runner Docker | sandbox, quotas, nettoyage, mode manuel | batterie sécurité dédiée |
| 4D | Contenu C# initial | 10 exercices S1–S2 | validation + tests exercices |
| 5 | DebugLab | méthode, journal et 8 scénarios | parcours + non-régression |
| 6A | SqlLab | SQL Server, reset, éditeur, validation | isolation et résultat |
| 6B | SQL/EF initial | 12 scénarios | validation automatisée |
| 7A | Mastery | scores, plafonds, preuves, portes | tests de contournement |
| 7B | Révisions | cartes et planification | horloge simulée |
| 7C | Examens/dashboard | examen sans aide et mesures | E2E examen |
| 8 | Contenu S1–10 | lots complets conformes | validateur et runners verts |
| 9 | Contenu S11–20 | API, tests, Git, Docker, CI | revue technique par lot |
| 10 | Contenu S21–24 | Azure, projet, entretien, anglais, carrière | volumes et rubriques |
| 11 | Finition MVP | accessibilité, sécurité, performance, backup, runbook | matrice des 16 critères |
| 12 | Audit pédagogique | personas adversariaux et corrections P0/P1 | rapport indépendant |

## Lots de contenu

Chaque lot reste révisable : 3–6 leçons ou 5–10 exercices, avec schéma, contenu, solutions et tests dans le même changement. Un tableau de couverture suivra semaine, compétence, difficulté, type de preuve et volumes cumulés. Aucun lot ne contient de placeholder.

## Risques et atténuations

| Risque | Impact | Réponse |
|---|---|---|
| Évasion ou épuisement via code | critique | défense en profondeur Docker, quotas, tests d'abus, arrêt fermé |
| Docker indisponible | élevé | mode manuel explicite sans validation automatique |
| Faux score de maîtrise | élevé | poids pratique/examen, plafonds d'aide, variété, récence, tests adversariaux |
| Fuite tests cachés/solutions | élevé | serveur uniquement, séparation d'artefacts, tests réseau/bundle |
| Contenu massif incohérent | élevé | schéma, petits lots, validateur, matrice de couverture, revue éditoriale |
| SQL destructif | élevé | instance jetable, least privilege, timeout, réseau interne |
| Accessibilité de l'éditeur | moyen | choix mesuré, navigation clavier, alternative fichier/IDE |
| Sauvegarde SQLite incohérente | moyen | checkpoint/backup API, manifeste/checksum, restauration sur copie |
| Dépendances/images vulnérables | moyen | versions verrouillées, scan, cadence de mise à jour |
| Périmètre 24 semaines trop large | élevé | MVP vertical d'abord, volumes par lots, portes qualité strictes |

## Compromis à valider

1. Monolithe modulaire plutôt que projets par module : moins de friction, discipline de dépendances à tester.
2. JSON canonique plutôt que JSON+YAML : moins convivial pour certains auteurs, validation plus simple.
3. SQLite mono-utilisateur : excellent pour le local, non adapté à une future collaboration sans migration.
4. Docker comme automatisation sûre raisonnable : mode manuel obligatoire et aucun discours de sécurité absolue.
5. Évaluation déterministe : transparente/hors ligne, nécessite des rubriques éditoriales soignées.
6. Markdown puis impression pour l'export : PDF spécialisé différé jusqu'à preuve du besoin.

## Jalons d'acceptation

- **Socle** : application, base et health check reproductibles.
- **Boucle pédagogique** : leçon → réflexion → tentative → feedback → maîtrise → révision.
- **MVP vertical** : un scénario réel de chaque type satisfait les 16 critères produit.
- **Catalogue initial** : volumes obligatoires atteints et validés par lots.
- **Release candidate** : installation vierge, E2E, sécurité, accessibilité et sauvegarde démontrées.

## État

- [x] Incrément 0 — conception initiale
- [x] Incrément 1A — solution, dépendances, conventions et vérifications
- [x] Incrément 1B — Web local, profil mémoire, navigation et health check
- [x] Incrément 1C — SQLite, migration, profil durable, health check et sauvegarde/restauration
- [x] Incrément 1D — image Web minimale, Compose local, volume persistant, healthcheck et runbook
- [x] Incrément 02A — huit schémas JSON v1, validateur déterministe, fixtures et CLI
- [x] Incrément 02B — catalogue immuable, références, cycles, recherche et rechargement atomique
- [x] Incrément 02C — lecteur, leçon de référence, quiz, notes, signets et progression de lecture honnête
- [x] Incrément 03A — session de diagnostic, banque versionnée, échantillonnage, minuterie serveur, reprise et collecte sans notation
- [x] Incrément 03B — barème versionné, score et intervalle de confiance, carte agrégée, lacunes critiques et rapport prudent
- [x] Incrément 03C — recommandations explicables, plan de 24 semaines, charge ajustable, versions et acceptation
- [x] Incrément 04A — réflexion préalable, tentatives manuelles, indices progressifs, solution protégée et historique non maîtrisé
- [x] Incrément 04B — contrat de runner, orchestration, résultats séparés et double déterministe sans exécution réelle
- [x] Incrément 04C — runner Docker éphémère isolé, quotas, nettoyage prouvé, scan d'image et mode manuel honnête
- [x] Incrément 04D — dix exercices C# S1–S2 complets, suites visibles/cachées, runner réel et revue éditoriale
- [x] Incrément 05 — cycle DebugLab, journal de bugs, huit scénarios réels et tests de non-régression
- [x] Incrément 06A — SQL Server isolé, sessions jetables, exécution bornée, validation structurée et audit de sécurité renforcé
- [x] Incrément 06B — douze scénarios SQL/EF Core S8–S10, datasets déterministes, solutions, contre-exemples, resets et validation automatisée
- [x] Incrément 07A — maîtrise versionnée, preuves C#/Debug/SQL typées, plafonds d’aide, récence/variété et portes A–D explicables
- [x] Incrément 07B — cartes issues des difficultés, planification J+1/J+3/J+7/J+14/J+30, file du jour, cartes personnelles et preuves de rétention vérifiées
- [x] Incrément 07C — examen sans aide, tirage et échéance serveur, rapport auditable, verrouillage des aides et dashboard factuel
- [x] Incrément 08 — contenu S1–S10 complet, examens 1–4, soumissions SQL déléguées au SqlLab et exercices EF Core exécutés dans le runner isolé
- [ ] Incréments 09 et suivants — non commencés

La prochaine action est uniquement l'incrément 09 « Contenu semaines 11 à 20 ». Aucun contenu S21+ ni modification opportuniste des moteurs ne doit être anticipé.
