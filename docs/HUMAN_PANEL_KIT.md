# Kit de panel humain — matériel de la condition 2 (P2-02)

> **Statut : protocole prêt, non exécuté.** Aucun panel n'a eu lieu. Ce kit prépare la seconde
> condition de levée du verdict de `docs/PEDAGOGICAL_AUDIT.md` ; il ne la lève pas. Ce qui manque
> encore : des humains et du temps, rien d'autre.

Ce document suffit à une personne extérieure pour animer trois sessions de panel et restituer des
conclusions qui entrent telles quelles dans le registre de l'audit. Il complète la page
`/human-review` et les grilles de `docs/HUMAN_REVIEW.md` : la revue humaine atteste le travail d'un
apprenant précis ; le panel juge le **produit et son contenu** avec des personnes réelles.

## 1. Qui anime, qui participe

**L'animateur** n'a contribué ni au code ni au contenu, et n'a aucun intérêt au verdict. Il ne
répond à aucune question pendant les tâches — chaque intervention est notée comme un blocage. Un
seul animateur suffit ; une seconde personne en observation silencieuse est un plus, pas une
exigence.

**Trois profils à recruter**, alignés sur les personas de l'audit :

| Profil | Persona d'audit | Critères d'inclusion | Critères d'exclusion |
|---|---|---|---|
| PH-1 — Débutant fragile | P1 | Se destine à une reconversion ; moins de six mois de code, tous langages confondus ; à l'aise pour lire du français technique | A déjà suivi un cursus informatique diplômant |
| PH-2 — Fort en quiz | P5 | Réussit facilement des QCM techniques (autoévaluation assumée) ; connaît la syntaxe C# ou Java ; n'a jamais tenu un poste de développement | Pratique professionnelle du .NET |
| PH-3 — Faible SQL | P4 | Développe (n'importe quel langage) mais déclare éviter SQL ou ne jamais écrire de jointure sans documentation | À l'aise avec les jointures et agrégations |

Une connaissance personnelle de l'animateur est admissible si les critères tiennent ; un
contributeur du dépôt ne l'est jamais.

## 2. Environnement et données

- Poste de l'animateur, installation locale suivie depuis `docs/RUNBOOK.md`. PH-1 tourne en mode
  `Manual` ; PH-2 exige le runner Docker (`scripts/build-code-runner.ps1`) ; PH-3 exige en plus
  SqlLab (`scripts/start-sql-lab.ps1`).
- **Un dossier de données neuf par session**, jamais le profil de quiconque. Commande complète, à
  adapter au mode du profil (`Manual` pour PH-1, `Docker` pour PH-2 et PH-3, plus
  `SqlLab__Enabled=true` pour PH-3) :

  ```powershell
  $env:LocalData__DirectoryPath = Join-Path $env:TEMP 'forge-panel-ph1-session-du-jour'
  $env:CodeRunner__Mode = 'Manual'
  dotnet run --project src/ForgeDotNet.Web
  ```

  Supprimer ce répertoire après la restitution. Pour le mode `Docker`, renseigner aussi
  `CodeRunner__Docker__ImageReference` avec l'empreinte affichée par
  `docker image inspect forge-dotnet-runner:local --format "{{.Id}}"`.
- Le participant invente un pseudonyme et un objectif fictifs. Aucune donnée réelle ne rentre dans
  le produit, aucune donnée du produit ne sort de la session.

## 3. Déroulé commun d'une session

75 minutes : 5 min de cadrage (consentement lu et signé, pseudonyme choisi), 45 à 50 min de tâches
**sans aide de l'animateur**, 10 min de relecture éditoriale ciblée (section 5), 10 min de débrief à
voix haute. L'animateur tient la feuille de temps : heure de début et de fin de chaque tâche, chaque
blocage (où, combien de temps, résolu comment), chaque moment où l'interface **ment ou surprend**
(le participant dit une chose, l'écran en dit une autre), et chaque reformulation demandée — « avec
tes mots, que vient-il de se passer ? » — notée réussie ou non, avec verbatim.

## 4. Scripts de session par profil

### PH-1 — Débutant fragile (mode Manual, 50 min de tâches)

| # | Tâche (formulation à lire telle quelle) | Cible | Ce que l'animateur note |
|---|---|---|---|
| 1 | « Crée ton profil avec 6 heures par semaine et prends connaissance du contrat. » | 5 min | Le contrat est-il lu ? Reformulation d'un engagement au choix. |
| 2 | « Passe le diagnostic réduit, réponds comme tu peux, puis lis ton évaluation. » | 15 min | Reformule-t-il « preuves insuffisantes » et l'intervalle d'incertitude sans aide ? Se sent-il jugé ou informé (verbatim) ? |
| 3 | « Génère ton plan, ajuste la charge si besoin, accepte-le. » | 10 min | La charge proposée est-elle perçue tenable ? Comprend-il que le plan n'est pas une promesse ? |
| 4 | « Lis la leçon “Choisir un type monétaire adapté” et réponds au quiz. » | 15 min | Temps de lecture réel ; reformulation de la notion ; réaction à un quiz raté. |
| 5 | « Ouvre l'exercice “Additionner deux montants” et remplis la réflexion préalable. » | 5 min | Les six champs sont-ils compris ? Si refus serveur : le message suffit-il pour corriger seul ? |

### PH-2 — Fort en quiz (Docker requis, 50 min de tâches)

| # | Tâche | Cible | Ce que l'animateur note |
|---|---|---|---|
| 1 | « Crée ton profil, puis lis une leçon de la semaine 1 et réussis son quiz. » | 10 min | Vitesse ; le quiz est-il jugé discriminant (verbatim) ? |
| 2 | « Résous l'exercice “Additionner deux montants” jusqu'à des tests verts, sans indice. » | 15 min | Le protocole réflexion → tentative → runner est-il suivi ou subi ? Blocages réels. |
| 3 | « Ouvre la page Maîtrise et explique-moi pourquoi rien n'est encore validé. » | 10 min | **Le cœur du persona** : reformule-t-il sans aide que lecture et quiz ne valent pas autonomie (variété, examen, récence) ? |
| 4 | « Essaie d'obtenir la solution d'un second exercice. » | 5 min | Le verrou (deux tentatives sérieuses + délai) est-il compris comme une protection ou une friction hostile (verbatim exact) ? |
| 5 | « Sur le tableau de bord, dis-moi ce qu'il te manque pour la porte A. » | 10 min | Les blocages listés sont-ils actionnables à ses yeux ? |

### PH-3 — Faible SQL (Docker + SqlLab, 50 min de tâches)

| # | Tâche | Cible | Ce que l'animateur note |
|---|---|---|---|
| 1 | « Crée ton profil, puis ouvre SqlLab et choisis le scénario “Projeter les clients actifs”. » | 10 min | L'énoncé et les colonnes attendues sont-ils compris sans aide ? |
| 2 | « Crée la session et écris ta requête ; valide-la contre la référence. » | 15 min | En cas d'échec : le message « résultat non conforme » et ses écarts permettent-ils de corriger **seul** ? Combien d'itérations ? |
| 3 | « Continue jusqu'à un résultat conforme, ou dis-moi quand tu abandonnes. » | 10 min | Le point d'abandon exact ; ce qui aurait aidé (verbatim). |
| 4 | « Ouvre Maîtrise et le tableau de bord : que faudrait-il pour la porte A ? » | 10 min | L'exigence SQL non compensée est-elle comprise comme légitime ? |
| 5 | « Réinitialise ta session et vérifie que les données sont revenues. » | 5 min | La mécanique jetable rassure-t-elle ou inquiète-t-elle ? |

## 5. Relecture éditoriale échantillonnée

L'audit confie au jugement humain quatre familles : les explications d'exercice, les leçons, les
fiches d'entretien et les réponses modèles d'anglais. L'échantillon est tiré au sort de façon
**reproductible** :

- **Graine : `forge-panel-2026-08-20`.** Pour chaque identifiant de la population, calculer
  `SHA-256("forge-panel-2026-08-20:" + identifiant)`, trier les empreintes hexadécimales par ordre
  croissant et retenir les N premières. Populations au tirage : 238 exercices, 96 leçons, 293
  fiches, 51 cartes d'anglais.

**10 explications d'exercice** (`content/reference/exercises/<id>/explanation.md`) :
`tests-shared-state-leak-001`, `algo-maximum-value-001`, `docker-image-tag-001`,
`api-token-bucket-001`, `algo-selection-sort-001`, `api-cache-storability-001`,
`docker-health-window-001`, `senior-breaker-window-001`, `api-route-normalize-001`,
`debug-copy-before-sort-001`.

**5 leçons** (`content/reference/curriculum/lessons/<id>/lesson.md`) :
`front-blazor-essentials-001`, `final-defense-english-001`, `debug-stacktraces-breakpoints-001`,
`security-authentication-001`, `api-routing-rest-001`.

**5 fiches d'entretien** (`content/reference/interviews/<id>.json`) :
`interview-security-jwt-decode-001`, `interview-senior-breaker-window-001`,
`interview-senior-circuit-breaker-001`, `interview-azure-secret-channel-001`,
`interview-s21-s24-017`.

**3 réponses modèles d'anglais** (`content/reference/english/<id>.json`) :
`english-card-13-spoken`, `english-card-08-written`, `english-card-19-written`.

**Répartition** : PH-1 relit deux explications et une leçon parmi les plus proches de son niveau ;
PH-2 relit quatre explications et trois fiches ; PH-3 relit deux leçons, deux explications et une
fiche ; l'animateur couvre le reste, dont les trois réponses d'anglais avec le participant le plus à
l'aise en anglais. Questions posées, par famille :

- *Explication* : après lecture, peux-tu dire **pourquoi** la solution est ce qu'elle est, pas
  seulement ce qu'elle fait ? Qu'est-ce qui manque ?
- *Leçon* : l'exemple commenté t'apprend-il quelque chose que l'intuition ne disait pas déjà ? Le
  contre-exemple est-il crédible ?
- *Fiche d'entretien* : les critères observables permettraient-ils de juger une vraie réponse ? En
  ajouterais-tu un ?
- *Anglais* : la réponse modèle sonne-t-elle comme un humain la dirait en réunion ?

## 6. Grille de restitution

Une ligne par tâche, une par contenu relu. Verdicts fermés : **réussi** / **réussi avec friction**
(objectif atteint, mais hésitation notée, détour ou aide de l'interface mal comprise) / **échoué**
(objectif non atteint ou abandon). Chaque friction ou échec porte au moins un verbatim daté et un
classement selon l'échelle de l'audit, reproduite ici pour que la session se suffise :

| Niveau | Définition opérationnelle (identique à l'audit) |
|---|---|
| P0 | Signal de maîtrise falsifiable, contenu protégé exposé, intégrité atteinte ou parcours critique impossible. |
| P1 | Blocage reproductible d'un persona, ambiguïté critique, faux signal pédagogique majeur. |
| P2 | Défaut localisé avec contournement honnête, charge ou message sous-optimal. |
| P3 | Amélioration éditoriale ou ergonomique mineure. |

Gabarit à recopier dans la restitution :

```markdown
### Session PH-x — <date> — animateur : <pseudonyme>
| Tâche | Verdict | Temps | Verbatim | Sévérité |
|---|---|---|---|---|
| 1 | réussi avec friction | 7 min | « ... » | P3 |
### Contenus relus
| Contenu | Verdict | Verbatim | Sévérité |
|---|---|---|---|
| algo-maximum-value-001/explanation.md | réussi | « ... » | — |
### Synthèse du participant (3 phrases, dans ses mots)
```

## 7. Consentement et données

À lire et faire signer avant la session ; l'exemplaire signé reste chez l'animateur, jamais dans le
dépôt :

> Je participe volontairement à une session d'essai du logiciel Forge.NET, sur des données fictives
> que je choisis. Mes remarques seront notées sous pseudonyme. Aucun enregistrement audio ou vidéo
> n'est réalisé ; les notes ne seront conservées que jusqu'à la rédaction de la restitution, puis
> détruites. Aucune donnée permettant de m'identifier ne sera versionnée ni publiée. Je peux
> interrompre la session à tout moment, sans justification.

Règles absolues : sessions sur données fictives uniquement ; aucun enregistrement conservé au-delà
de la restitution ; aucune donnée personnelle — nom, employeur, voix, visage — dans le dépôt ; les
restitutions n'utilisent que PH-1, PH-2, PH-3.

## 8. De la restitution au registre de l'audit

La restitution de chaque session se colle **telle quelle** dans `docs/PEDAGOGICAL_AUDIT.md`, section
« Panel — protocole prêt, non exécuté », qui devient alors « Panel — exécuté le \<date\> ». Les
défauts P0/P1 relevés suivent la discipline de l'audit : correction et non-régression avant tout
verdict favorable ; les P2/P3 rejoignent le backlog sans correction silencieuse. La condition 2
n'est considérée levée que si les **trois** profils ont terminé leur session, restitution intégrée —
et ce jugement appartient à l'auditeur, pas à ce kit.
