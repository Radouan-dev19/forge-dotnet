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
- [x] Incrément 09 — contenu S11–S20 complet, projets progressifs, examens 5–6 et laboratoires API/tests/Git/Docker/CI validés
- [x] Incrément 10 — contenu S21–S24 complet, Azure localement simulé, projet final guidé sans remise fournie, examens 7–8, 190 questions, 50 cartes dʼanglais et outils carrière validés
- [x] Incrément 11 — finition MVP, matrice des 16 critères conforme, installation vierge, sécurité, accessibilité, performance, sauvegarde et qualification complète
- [ ] Incrément 12 — audit pédagogique indépendant et contradictoire

L'audit du 7 août 2026 est refusé et documenté dans `PEDAGOGICAL_AUDIT.md`. L'incrément 12 reste ouvert : corriger et démontrer la clôture de ses P1, puis réexécuter les sept personas dans un environnement navigateur/Docker réinitialisable. Aucun incrément suivant n'est créé implicitement.

Reprise du 10 août 2026, détaillée dans la section « Reprise » du rapport d'audit :

| Défaut | État |
|---|---|
| P1-02 — exemples d'exercice et d'énoncé SQL | clos |
| P1-03 — parcours SQL/EF intégré au produit | clos |
| P1-04 — message Practice après solution | clos |
| P2-01 — page d'accueil Practice | clos |
| P1-01 — leçons non finalisées | clos sur le contenu : les 70 leçons S1–S24 sont rédigées et sorties du registre de dette |
| Boucle d'aide générique | clos : les 135 indices de niveau 4, jeux d'erreurs fréquentes et explications sont propres à leur exercice, figés par `ExerciseHintQualityTests` |
| Matériel de diagnostic générique | clos : les 17 DebugLabs dont le ticket nommait la cause sont repris — ticket de symptôme, journal mesuré, grille spécifique — figés par `DebugScenarioQualityTests` |
| Porte de maîtrise infranchissable | clos sur les domaines à exercices : la rétention espacée accepte les cartes d'exercice, à garanties et seuils inchangés. Banque à 230 cartes sur 115 exercices — C#, Débogage, Api, Tests intégralement couverts, figés par `ReviewCardQualityTests`. SQL reste hors de portée : sa pratique passe par des scénarios, pas par des exercices |
| Verdict de maîtrise jamais prononcé | clos pour la porte A : sur vingt-trois clés d'accomplissement, une seule avait un producteur, si bien qu'aucune porte ne pouvait s'ouvrir. Les quatre projets console portent un starter et des suites d'acceptation exécutées dans le bac à sable ; une réussite complète produit `project.console`. Deux clés ont depuis franchi la porte A : `code-review` (`project-code-review-001`) et `ef-core` (`project-orders-database-001`, dont les suites exécutent du vrai EF Core contre une vraie base SQLite). Les dix-neuf clés restantes sont inventoriées par `ProjectAchievementTests` avec, chacune, le diagnostic de ce qui lui manque ; ce nombre ne peut que descendre, et seulement d'autant de clés réellement produites |
| Application livrée incapable de valider quoi que ce soit | clos : le mode `Manual` par défaut ne rapporte aucun test, donc aucune preuve, donc aucune porte — un apprenant suivant le README obtenait une installation où rien ne pouvait être validé, sans que rien le lui dise. La construction de l'image du bac à sable n'était par ailleurs documentée nulle part, et son contexte n'est pas la racine du dépôt. `scripts/build-code-runner.ps1`, message de démarrage qui le nomme, section « Valider des exercices » du README, bannière sur `/mastery`, figés par `InstallationHonestyTests` |
| 293 documents publiés sans aucun écran | clos : `InterviewQuestion` (242 fiches) et `EnglishActivity` (51 cartes) — deux familles sur dix — étaient chargées, validées et comptées sans qu'aucune route ne permette de les lire. Pages `/interviews` et `/english`, lien depuis les 175 exercices appariés, réponse modèle masquée par défaut, aucune preuve de maîtrise produite. `ContentReachabilityWebTests` exige désormais une route servie pour **chaque** `ContentDocumentType` |
| Bac à sable inopérant sur un moteur Docker à jour | clos : l'option de montage `bind-nonrecursive`, supprimée dans Docker 29, faisait échouer la création de tout conteneur. Aucun exercice ni projet ne pouvait s'exécuter sur un poste à jour, pendant que la vérification hors ligne restait verte. Remplacée par `bind-recursive=disabled`, de sémantique identique, les dix-huit contrôles d'isolation verts |
| Soumission de projet impossible | clos : le contrat d'exécution rejetait la cible `<projet>.<jalon>` que `SubmitProject` construit, et deux projets sur six nommaient leur manifeste de suite d'après le seul identifiant de projet. Le producteur de la porte A ne pouvait donc jamais se déclencher. Motif élargi à un second segment sans point interne, cinq manifestes corrigés, trajet complet exécuté par `ProjectSubmissionDockerRunnerTests` et convention figée hors Docker par `ProjectCorrectnessTests` |
| Domaines de maîtrise inatteignables | clos : deux composantes sans producteur et une attribution de domaine codée en dur plaçaient six domaines sous leur propre seuil — débogage 60 pour 80, Api et Tests 15 pour 85, cinq domaines à 0. La porte A était donc mathématiquement impossible. Attribution par la compétence, quiz de leçon projeté, banque portée à 350 cartes sur 175 éléments : tous les domaines plafonnent à 90. `MasteryReachabilityTests` calcule ce plafond et refuse qu'il repasse au niveau du seuil |

