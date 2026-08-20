# Protocole de revue par un tiers

Sept exigences du parcours ne sont pas vérifiables par une machine. Ce document dit comment un
relecteur humain peut les observer, sur quelle preuve, et comment consigner son verdict — dans le
produit, par la page `/human-review`, depuis le 18 août 2026.

Distinct mais complémentaire : `docs/HUMAN_PANEL_KIT.md` outille le **panel humain** de l'audit
(P2-02), qui juge le produit et son contenu avec des participants recrutés — pas le travail d'un
apprenant. La revue atteste une personne ; le panel évalue Forge.NET.

## Ce qu'une attestation est, et n'est pas

Une attestation enregistrée est un **troisième type de preuve**,
`MasteryVerificationKind.HumanAttestation` — ni une preuve machine, ni une déclaration de
l'apprenant. La frontière est tenue par le code et non par la discipline :

- les règles ne l'admettent que pour les six clés de `MasteryPolicyCatalog.HumanJudgementKeys` et
  pour la composante Explication — sur toute clé vérifiable par la machine, elle vaut zéro, parce
  que la parole ne remplace jamais une preuve exécutée ;
- `MasteryRules.IsEligible` refuse toujours `ManualDeclaration` sans condition : l'apprenant seul
  ne peut rien attester, et le contrôle d'identité refuse un relecteur qui porte son nom ;
- l'enregistrement refuse une grille incomplète, un critère obligatoire non observé, une durée
  au-dessous du minimum de la grille, un écart non nommé, et le rejeu d'une même revue ;
- partout où elle apparaît, l'attestation est étiquetée « attestée par un relecteur humain, non
  vérifiée par la machine » — le produit ne peut vérifier ni l'identité du relecteur, ni ce qu'il
  a réellement observé, et le dit.

Ce n'est pas une certification. Un relecteur atteste de ce qu'il a vu, à une date, sur un artefact
nommé. Il n'engage aucune institution, et l'attestation ne vaut rien face à un employeur qui ne
connaît pas le relecteur. Ce qu'elle vaut est double : une **répétition sous contrainte réelle**,
faite avant l'entretien plutôt que pendant — et, pour les portes C et D, la seule preuve possible
des exigences qu'aucune machine ne jugera jamais.

## Pourquoi ces sept-là

Six sont des exigences de porte classées `Blocker.HumanJudgement` dans `ProjectAchievementTests`. La
septième est la composante `MasteryComponent.Explanation`, dont `docs/MASTERY.md` établit qu'elle est de
la même nature : produire un compte rendu causal dans ses propres mots n'a pas de substitut machine.

| Clé ou composante | Porte | Ce qui ne s'automatise pas |
|---|---|---|
| `git.clean` | B | juger un historique exige de lire un dépôt réel, que le produit local ne voit pas |
| `presentation.10-minutes` | B | une présentation orale ne se vérifie pas par une suite de tests |
| `interview.mock` | C | un entretien suppose un interlocuteur qui relance et conteste |
| `architecture.pragmatic` | D | la qualité d'une note de décision se juge sur son argumentation |
| `english` | D | l'expression demande un lecteur ; les 51 cartes sont auto-évaluées |
| `project.final-defense` | D | une défense est une performance devant un jury, par construction |
| composante `Explanation` | — | 10 % du score, sans producteur automatique, et sans substitut machine honnête |

## Le relecteur

**Qui convient.** Une personne qui écrit du code en équipe depuis au moins trois ans, ou qui a mené des
entretiens techniques. Elle n'a pas besoin de connaître Forge.NET : les critères ci-dessous sont
autoportants.

**Ce qui disqualifie une revue**, sans jugement moral, parce que le biais est mécanique :

- le relecteur a écrit tout ou partie de l'artefact qu'il évalue ;
- il découvre l'artefact pendant la revue et n'a pas pu le lire avant, pour les exigences qui l'exigent
  ci-dessous ;
- il connaît d'avance le verdict attendu parce qu'on le lui a demandé.

Un relecteur complaisant ne coûte pas seulement une attestation sans valeur : il coûte l'information
que la revue devait produire. Une revue qui n'a rien trouvé n'est utile que si elle a vraiment cherché.

## Trois règles communes

**1. La preuve avant le jugement.** Le relecteur reçoit l'artefact et le lit **avant** de voir la
grille pour les exigences écrites (Git, architecture, explication). Pour les exigences orales
(présentation, entretien, anglais, défense), il lit la grille avant et observe en direct.

