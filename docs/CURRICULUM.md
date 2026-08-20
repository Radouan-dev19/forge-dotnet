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
| S25 | rendu et réconciliation, état et flux unidirectionnel, formulaires contrôlés, contrat client/serveur | raisonnement front-end transposable, exercices C# notés |
| S26 | Angular (DI, RxJS, désabonnement, détection de changement) et React (hooks, dépendances d'effet, état dérivé) | client Angular et client React branchés sur l'API JWT |
| S27 | Blazor : Server contre WebAssembly, paramètres et cascade, interop JS, état d'authentification | client Blazor authentifié, suite bUnit verte dans la solution |

## Distribution minimale du contenu

Le catalogue final contient au moins 70 leçons, 80 exercices C#/algo, 25 labs debug, 40 exercices SQL/EF, 35 API/tests/sécurité, 15 Git/Docker/CI/Azure, 50 cartes d'anglais, 190 questions d'entretien selon la répartition demandée, 8 mini-projets, 1 projet final et 8 examens. Ces volumes sont livrés par lots décrits dans la roadmap, jamais dans un commit unique.

À la validation de lʼincrément 10, S1–S24 sont matérialisées : 70 leçons, 135 exercices dont 35 activités API/tests/sécurité et 15 activités Git/Docker/CI/Azure, 25 DebugLabs, 40 scénarios SQL/EF, 190 questions dʼentretien (120 junior, 50 intermédiaire, 20 avancé), 50 cartes dʼanglais plus une activité historique, 8 mini-projets, 1 projet final guidé et 8 examens. Les détails, limites du mode Azure simulé et preuves de volume figurent dans `CONTENT_S21_S24.md`.

### État courant du volume de pratique

Le relevé ci-dessus est celui de lʼincrément 10 et nʼest pas réécrit. Depuis, les reprises de densité
décrites dans `ROADMAP.md` portent le catalogue à **238 exercices** et **293 fiches dʼentretien**
(mesure du 19 août 2026 : compte des dossiers `content/reference/exercises/` et
`content/reference/interviews/`). La répartition par semaine :

| Période | Exercices référencés | Par semaine |
|---|---:|---:|
| S1–S10 | 88 | 8,8 |
| S11–S14 | 45 | 11,3 |
| S15–S17 | 25 | 8,3 |
| S18–S20 | 23 | 7,7 |
| S21–S24 | 21 | 5,3 |
| S25–S27 (front-end) | 4 | 1,3 |
| Piste senior (8 semaines) | 32 | 4,0 |

La pratique des semaines S8 à S10 vit dʼabord dans les **40 scénarios SQL/EF du SqlLab** ; leur
module référence en plus un exercice EF Core réel du thème — jointure en creux, agrégation avec
filtre de groupe, pagination par jeu de clés — exécuté par le runner comme les variantes dʼexamen
`ef-orders-*`.

La cible de huit exercices par semaine est **atteinte sur S11–S19** (huit à treize chacune), S20 et
S22 sont à six, et S23–S24 restent volontairement légères — leur charge vit dans le projet final et
les preuves de défense et de carrière. Le comptage exact est figé par les matrices de
`ContentS11S20CoverageTests` et `ContentS21S24CoverageTests`, qui refusent toute dérive dans un sens
comme dans lʼautre. Le reliquat T9 est clos : chaque exercice à domaine dʼentrée booléen forme une
paire réciproque de `variantId` avec un frère à domaine ouvert sur le même sujet, et un test refuse
tout `variantId` booléen pointant vers un autre booléen.

## Bloc front-end (S25–S27)

Le front-end est un axe distinct, ajouté après les vingt-quatre semaines du socle et matérialisé par un bloc dédié de trois modules dans `forge-reference.json` (semaines 25 à 27). Il vise le niveau « opérationnel sur le framework de l'équipe, capable de se débrouiller sur les deux autres », sur les huit compétences attendues : composant et cycle de vie, état local contre partagé, formulaire et validation, appel HTTP avec erreurs et annulation, routage et garde d'accès, consommation d'un JWT, test de composant, construction et déploiement d'un artefact.

Sa stratégie est mixte et assumée :

- Le raisonnement transposable est enseigné par quatre leçons de socle et trois leçons de spécificité (Angular, React, Blazor), puis pratiqué par quatre exercices C# à domaine ouvert qui passent par le runner et **alimentent le score de maîtrise** : réduction d'état, machine à états d'un champ, décision de cache stale-while-revalidate, garde de route à partir d'un JWT.
- Le câblage réel passe par trois laboratoires branchés sur l'API du laboratoire `api-jwt-bearer` : `angular-orders-client`, `react-orders-client` et `blazor-jwt-client`.
- Seul `blazor-jwt-client` produit une preuve automatique : sa suite bUnit s'exécute dans la solution même (`dotnet test`), sans navigateur ni conteneur. Les clients Angular et React exigent une installation réseau `npm` unique — le seul point du parcours dans ce cas — et leur réussite reste **déclarée**, jamais collectée par le serveur.

Trois activités de débogage « base existante » (une par framework) entraînent la navigation en terrain inconnu par la méthode en quatre temps des DebugLabs, en réduisant le défaut planté à son noyau décidable en C#. L'examen 9 (`final-readiness-v1`) tire désormais aussi les quatre exercices front-end.

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

Mini-projets progressifs : import de commandes, bibliothèque de collections, analyseur de logs, moteur de promotions, base commandes, API mini-ERP, stratégie de tests, livraison conteneurisée. Six d'entre eux sont **vérifiables** — squelette, corrigé de référence, une suite d'acceptation par jalon exécutée dans le bac à sable — et deux produisent une exigence de porte au-delà de la porte A : la base commandes de S10 produit `ef-core`, la revue de code de la piste senior (Senior S7) produit `code-review`. Le projet final assemble API, SQL Server, EF Core, règles, validation, auth, tests, Docker, CI, logs et déploiement ; la plateforme fournit jalons et grille, jamais la remise complète avant soumission.

## Anglais et carrière

L'anglais est transversal : ticket, commit, PR, bug, architecture, clarification, désaccord, incident et entretiens de 5 puis 15 minutes. Les candidatures commencent après la Porte A ou au plus tard en S12. Les preuves restent honnêtes et attribuent l'assistance.

**Kit carrière (S24), servi depuis le 20 août 2026.** La promesse de la semaine 24 — CV, candidatures, négociation — est portée par cinq guides publiés sous le type de document `CareerGuide` et servis sur `/career` : le CV par preuves, le carnet STAR (trois récits rédigés issus d'un DebugLab, d'un projet console et de l'incident simulé), le protocole de prospection (le fichier de suivi reste local à l'apprenant), la négociation chiffrée et le plan 30/60/90. Les guides passent le validateur comme tout document du catalogue, la règle de joignabilité les couvre, la page S24 de `/learn` et le tableau de bord (porte A ouverte) y renvoient. Aucun guide ne produit de preuve de maîtrise et aucun ne promet emploi ou salaire — chaque page l'annonce.

## Piste senior (Senior S1–S8)

Les vingt-quatre semaines ne mènent pas au niveau senior, et cette page le déclarait déjà. Une **piste senior** distincte comble une partie de ce manque, dans un **second document de parcours**, `content/reference/curriculum/forge-senior-reference.json`, chargé et paginé à part (page `/learn-senior`) avec ses propres tests de couverture. Le parcours junior des vingt-quatre semaines n'est pas modifié : sa matrice reste le relevé du chemin junior vers confirmé.

**Numérotation, arbitrée le 18 août 2026.** Les numéros 25 à 27 désignaient deux contenus à la fois : le bloc front-end du parcours junior et les premières semaines de la piste senior. La piste senior est un **semestre distinct** : ses semaines s'affichent désormais **Senior S1 à Senior S8**, dans le manifeste comme sur `/learn-senior`. Les identifiants de contenu (`senior-25` à `senior-32`, leçons et exercices `senior-*`) et les numéros de semaine internes (25 à 32, figés par `ContentSeniorCoverageTests`) restent inchangés : seul l'affichage change. L'ordre de lecture est : **S1–S24 → bloc front-end S25–S27 → piste senior Senior S1–S8**.

L'objectif n'est pas d'apprendre à faire des microservices. C'est une thèse assumée : un junior qui découpe en microservices produit un système distribué qu'il ne sait pas exploiter. La piste vise les fondamentaux distribués qui rendent la conversation crédible en entretien senior, **y compris le refus argumenté de découper**.

| Semaine | Thème | Livrable |
|---|---|---|
| Senior S1 | résilience d'un appel : délai, réessai avec jitter, disjoncteur, cloisonnement | budget d'appel tenu sous panne injectée |
| Senior S2 | idempotence, rejeu, au-moins-une-fois contre exactement-une-fois | endpoint rejouable prouvé |
| Senior S3 | messagerie : consommateur idempotent, outbox, file de lettres mortes | décision de consommation |
| Senior S4 | cohérence éventuelle, compensation d'une saga | ordre de compensation |
| Senior S5 | découper ou non : coût d'un déployable, frontières, refus argumenté | décision de découpe |
| Senior S6 | observabilité distribuée : corrélation inter-service, budget d'erreur | décision de gel de livraison |
| Senior S7 | revue de code sur des diffs à défauts plantés | produit l'accomplissement `code-review` |
| Senior S8 | base de code existante, méthode en quatre temps | correctif + non-régression sur du legacy |

Senior S7 est le premier producteur de l'accomplissement `code-review`, qui n'en avait aucun : un projet vérifiable note le classement de défauts plantés sur **quatre diffs** — correction, sécurité, concurrence, et deux faux positifs de style qui coûtent s'ils sont présentés comme bloquants. Senior S8 applique la méthode des DebugLabs à un défaut planté dans du code que l'apprenant n'a pas écrit, sur **deux bases cassées distinctes** (pagination et période d'essai).