Les dix projets portent désormais un dossier — `projects/<id>/project.json`, `brief.md`, et pour les
**six** projets vérifiables — les quatre projets console, la revue de code de la piste senior (Senior S7) et la base commandes
de S10 — un `starter/`, un corrigé de référence et une suite d'acceptation par jalon.
La reprise de leurs briefs et de leurs jalons a par ailleurs sorti cinq documents du registre de
dette, qui descend de 164 à **159**.

Descente de la dette éditoriale, vague par vague : 376 → 346 (S1–S10) → 337 (S11–S13) → 328
(S14–S16) → 319 (S17–S19) → 310 (S20–S22) → 306 (S23–S24), puis 274 → 245 → 225 → 196 → 181 par
la reprise des cent trente-cinq échelles d'indices, 172 → 164 par celle des dix-sept DebugLabs
générés, 164 → 159 par les briefs de projet, puis 159 → 131 → 106 → **0** par les trois derniers
lots décrits ci-dessous.

Reste ouvert, sans constituer un P1 : la densité de pratique en S11–S24.

## Extinction de la dette éditoriale

Les trois derniers lots ferment le registre. Chacun a été mesuré avant et après, et le registre
régénéré par `--emit-content-debt` à chaque étape.

| Lot | Documents traités | Ce qui était recopié | Dette |
|---|---:|---|---:|
| Scénarios SQL | 28 | l'assertion d'effet, la note de solution et cinq énoncés identiques | 159 → 131 |
| Cartes d'anglais | 50 | la consigne, les attendus, le vocabulaire, la réponse modèle, l'erreur et la variante | 131 → 106 |
| Fiches d'entretien | 191 | les critères observables, les erreurs fréquentes et une phrase de méthode dans la réponse | 106 → **0** |

Deux constats méritent d'être conservés, parce qu'ils portent sur la mesure autant que sur le contenu.

**Le comptage par document sous-estimait la duplication.** Les règles d'authenticité travaillent sur
des paragraphes entiers : une phrase gabarit insérée au milieu d'un paragraphe par ailleurs propre
échappe à la détection. Cinquante réponses modèles d'entretien portaient ainsi la même phrase de
méthode et soixante-treize la même phrase de preuve, sans qu'aucune n'apparaisse au registre. La
reprise a donc visé les phrases, pas seulement les documents signalés.