**2. Le refus est le défaut.** Un critère non observé est **non satisfait**. Le relecteur ne complète
pas mentalement ce que le candidat « voulait sûrement dire ». S'il faut le compléter, c'est le constat.

**3. Un écart nommé, toujours.** Même un verdict favorable cite au moins un point concret à travailler,
localisé — un commit, un instant de l'enregistrement, une phrase de la note. « C'était bien » n'est pas
une revue et rend l'attestation nulle.

---

## Les sept grilles

Chaque grille est binaire par critère : observé / non observé. Le verdict global est **satisfait**
seulement si tous les critères obligatoires sont observés. Il n'y a pas de moyenne, pas de compensation
— la même règle que les portes du produit.

### 1. `git.clean` — historique Git propre

**Preuve acceptée** : un dépôt accessible au relecteur, et une plage de commits nommée (par exemple
`main~30..main`), portant du travail réel de l'apprenant. Ni capture d'écran, ni export.

| # | Critère observable | Obligatoire |
|---|---|:---:|
| 1 | Chaque message de commit dit **ce qui change et pourquoi**, pas quel fichier est touché | oui |
| 2 | Aucun commit ne mélange une correction de fond et une reformulation de style | oui |
| 3 | Un commit isolé pris au hasard dans la plage se comprend **sans lire les autres** | oui |
| 4 | Aucun secret, jeton, dump de données ni chemin personnel dans l'historique — y compris supprimé plus tard, donc toujours présent dans les objets | oui |
| 5 | Les branches portent un nom qui dit l'intention ; aucune branche morte non fusionnée sans raison | non |
| 6 | Le candidat sait dire pourquoi il a fusionné ou rebasé à un endroit donné | non |

**Ce qui ne compte pas** : le nombre de commits, la présence d'une convention formelle
(*conventional commits*), la linéarité de l'arbre. Un historique linéaire de messages vides échoue ; un
arbre avec fusions et messages justes réussit.

### 2. `presentation.10-minutes` — présentation de 10 minutes

**Preuve acceptée** : la présentation en direct, ou un enregistrement continu et non monté. Un support
sans parole ne suffit pas.

| # | Critère observable | Obligatoire |
|---|---|:---:|
| 1 | Le problème est posé avant la solution, et en une phrase | oui |
| 2 | Au moins une décision technique est **justifiée par une contrainte**, pas par une préférence | oui |
| 3 | Une limite connue du travail est énoncée spontanément, sans qu'on la demande | oui |
| 4 | Le temps est tenu à ±2 minutes sans couper la fin | oui |
| 5 | Une question du relecteur reçoit une réponse ou un « je ne sais pas » net, jamais un contournement | oui |
| 6 | Le support sert le propos et n'est pas lu mot à mot | non |

**Ce qui ne compte pas** : l'aisance, l'accent, la qualité graphique du support, l'absence d'hésitation.
On évalue si un auditeur non initié repart en sachant ce qui a été fait et pourquoi.

### 3. `interview.mock` — entretien blanc

**Preuve acceptée** : un entretien de 45 à 60 minutes conduit en direct par le relecteur, avec au moins
deux relances contradictoires. Les questions viennent de `content/reference/interviews/` — 242 fiches,
chacune portant ses `observableCriteria`, sa `modelAnswer` et ses `commonMistakes` — que le relecteur
choisit **sans les montrer au candidat**.

| # | Critère observable | Obligatoire |
|---|---|:---:|
| 1 | Sur trois questions tirées, les `observableCriteria` de la fiche sont énoncés **par le candidat**, sans être soufflés | oui |
| 2 | Face à une relance contradictoire, le candidat révise ou tient sa position **en donnant une raison** ; il ne cède pas au ton | oui |
| 3 | Une question hors de son domaine reçoit « je ne sais pas, voilà comment je chercherais » plutôt qu'une improvisation | oui |
| 4 | Aucune affirmation de fait n'est inventée ; le relecteur vérifie au moins une affirmation vérifiable | oui |
| 5 | Le candidat pose au moins une question qui porte sur le travail réel | non |

**Ce qui ne compte pas** : le nombre de bonnes réponses. Un candidat qui ignore trois notions mais dit
franchement ce qu'il ignore et comment il l'apprendrait passe ; un candidat qui répond juste partout en
récitant échoue au critère 4 dès la première vérification.

### 4. `architecture.pragmatic` — note d'architecture