**Densité de la piste, reprise du 19 août 2026.** Chaque semaine senior porte désormais **quatre exercices** à noyau décidable — budget de réessai, verdict de disjoncteur, collision de clés, verdict de rejeu, plan de compensation, décision de découpe, tri de revue par preuve, localisation dans une trace — plus sa leçon, sa fiche dʼentretien par exercice et, en S7/S8, le projet de revue et les deux laboratoires hérités : au moins **quatre activités par semaine senior**. Chaque leçon senior nomme en outre son vocabulaire dʼentretien en anglais (sous-section « Le nom en entretien »), outils du marché cités pour le vocabulaire, pas comme dépendances.

**Ce que la piste ne remplace pas, sans l'enjoliver.** Recevoir la revue d'un humain qui n'est pas d'accord, arbitrer sous pression, traverser un désaccord d'équipe : aucun contenu ne remplace ces situations. La piste entraîne le raisonnement distribué, le classement d'une revue et la navigation en terrain inconnu ; elle ne fabrique ni relecteur humain, ni interlocuteur qui conteste. Cette limite est écrite ici et sur la page de la piste, par la même discipline que le projet s'impose partout ailleurs.

Le catalogue compte désormais **neuf examens** : le huitième reste la synthèse S1–S24, le neuvième (`senior-readiness-v1`) tire **8 exercices parmi les 32** de la piste senior — le tirage a suivi la densité pour rester proportionnel au vivier.

## Chapitre IA (hors parcours, libre accès)

Depuis le 20 août 2026, six guides `AiGuide` servis sur `/ai` couvrent l'usage professionnel d'un
assistant de code : modèle mental (contexte, tokens, coût), économie de tokens, paramétrage par
couches, skills, agents et sous-agents, boucle quotidienne. Le chapitre est volontairement **sans
semaine, sans prérequis et sans ordre imposé** : il se lit selon les besoins et les disponibilités.
Il ne produit aucune preuve de maîtrise — le bac à sable, sans réseau par conception, ne peut
vérifier aucun usage d'assistant — et chaque guide rappelle la frontière du contrat d'apprentissage :
l'IA outille la production du développeur, jamais les preuves mesurées du parcours (exercices
comptés, examens, réflexions, explications, journaux de débogage).

## Après les 24 semaines

Parcours distinct de 12–24 mois : systèmes distribués, messaging, cache, résilience, observabilité avancée, architecture, mentoring, estimation, incidents, anglais B2/C1, allemand A2/B1 et leadership. Il ne fait pas partie du MVP.