**Certaines réponses modèles étaient grammaticalement cassées.** Vingt-cinq cartes d'anglais
commençaient par un collage du générateur — « My decision is that On the stated window… ». Aucune
règle ne pouvait le voir : le texte était unique, donc conforme. Il est réécrit.

État après reprise : les 197 fiches d'entretien portent **401 critères observables distincts sur 401**
et **204 erreurs fréquentes distinctes sur 204**, contre 50 et 29 auparavant. Les 51 cartes d'anglais
portent 51 consignes, 51 réponses modèles et 51 variantes distinctes, contre 3, 26 et 3. Les 28
scénarios SQL portent chacun leur propre assertion d'effet et leur propre note de solution.

Le plafond de `ContentAuthenticityTests` est descendu à **zéro**, ce qui change la nature du cliquet :
il n'encadre plus une dette héritée, il interdit toute réapparition. Le premier paragraphe d'au moins
douze mots partagé par plus de trois documents d'un même lot fait désormais échouer le build.

Le verdict reste **REFUSÉ**. Les garde-fous d'authenticité, le registre de dette à cliquet et les
échafaudeurs non destructeurs empêchent la dette de croître, et les défauts de contenu qui motivaient
le refus sont clos. Mais les sept personas n'ont pas été rejoués — navigateur et démon Docker
indisponibles — et aucun panel humain n'a été réuni. Ce sont les deux seules conditions de levée.

## Reprise de la densité de pratique S11–S17

La densité était de 3,6 exercices par semaine en S11–S24 contre 8,8 en S1–S10 — l'écart portait
précisément sur les semaines qui décident d'une embauche backend. La reprise avance par lots, semaine
par semaine, et la matrice figée de `ContentS11S20CoverageTests` la rend visible à chaque étape.

| Lot | Semaines portées | Exercices ajoutés | Densité S11–S17 |
|---|---|---:|---:|
| Lot 0 | S11 | 1 — `api-content-negotiation-001` | 5,1 → 5,3 |
| Lot 1 | S12 à S17 | 6 | 5,3 → 6,0 |
| Lot JWT | S14 | 6 — `security-jwt-*` | 6,0 → 6,9 |
| Lot REST | S11 à S13 | 12 — versionnage, ETag, débit, cache, CORS, webhooks | 6,9 → **8,6** |
| Lot tests-qualité | S15 à S17 | 6 — mutation, couverture, ordre, caractérisation, assertions, sévérité | 8,6 → **9,4** |

Le lot 1 ajoute `api-validation-aggregate-001` (S12), `api-sort-expression-001` (S13),
`security-scope-grant-001` (S14), `tests-boundary-probe-001` (S15),
`tests-shared-state-leak-001` (S16) et `quality-unreachable-branch-001` (S17). Chacun porte ses
quatre indices propres, ses trois cas visibles et au moins quatre cas cachés, ses deux cartes de
révision, sa fiche d'entretien, et rejoint la banque de l'examen correspondant — sans quoi il ne
pourrait jamais être tiré. `ExerciseCorrectnessTests` prouve hors Docker que chaque solution passe
tous ses cas et que chaque starter en échoue au moins un.

Le choix de conception du lot est explicite : domaines d'entrée ouverts et sémantique réelle, à
l'image du lot 0. Un exercice dont le domaine d'entrée se compte sur les doigts — deux booléens, par
exemple — se résout par une table de correspondance apprise par cœur, ce qui mesure la mémoire et non
la compétence annoncée. C'est le défaut structurel des activités `azure-*`, `docker-*` et `ci-*`
existantes, et il ne se corrige pas en ajoutant des exercices de la même forme.