**Preuve acceptée** : une note écrite de 2 à 4 pages sur une décision **réellement prise** dans un
projet de l'apprenant, lue par le relecteur avant tout échange.

| # | Critère observable | Obligatoire |
|---|---|:---:|
| 1 | Au moins deux options sont décrites, dont une que l'auteur n'a pas retenue mais présente **sous son meilleur jour** | oui |
| 2 | Le critère de départage est nommé et il est concret — coût d'exploitation, délai, compétence de l'équipe, réversibilité | oui |
| 3 | Le coût de la décision retenue est énoncé : ce qu'elle rend plus difficile | oui |
| 4 | Une condition de réexamen est écrite : le fait futur qui invaliderait le choix | oui |
| 5 | La note tient sans schéma ; les schémas, s'il y en a, n'ajoutent pas d'information absente du texte | non |

**Ce qui ne compte pas** : le vocabulaire d'architecture, le nombre de couches, la conformité à un
patron nommé. Une note qui choisit un monolithe pour une raison écrite passe ; une note qui choisit des
microservices sans coût énoncé échoue au critère 3.

### 5. `english` — anglais professionnel

**Preuve acceptée** : deux cartes de `content/reference/english/` — **une `*-written`, une `*-spoken`**
— traitées devant le relecteur, sans préparation écrite préalable pour la carte orale. Chaque carte
porte ses `expectedElements` et sa `modelAnswer` : ce sont les critères, ils n'ont pas à être réécrits.

| # | Critère observable | Obligatoire |
|---|---|:---:|
| 1 | Les `expectedElements` de la carte écrite sont tous présents dans la production du candidat | oui |
| 2 | Les `expectedElements` de la carte orale sont énoncés **à l'oral**, sans lecture d'un texte préparé | oui |
| 3 | Aucune des `commonMistakes` de la carte n'est commise | oui |
| 4 | Un malentendu provoqué par le relecteur — « sorry, you mean X? » — est corrigé et non répété plus fort | oui |
| 5 | Le registre est professionnel : une objection est formulée sans agressivité ni excuse excessive | non |

**Ce qui ne compte pas** : l'accent, la vitesse, les erreurs de grammaire qui n'empêchent pas de
comprendre, la richesse du vocabulaire. On évalue si un collègue anglophone repart avec la bonne
information. La `modelAnswer` est un exemple de contenu suffisant, **jamais une formulation à
retrouver**.

### 6. `project.final-defense` — défense du projet final

**Preuve acceptée** : une défense en direct de 30 minutes minimum, sur
`project-final-service-operations-001`, devant au moins deux relecteurs dont un qui n'a pas suivi le
projet. Le code est ouvert et navigable pendant la défense.

Les six critères de la grille du projet — valeur métier, architecture, tests, sécurité, observabilité,
honnêteté des preuves — sont dans son `project.json` et servent de plan. S'y ajoutent les critères
propres à la défense :

| # | Critère observable | Obligatoire |
|---|---|:---:|
| 1 | Le candidat ouvre le code à un endroit qu'il n'a pas choisi et l'explique | oui |
| 2 | Pour chaque affirmation de qualité — « c'est testé », « c'est sécurisé » — une **preuve est montrée à l'écran**, pas racontée | oui |
| 3 | Ce qui a été fait avec assistance est attribué, spontanément, avant qu'on le demande | oui |
| 4 | Un défaut trouvé en séance est reconnu sans être minimisé, et son impact est estimé à voix haute | oui |
| 5 | Le candidat sait dire ce qu'il referait autrement et pourquoi | oui |
| 6 | Une question sur l'exploitation — « il est 3 h, le service ne répond plus, tu fais quoi » — reçoit une marche à suivre | oui |

**Ce qui ne compte pas** : la complétude du projet. Une tranche verticale honnête et défendue vaut mieux
qu'un périmètre large présenté vaguement. Le critère 3 est éliminatoire par lui-même : une assistance
non attribuée invalide la défense entière, quelle que soit la qualité du reste.

### 7. Composante `Explanation` — explication d'une solution

**Preuve acceptée** : un exercice **déjà résolu sans assistance**, choisi par le relecteur dans
l'historique de pratique du candidat, expliqué à l'oral ou à l'écrit sans que le code soit sous les
yeux.

| # | Critère observable | Obligatoire |
|---|---|:---:|
| 1 | L'explication énonce le **pourquoi** de l'approche, pas la suite des instructions | oui |
| 2 | Au moins un lien causal est affirmé et **vérifiable** : « si je retire ceci, tel cas casse » — le relecteur le vérifie | oui |
| 3 | Le candidat nomme un cas limite que sa solution traite, et dit comment il l'a su | oui |
| 4 | Confronté à une entrée qu'il n'a pas vue, il prédit la sortie correctement | oui |
| 5 | Une approche écartée est mentionnée, avec ce qu'elle coûtait | non |

**Ce qui ne compte pas** : la terminologie exacte, la fluidité, la longueur. Le critère 2 est le cœur :
c'est lui qui sépare un modèle causal d'une récitation, et c'est précisément ce qu'aucune correction
automatique ne sait faire — d'où l'existence de cette grille.

---

## Consigner le verdict

Le verdict se consigne deux fois, et les deux copies n'ont pas le même rôle.

**Dans le produit**, par la page `/human-review` : la grille est remplie critère par critère avec ce
qui l'a montré, et l'attestation acceptée produit la preuve `HumanAttestation` décrite plus haut.
Le produit n'enregistre que les revues **satisfaites** — tous les critères obligatoires observés :
un verdict non satisfait reste un document personnel, et c'est voulu, parce qu'un refus est une
information pour l'apprenant, pas une preuve pour une porte.

**Dans le dossier personnel de l'apprenant**, le fichier ci-dessous — y compris pour les verdicts
non satisfaits, qui sont souvent les plus utiles à relire. Ne pas le déposer sous `content/` : le
validateur de contenu le refuserait, et sa présence y suggérerait un statut qu'il n'a pas.

**Nom** : `<clé>-<AAAA-MM-JJ>.md`, par exemple `git.clean-2026-09-14.md`.

**Contenu** :

```markdown
# Attestation de revue — git.clean

- Relecteur : Prénom Nom, fonction, organisation (ou « indépendant »)
- Lien avec l'apprenant : collègue / ancien collègue / rencontre professionnelle / aucun
- Date et durée : 2026-09-14, 40 minutes
- Artefact examiné : <URL du dépôt> — plage `main~30..main`, empreinte du commit de tête
- Grille appliquée : docs/HUMAN_REVIEW.md, section 1, révision <date du fichier>

## Verdict

Satisfait / Non satisfait

## Critères

| # | Observé | Ce qui l'a montré |
|---|:---:|---|
| 1 | oui | commits a1b2c3d et e4f5g6h : intention énoncée, fichier jamais cité |
| 2 | non | 9h8i7j6 mêle une correction de fuseau horaire et un renommage de 14 fichiers |
| … | | |

## Écart nommé

Un point concret à travailler, localisé, obligatoire y compris si le verdict est favorable.

## Déclaration du relecteur

J'ai examiné cet artefact moi-même. Je n'en suis pas l'auteur. Ce document atteste de ce que j'ai
observé à cette date ; il ne constitue ni certification, ni garantie de compétence future.
```

**Ce que ce fichier permet.** Le relire six mois plus tard et constater ce qui a bougé. Le montrer à un
tiers comme trace d'une revue subie — jamais comme diplôme. Rendre le refus possible : une exigence
peut être **non satisfaite**, ce qui est une information, alors qu'une absence d'attestation n'en est
pas une.

## Ordre conseillé

`git.clean` en premier — l'artefact existe déjà et la revue est asynchrone, donc facile à obtenir.
`architecture.pragmatic` ensuite, pour la même raison. Puis la composante `Explanation` et `english`,
qui demandent une séance courte. `presentation.10-minutes` et `interview.mock` ensuite. La défense
finale en dernier, et devant deux relecteurs : c'est la seule qui suppose que tout le reste a déjà été
éprouvé.

## Ce qui reste vrai après toutes les attestations

Le tableau de maîtrise distingue trois natures de preuve et ne les confondra jamais : une exigence
attestée s'affiche « attestée par un relecteur humain, non vérifiée par la machine », pas « prouvée ».
Les portes B, C et D peuvent désormais admettre ces attestations pour leurs seules exigences à
jugement humain — leurs exigences vérifiables continuent d'exiger une preuve exécutée, et aucun
seuil, aucun poids, aucun plafond d'aide n'a bougé.

Un employeur ne recrute toujours pas sur une attestation qu'il n'a pas commandée. Ce que ces sept
revues produisent vaut probablement davantage que la porte qu'elles ouvrent : la liste des points où
quelqu'un de compétent vous a effectivement arrêté.