Le lot JWT ajoute à S14 deux leçons — anatomie d'un jeton, ordre de validation — et six exercices
`security-jwt-*` qui font décoder, signer et valider des jetons réels avec la cryptographie de la
BCL, plus le laboratoire `api-jwt-bearer` qui câble le middleware correspondant. S14 devient la
première semaine à dépasser la cible, avec douze exercices.

Le lot OAuth/OIDC prolonge cette piste en S21 — la compétence d'identité de la semaine rend le
placement cohérent, l'identité gérée étant un flux d'identifiants client : trois leçons
(`oauth-flows-001`, `oauth-pkce-001`, `oidc-identity-001`), cinq exercices `security-*` sur les
noyaux décidables — défi PKCE, verdict de state, choix de flux, revendications du jeton
d'identité, fenêtre de rotation — et le laboratoire `oauth-local-idp`, un guichet d'autorisation
en processus qui déroule les deux flux sans aucun fournisseur réel.

Le lot REST comble les six manques d'API relevés par l'audit (T10) en portant S11 à S13 à dix
exercices chacune : un sujet par paire d'exercices — versionnage (`api-versioning-001`), ETag et
concurrence conditionnelle (`api-etag-concurrency-001`), limitation de débit
(`api-rate-limiting-001`), Cache-Control (`api-cache-control-001`), CORS (`api-cors-001`) et
webhooks (`api-webhooks-001`). Deux continuités sont montrées explicitement : l'ETag conditionnel
est la concurrence optimiste de `sql-isolation-001` et `ef-core-data-access-001` remontée dans
HTTP, et la vérification de signature des webhooks réutilise le HMAC du lot jetons. Ce lot fait
franchir la cible de huit exercices à S11–S14, ce qui sert aussi la reprise de densité (T8).

**Cible T8 atteinte le 19 août 2026.** S15 à S17 passent de six à huit exercices chacune avec le
lot tests-qualité — score de mutation, plancher de couverture, dépendance à l'ordre d'exécution,
caractérisation d'un code hérité, assertions robustes au remaniement, matrice de sévérité de revue.
S19 passe de cinq à huit et S20 de trois à six avec le lot livraison, sur des noyaux décidables à
domaine ouvert : limite mémoire effective d'une chaîne de contraintes, invalidation de cache par
liste d'instructions, politique d'empreinte contre étiquette flottante, tri d'un journal de pipeline,
fenêtre de recul exponentiel, porte de déploiement sur pièces. S22 passe de trois à six avec le lot
observabilité : segments de trace orphelins, budget d'erreur restant, alerte de persistance.
Densités mesurées après les lots T8 et T9 : S11 et S12 à onze, S13 à dix, S14 à treize, S15 et S17
à huit, S16 et S19 à neuf, S18 à huit, S20 et S22 à six, S21 à neuf, S23 à deux, S24 à quatre. S23 et S24 restent volontairement à un et trois exercices : la charge de ces
semaines vit dans le projet final `project-final-service-operations-001` — cinq jalons, grille
complète — et dans les fiches de défense, d'anglais et de carrière ; les gonfler d'exercices
détournerait le temps de la soutenance qu'elles préparent. La couverture d'examen reste close : tous
les exercices publiés figurent dans au moins une banque, la nouvelle banque `delivery-pipeline-v1`
(examen 7) portant les exercices S18–S20 que l'examen Azur-observabilité hébergeait faute de mieux.
Les sujets qu'un runner à méthode statique ne peut pas héberger réellement — écrire un vrai fichier
de conteneur, une vraie définition de pipeline, un vrai déploiement — relèvent toujours des
laboratoires de `content/labs/`.

**T9 clos le 19 août 2026.** Les douze exercices à domaine d'entrée entièrement booléen relevés par
l'audit — qui formaient entre eux des chaînes de variantes fermées — ont chacun leur frère à domaine
ouvert sur le même sujet, en paire réciproque de `variantId` : contrat statut-en-têtes pour la table
de statuts, analyse de profil pour la durée de vie d'injection, faces publique et journal pour le
message de connexion, taxonomie flux-contrat pour le choix de double, relevé gradué pour le socle de
durcissement, journal de pipeline consolidé pour le résultat de travail, porte sur pièces pour la
porte de déploiement, barème additif pour la sévérité de revue, grille rythme-artefact-livraison
pour l'hébergement, cascade sensibilité-consommateur-rotation pour la source de valeur sensible,
dossier de preuves daté pour le jalon, brief extrait du journal pour l'incident. Un treizième
booléen hors liste, `git-rebase-or-merge-001`, a été apparié à `git-branch-name-001`. Le test
`EveryBooleanDomainExerciseVariesIntoAReciprocalOpenDomainSibling` refuse désormais tout `variantId`
d'exercice booléen pointant vers un autre booléen et exige la réciprocité de la paire : les chaînes
fermées ne peuvent pas réapparaître. Note d'exactitude : la mention antérieure de
`docker-memory-limit-001` parmi les douze était une erreur de ce document — cet exercice n'est pas à
domaine booléen ; son frère `docker-memory-effective-001` reste une transposition légitime.

**T10 clos le 19 août 2026 : densité et vocabulaire de la piste senior.** Chaque semaine senior
passe dʼun à quatre exercices — vingt-quatre ajouts à noyau décidable et domaine ouvert : calendrier
de réessai avec jitter plafonné, transition de disjoncteur motivée, verdict de cloisonnement,
collisions de clés dʼidempotence, verdict de rejeu contre registre, fenêtre de déduplication,
lettres mortes avec budget, drainage dʼarriéré, clés multi-partitions, plan de compensation
inversé, état final dʼune saga, régression de lectures monotones, verdict de découpe, disponibilité
dʼune chaîne, cycles de déploiement, chemin critique dʼune trace, taux de combustion, percentile au
rang le plus proche, tri de revue par preuve, verdict de fusion protégée, propriétaires requis,
points chauds churn-complexité, ordre dʼétranglement, retrait de drapeaux. Chacun porte sa fiche
dʼentretien de niveau avancé avec la question de vocabulaire anglais, et chaque leçon senior gagne
la sous-section « Le nom en entretien » — termes anglais exacts et outils du marché (Polly,
RabbitMQ, Kafka, OpenTelemetry, gRPC) cités en une phrase chacun, vocabulaire et non dépendance.
Le projet de revue Senior S7 monte à quatre diffs (concurrence et sécurité plantées deux fois, plus
un second faux positif de style qui coûte sʼil bloque) ; Senior S8 gagne un second laboratoire
hérité, `senior-legacy-trial-002`, sur une base cassée différente — un reste de période dʼessai
sans plancher. La banque `senior-readiness-v1` élargit son vivier de 8 à 32 candidats et son tirage
de 5 à 8 : le tirage suit lʼélargissement du vivier — une pioche de plus par grand bloc de thèmes —
sans quadrupler la durée de lʼépreuve.

Limite de schéma levée pour S8–S10 : le schéma de curriculum v1 impose au moins un exercice par
module, et ces modules portaient des exercices algorithmiques recopiés de S5. Le runner embarquant
EF Core et SQLite, chacun porte désormais un exercice EF Core réel de son thème —
`ef-join-silent-customers-001` (jointures, S8), `ef-aggregate-status-counts-001` (agrégations, S9),
`ef-keyset-pagination-001` (pagination, S10) — de la même forme que les variantes d'examen
`ef-orders-*`, tirables par la banque `sql-ef-core-v1`. Le gros de la pratique SQL de ces semaines
vit toujours dans le SqlLab. Donner aux modules des champs `scenarioIds`/`labIds` demanderait une
version 2 du schéma, son chargeur et ses pages : à évaluer comme un lot propre, pas en correctif.
