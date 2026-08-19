# Maîtrise — politique v1

## Portée

L’incrément 07A a introduit la projection locale, déterministe et explicable à partir d’observations typées. 07B a raccordé la rétention vérifiée et 07C raccorde les examens sans modifier la politique active `forge-mastery`, version `1`, révision `mastery-v1-20260729`.

Les seuils v1 retenus sont : validation ordinaire à 80, validation critique à 85, preuve récente sur 30 jours, expiration des observations de score après 90 jours et au moins trois exercices autonomes distincts. C#, débogage, SQL, API et tests sont critiques. Les seuils propres à la porte A restent ceux de la spécification produit ; ils ne remplacent pas le seuil de validation d’un module.

## Formule

```text
score = 0,45 × pratique autonome
      + 0,25 × examen sans aide
      + 0,15 × rétention espacée
      + 0,10 × explication
      + 0,05 × quiz
```

Chaque composante est bornée à 0–100. Une composante sans preuve vaut zéro et son poids n’est jamais redistribué. Le score final est borné et arrondi à deux décimales, avec arrondi au plus proche et milieu éloigné de zéro.

### Sources de la rétention espacée

La composante « rétention espacée » n’était alimentée que par une **question ratée du bilan
d’entrée**. Ce bilan compte trente-six questions, quatre par domaine, passées une seule fois, et
toute preuve est écartée au-delà de la durée de validité. Le poids d’une composante sans preuve
n’étant jamais redistribué, un domaine critique plafonnait alors à `0,45 + 0,25 + 0,10 + 0,05`, soit
exactement 85 — le seuil lui-même, atteignable uniquement avec 100 partout ailleurs et sans le
moindre indice. Un profil sérieux mais imparfait — pratique 95, examen 90, explication 90, quiz 100 —
tombait à 79,25, sous le seuil non critique. Passé la durée de validité, la situation valait pour
tout le monde.

Les **cartes de révision des exercices** rejoignent donc les sources admissibles. Les garanties
restent identiques pour toutes : correction serveur, mode à choix, et carte déclarant explicitement
pouvoir produire une preuve — `ReviewRules.ProducesMasteryEvidence` en tient la liste, et un test de
domaine la borne. Une carte n’apparaît que pour un exercice réellement soumis au runner, ce qui
empêche d’en récolter sur un exercice jamais ouvert. **Aucun poids ni seuil n’a été modifié** :
`MasteryRulesTests` documente le blocage et prouve son ouverture par la seule addition d’une preuve.

La banque compte **454 cartes pour 227 éléments pratiqués** — les 187 exercices publiés et les
40 scénarios SQL/EF, deux cartes chacun (mesure du 18 août 2026). Les domaines critiques dont la
pratique passe par des exercices — C#, débogage, API, tests — sont couverts intégralement, et deux
règles de `ReviewCardQualityTests` le tiennent : tout exercice d’un de ces domaines porte ses
cartes, et le domaine déclaré par une carte est celui déduit de la première compétence de son
exercice. Une carte mal classée alimenterait la rétention d’un domaine que l’apprenant n’a pas
travaillé.

Le domaine SQL, longtemps hors de portée parce que sa pratique passe par des scénarios et non par
des exercices, est couvert depuis que la clé de carte a été généralisée de l’exercice vers
l’élément pratiqué : les 40 scénarios SQL/EF portent 80 cartes qui alimentent sa rétention.

Les plafonds d’assistance sont H1 90, H2 80, H3 70, H4 60 et solution 0. La consultation d’une solution contamine définitivement la pratique autonome du même exercice dans la politique v1, y compris si une réussite antérieure existe. Une reprise espacée peut produire une preuve distincte uniquement lorsqu’une carte diagnostique est vérifiée côté serveur.

Pour un même exercice, la tentative vérifiée la plus récente a un poids 1, la précédente 0,5 et chaque tentative plus ancienne 0,25. Une observation âgée de 31 à 90 jours reçoit en plus un poids de récence 0,5 ; au-delà de 90 jours, elle ne contribue plus.

## Observations admises

| Source | Observation persistée | Admissibilité au score |
|---|---|---|
| Pratique C# | résultat runner, version/révision, compteurs, diagnostic et empreinte SHA-256 | tests automatiques réellement exécutés ; une déclaration manuelle reste visible mais vaut zéro |
| DebugLab | correction, résultat de tests et contamination par solution | exécution automatique seulement |
| SqlLab | statut, validation de référence, diagnostic et empreinte SHA-256 | validation de référence réussie seulement |
| Examen | item soumis d’un rapport terminé, résultat runner, tirage/durée et assistance figés | `ExamEngine` automatique et sans aide uniquement ; un faux examen ou une déclaration manuelle est refusé |
| Révision | réponse d’une carte diagnostique à choix corrigée côté serveur | `ReviewEngine` uniquement ; autoévaluation et carte personnelle restent sans score |
| Explication/quiz/livrable | preuve serveur typée attendue | la déclaration manuelle est refusée |
| Revue humaine | attestation d’un relecteur nommé, grille complète, critères obligatoires observés | `HumanAttestation`, admise pour les six clés à jugement humain et la composante Explication uniquement ; affichée « non vérifiée par la machine » |

Le code C# et la requête SQL ne sont jamais placés dans les tables d’observation. Les activités de pratique existantes conservent leurs propres données pédagogiques selon leur contrat ; la projection de maîtrise ne copie ni code, ni requête, ni sortie, ni solution.

Une validation de domaine exige simultanément le score requis, trois exercices autonomes distincts non contaminés, une pratique autonome vérifiée sans aide datant d’au plus 30 jours et un examen final vérifié sans aide. Une tentative assistée, abandonnée ou sans validation automatique ne fournit pas cette preuve. Un quiz récent ne satisfait pas la preuve autonome. Une série quotidienne n’est jamais une observation.

## Portes

### Producteurs d’accomplissement

La politique déclare **vingt-trois clés d’accomplissement**. Longtemps, une seule avait un
producteur — `exam.90-minutes` — et les quatre portes étaient donc fermées définitivement, sans que
rien ne le signale. `project.console` en a désormais un : une soumission de projet dont **toutes**
les suites d’acceptation passent, réellement exécutées dans le bac à sable, enregistrée en
`AutomaticTests`. La clé satisfaite est déclarée par le contenu, dans le manifeste du projet, et non
déduite du code.

**Un producteur déclaré n’est pas un producteur qui fonctionne.** Le rejeu du 17 août 2026 a montré
que le trajet de soumission était rompu en deux endroits — le contrat d’exécution rejetait la cible
que le produit construit, et deux projets nommaient mal leur manifeste de suite — si bien qu’aucun de
ces producteurs n’avait jamais pu se déclencher. Les tests hors ligne restaient verts. Depuis,
`ProjectSubmissionDockerRunnerTests` exécute le trajet dans un conteneur réel, et
`ProjectCorrectnessTests` fige la convention de nommage sans Docker. Un producteur inventorié comme
tel doit désormais l’être de bout en bout.

Une soumission faite en mode manuel n’est pas une réussite : le domaine refuse une réussite non
vérifiée automatiquement, et `IsVerifiedAchievement` rejette de toute façon `ManualDeclaration`.
`ProjectAchievementTests` tient l’inventaire : toute clé exigée par une porte figure soit parmi les
clés produites, soit parmi les **treize** clés déclarées sans producteur, et ce dernier nombre ne
peut que descendre.

Deux clés en sont d’abord sorties, toutes deux au-delà de la porte A. `code-review` : `project-code-review-001`
(piste senior, Senior S7) fait noter des diffs à défauts plantés par des suites d’acceptation. `ef-core` :
`project-orders-database-001` fait écrire trois méthodes qui interrogent et modifient une **vraie
base** — le bac à sable embarque `Microsoft.EntityFrameworkCore` et `Microsoft.Data.Sqlite` parmi ses
assemblies approuvées, ce qui rend l’exécution réelle et non simulée. Les trois suites vérifient
respectivement la traversée du modèle, le nombre de requêtes SQL émises (compté par un intercepteur,
non estimé) et la persistance d’une écriture relue depuis un contexte neuf.

**Six autres clés en sont sorties le 18 août 2026**, chacune par un projet vérifiable qui suit
exactement ce modèle — clé déclarée par le manifeste, suites exécutées dans le bac à sable, trajet
prouvé hors Docker par `ProjectCorrectnessTests`, ouverture de porte prouvée par
`MasteryRulesTests.EachNewlyProducedKeyOpensItsGateOnlyOnceVerified` :

| Clé | Projet | L’artefact réellement exercé |
|---|---|---|
| `validation-errors` | `project-validation-pipeline-001` | `Validator.TryValidateObject` traverse les attributs du vrai pipeline DataAnnotations — celui qu’ASP.NET Core exécute —, un attribut personnalisé l’étend, et les manquements se projettent dans un contrat 200/422 stable |
| `logs` | `project-operations-log-001` | le vrai `ILogger` de `Microsoft.Extensions.Logging` : niveaux et seuil du puits, caviardage avant émission, corrélation par `BeginScope` — le puits capturé est la seule source du résultat |
| `incident.simulated` | `project-incident-drill-001` | un service simulé déterministe que le code détecte sur deux points soutenus, atténue par la bonne action et vérifie rétabli sur les signaux relus — la clé nomme un incident simulé, c’est ce qui est exercé |
| `performance` | `project-query-budget-001` | le squelette fonctionne mais interroge élément par élément ; l’apprenant rend le même résultat sous un budget d’allers-retours compté par un intercepteur — un avant mesuré, un après au même volume |
| `security` | `project-abuse-hardening-001` | HMAC recalculé et comparé en temps constant, canonisation de chemin, fenêtre anti-rejeu — éprouvés par des cas cachés d’abus que l’énoncé ne liste pas |
| `feature.autonomous` | `project-autonomous-feature-001` | une spécification contractuelle complète, aucun découpage ni indice : la fonctionnalité se livre sur le seul contrat, dans les limites déclarées du bac à sable |

Aucun de ces producteurs ne touche au projet final, qui reste guidé et sans corrigé par décision
figée : chacun est un livrable dédié, borné, dont l’artefact est celui que sa clé nomme.

#### Diagnostic des treize clés restantes

L’inventaire ne se contente plus de compter : chaque clé sans producteur porte **ce qui lui manque et
pourquoi**, et le test refuse une justification trop courte pour être actionnable. Le classement suit
un principe unique — *un accomplissement ne s’attribue que sur une preuve qui exerce le même artefact
que son intitulé nomme*. Les exercices `api-*`, `docker-*`, `ci-*` et `tests-*` sont des fonctions
pures qui raisonnent **sur** leur sujet sans le pratiquer ; en tirer l’accomplissement correspondant
fabriquerait le faux signal que cette politique existe pour empêcher.

| Blocage | Clés | Ce qu’il faut |
|---|---:|---|
| Contenu vérifiable manquant | **7** | un livrable exécuté et vérifié côté serveur, ou un canal de preuve qui n’existe pas encore |
| Jugement humain requis | **6** | rien de ce que du code peut produire |

Les six clés du second groupe — Git propre, présentation de 10 minutes, entretien blanc, architecture
pragmatique, anglais, défense finale — ne descendront jamais par du code. Les compter comme une dette
technique conduirait à fabriquer une preuve automatique de ce qui ne s’automatise pas, par exemple un
entretien noté par une suite de tests.

Ce qui existe pour elles est le protocole de [`HUMAN_REVIEW.md`](HUMAN_REVIEW.md), **branché au
produit depuis le 18 août 2026** : la page `/human-review` enregistre l’attestation d’un relecteur
humain nommé, sous un troisième type de preuve — `MasteryVerificationKind.HumanAttestation` — que
les règles admettent **exclusivement** pour ces six clés et pour la composante Explication.
`MasteryPolicyCatalog.HumanJudgementKeys` est la liste fermée ; sur toute autre clé, une attestation
vaut zéro, exactement comme une déclaration. L’enregistrement refuse l’auto-attestation (contrôle
d’identité contre le profil), la grille incomplète, le critère obligatoire non observé, la durée
au-dessous du minimum de la grille et le rejeu d’une même revue. Partout, l’attestation s’affiche
« attestée par un relecteur humain, non vérifiée par la machine » : le produit ne peut vérifier ni
l’identité du relecteur ni ce qu’il a observé, et le dit au lieu de le laisser croire.
`ManualDeclaration` — l’apprenant seul — reste refusée sans condition.

**Une route écartée, une autre empruntée.** L’exigence « EF Core » paraissait branchable sur les
validations du laboratoire SQL : cinq scénarios `ef-*` sont publiés, ils exécutent du vrai code EF Core
et leur résultat est comparé côté serveur. La vérification a montré le contraire.
`FileSystemSqlScenarioSource` n’expose que les scénarios dont le contrat déclare le mode `sql`, et les
scénarios EF déclarent le mode `ef` : ils sont donc absents du laboratoire. Par ailleurs un scénario EF
n’est tirable en examen que s’il porte un dossier `exam/`, et **trois des cinq n’en ont pas**. Cette
route reste fermée, et `EfScenarioReachabilityTests` fige le diagnostic pour que la prochaine tentative
le trouve écrit au lieu de le redécouvrir.

La clé a finalement été produite par une autre route, qui satisfait la même exigence de preuve : un
projet vérifiable dont les suites s’exécutent dans le bac à sable, où EF Core et SQLite sont des
assemblies approuvées. Ce n’est pas un contournement du diagnostic ci-dessus mais son complément —
le problème n’était pas que le produit ne sache pas exécuter EF Core, c’était qu’aucun **chemin
vérifié** n’y menait.

**Cinq clés examinées puis refusées.** Le même travail a cherché à brancher `api.functional`,
`tests.unit`, `tests.integration`, `docker` et `ci` sur des projets vérifiables. Aucune ne passe la
règle d’admission :

| Clé | Pourquoi la preuve serait fausse |
|---|---|
| `api.functional` | le bac à sable démarre avec `--network none` et n’approuve aucune assembly d’hébergement HTTP : un projet ne pourrait faire décider que des règles, sans exercer une ligne de HTTP |
| `tests.unit` | le runner invoque une méthode statique nommée d’avance ; il ne découvre pas des tests écrits par l’apprenant, et lui faire rendre un rapport d’assertions serait falsifiable |
| `tests.integration` | une base réelle est atteignable, mais l’artefact nommé reste des tests écrits par l’apprenant — exercer une base n’est pas écrire un test d’intégration |
| `docker` | la soumission est du C# compilé dans un conteneur déjà construit ; rien n’y bâtit ni n’y exécute d’image |
| `ci` | aucun pipeline ne s’exécute dans le bac à sable, et le workflow du laboratoire tourne hors produit, sans preuve collectée par le serveur |

Les cinq restent dans l’inventaire avec leur diagnostic. Leur produire un accomplissement sur un
exercice qui *raisonne sur* le sujet aurait fait descendre le compteur sans rien débloquer : c’est
exactement le faux signal que cet inventaire existe pour empêcher.

#### Les sept clés restantes : deux canaux conçus, aucun encore admis

La revue du 18 août 2026 a classé les sept clés « contenu manquant » qui subsistent. Aucune n’est
produisible sur les canaux existants sans mentir — c’est ce que les refus ci-dessus établissent —
mais deux **canaux nouveaux** ont été conçus, sans être implémentés, parce que leurs conditions
d’admission ne sont pas encore démontrées :

- **Le canal des suites hébergées**, pour `api.functional`, `authn-authz` et une partie de
  `deployment`. L’image du bac à sable embarquerait ASP.NET Core et un harnais
  `WebApplicationFactory` fourni par la suite — jamais par l’apprenant — dont le serveur de test
  est en mémoire : `--network none` resterait vrai, aucun port ne s’ouvrirait. La soumission
  resterait une classe de l’apprenant compilée avec le harnais. Conditions avant toute
  implémentation : chiffrer la taille d’image et sa surface d’attaque, étendre la liste
  d’assemblies approuvées, redémontrer une à une les garanties de
  `DockerCodeRunnerSecurityTests`, et reconstruire l’empreinte épinglée. Tant que ces conditions
  ne sont pas tenues, les clés restent inventoriées.
- **Le canal des suites à mutants**, pour `tests.unit` et `tests.integration`. L’artefact que ces
  clés nomment est un test écrit par l’apprenant ; le refus documenté tient au fait qu’un rapport
  d’assertions rendu par la soumission serait falsifiable. Le canal conçu inverse la charge : la
  soumission de l’apprenant est sa **suite de tests**, et le runner la compile successivement
  contre une implémentation correcte cachée puis contre des mutants cachés — la suite est admise
  si elle passe sur l’implémentation correcte et échoue sur chaque mutant. Rien n’y est
  falsifiable par une valeur en dur, puisque les implémentations éprouvées sont secrètes.
  Condition : un nouveau type de suite dans le contrat du conteneur, avec ses règles de quota.

Trois clés n’ont pas de canal honnête identifié. `docker` garde une piste locale — le produit
dispose d’un client Docker en mode CLI et pourrait inspecter une image construite par l’apprenant
(utilisateur non root, HEALTHCHECK, absence de secret dans les couches), à condition de lier
l’image au travail demandé pour fermer le rejeu — non instruite. `ci` et `deployment` restent
au diagnostic d’origine : rien de ce que le serveur collecte ne prouve un pipeline ou un
déploiement réels, et une preuve déclarée vaudrait zéro.

| Porte | Conditions cumulatives |
|---|---|
| A — Junior fiable | C# ≥85, débogage ≥80, SQL ≥75, 10 exercices vérifiés sans aide et non contaminés, mini-projet console vérifié, examen sans aide de 90 minutes |
| B — Backend .NET | A, API fonctionnelle, EF Core, validation/erreurs, tests unitaires et d’intégration, Git propre, présentation de 10 minutes |
| C — Équipe moderne | B, Docker, CI, authN/authZ, logs, déploiement, incident simulé, entretien blanc |
| D — Intermédiaire en construction | C, performance, sécurité, architecture pragmatique, fonctionnalité autonome, revue de code, anglais professionnel, défense finale |

Chaque condition absente produit un motif de blocage. Une porte ne s’ouvre jamais par moyenne ou compensation. Depuis 07C, l’accomplissement d’examen de la porte A exige une tentative réussie sans aide dont la durée configurée est d’au moins 90 minutes. La banque de référence de 30 minutes ne le satisfait pas.

### Ce qu’un domaine peut réellement atteindre

Une version antérieure de ce document affirmait que la porte A était « franchissable par le travail »
dès lors que ses deux accomplissements avaient un producteur. **C’était faux**, et pour une raison
qui n’avait pas été cherchée : le producteur d’un accomplissement ne dit rien du plafond d’un score.

Deux composantes n’avaient aucun producteur — explication et quiz — et la pratique comme les examens
attribuaient leurs observations à un domaine codé en dur. Un exercice `api-*` alimentait le score C#
et jamais le score Api. Le poids d’une composante sans preuve n’étant jamais redistribué, il en
résultait des plafonds inférieurs aux seuils, donc des conditions de porte hors d’atteinte :

| Domaine | Plafond mesuré au départ | Plafond aujourd’hui | Seuil |
|---|---:|---:|---:|
| C# | 85 — atteignable seulement à 100 partout | **90** | 85 |
| Débogage | **60** | **90** | 80 |
| Api, Tests | **15** | **90** | 85 |
| Sécurité | **15** | **90** | 80 |
| SQL | 70 | **90** | 75 |
| Docker, CI, Architecture, Performance, Anglais | **0** | **90** | 80 |

Trois corrections, sans qu’aucun poids ni seuil ne bouge : le domaine d’une observation de pratique
ou d’examen vient désormais de la compétence de l’élément travaillé — `MasterySkillDomains` en est la
source unique ; le quiz de leçon, déjà corrigé côté serveur, produit une observation ; et la banque
de cartes couvre les exercices publiés **et** les 40 scénarios SQL — 364 cartes sur 182 éléments à ce
jour. `ReviewCardQualityTests` en tient le plancher et exige que tout exercice neuf d’un domaine
critique arrive avec ses cartes : sans elles, il alimenterait la pratique sans jamais alimenter la
rétention, et le domaine replafonnerait sous son seuil.

`MasteryReachabilityTests` calcule ce plafond à partir d’un inventaire des producteurs et refuse
qu’un domaine plafonne **à** son seuil ou en dessous — un plafond égal au seuil n’est franchi qu’avec
cent sur chaque composante produite, ce qui est un blocage déguisé en objectif.

### L’explication : trois routes cherchées, trois routes refusées

L’**explication reste sans producteur**, et cette section dit pourquoi au lieu de l’affirmer. Elle pèse
10 %, son poids n’est jamais redistribué, et le plafond général est donc de **90**. C’est la seule
composante inventoriée comme non produite.

Une précision qui change la priorité : **90 est au-dessus des deux seuils** — 80 pour un domaine
ordinaire, 85 pour un domaine critique. L’absence de producteur coûte un score qui n’atteint jamais
cent ; elle ne ferme aucun domaine et aucune porte. `MasteryRulesTests` fige ce calcul, précisément
pour qu’une reprise future ne justifie pas un producteur fabriqué en invoquant un blocage inexistant.

Trois routes ont été examinées.

**1. La carte à choix sur le « pourquoi ».** Attacher à un exercice résolu une carte dont la question
porte sur la raison de la solution, corrigée côté serveur par comparaison à une réponse privée, puis la
projeter en Explication. Le moteur est honnête — c’est celui de la rétention espacée. La projection ne
l’est pas : **reconnaître la bonne réponse parmi quatre n’est pas produire un raisonnement**. C’est
exactement l’acte que la composante Quiz mesure déjà, à 5 %. La reprojeter en Explication paierait deux
fois le même geste ; et une carte attachée à un exercice déjà couvert alimenterait rétention *et*
explication — 25 % du score pour un clic. La règle d’éligibilité refuse d’ailleurs cette route
d’elle-même : seul `ServerRubric` admet une observation d’explication, jamais `ReviewEngine` ni
`QuizEngine`.

**2. L’explication personnelle du protocole de pratique.** Elle existe déjà, elle est stockée, et le
serveur la contrôle : longueur minimale, et refus si elle recopie la solution ou la variante. Mais ce
contrôle **mesure l’effort, pas la justesse** — il prouve que l’apprenant a écrit assez de mots
distincts, jamais que ce qu’il a écrit est vrai. S’en servir comme preuve reviendrait à noter la
longueur d’un texte. Second défaut, dirimant : cette étape n’est atteignable qu’**après consultation de
la solution**, c’est-à-dire sur un exercice que la politique tient pour contaminé.

**3. La rubrique déterministe.** `CONTENT_GUIDE.md` en décrit une depuis l’origine — concepts
obligatoires, synonymes acceptés, contradictions, exemples attendus, score par critère — et c’est ce
que `ServerRubric` attendait. Elle n’a jamais été construite, et l’examen montre qu’elle ne le sera pas
honnêtement. Ou bien les concepts exigés sont publiés, et la rubrique note une transcription ; ou bien
ils sont secrets, et elle note une devinette. Quant aux exigences structurelles — « au moins un lien
causal » — un appariement déterministe ne peut les chercher que par connecteurs, et « X parce que Y »
les satisfait avec n’importe quels X et Y. Le guide tranche lui-même le seul recours qui resterait :
*« un LLM optionnel ne peut pas modifier seul la maîtrise »*.

**Conclusion.** Expliquer, au sens où ce parcours emploie le mot, c’est **produire un compte rendu
causal dans ses propres mots**. Tout substitut vérifiable par une machine mesure autre chose : la
reconnaissance (choix), l’effort (longueur), ou la prédiction (« que rend ce code si je change ceci »).
Les trois sont des signaux utiles — deux sont déjà mesurés ailleurs — mais aucun n’est le geste que la
composante nomme. Ce qui juge la production d’un compte rendu, c’est un lecteur.

L’explication rejoint donc, en nature, les six exigences classées « jugement humain » de
`ProjectAchievementTests` : elle est la seule **composante** de cette classe. Elle porte à ce titre la
septième grille de [`HUMAN_REVIEW.md`](HUMAN_REVIEW.md), dont le critère central — affirmer un lien
causal que le relecteur vérifie en direct — est exactement ce qu’aucune correction automatique ne sait
faire. Rien n’y est enregistré comme preuve automatique.

**La quatrième route, empruntée le 18 août 2026 : le lecteur lui-même.** Puisque ce qui juge un
compte rendu est un lecteur, c’est un lecteur qui l’atteste — la septième grille, saisie sur
`/human-review`, produit une observation d’Explication sous `HumanAttestation`, portée par
l’exercice que le relecteur a choisi dans l’historique et par son domaine. Le plafond de 100 n’est
donc atteignable que pour un profil **attesté**, domaine par domaine ; un profil sans relecteur
plafonne toujours à 90, ce que `MasteryReachabilityTests` continue de mesurer comme la limite du
produit seul. Aucun poids, aucun seuil n’a bougé ; les trois routes automatiques restent refusées.

### Portes B, C et D

Elles restent fermées, mais plus pour les mêmes raisons, et l’écart est mesuré. Treize de leurs
exigences n’ont aucun producteur, ce que `ProjectAchievementTests` consigne clé par clé avec le
diagnostic de ce qui manque à chacune ; huit sont produites au-delà de la porte A. L’état exact au
18 août 2026 :

- **Porte B** : `ef-core` et `validation-errors` sont produites ; `api.functional`, `tests.unit` et
  `tests.integration` attendent les canaux conçus ci-dessus ; `git.clean` et
  `presentation.10-minutes` exigent un jugement humain — **attestables** depuis `/human-review`.
- **Porte C** : `logs` et `incident.simulated` sont produites ; `docker`, `ci`, `authn-authz` et
  `deployment` restent sans canal admis ; `interview.mock` exige un jugement humain — attestable.
- **Porte D** : toutes ses exigences propres sont soit produites — `performance`, `security`,
  `feature.autonomous`, `code-review` — soit humaines et attestables — `architecture.pragmatic`,
  `english`, `project.final-defense`. La porte D n’attend plus aucun contenu qui lui soit propre :
  elle attend la porte C.

`MasteryRulesTests` prouve, clé par clé, que chaque exigence nouvellement produite ouvre réellement
sa porte sur un profil fabriqué — fermée sans la clé, fermée sur déclaration manuelle, ouverte sur
preuve `AutomaticTests` — sur le modèle de la preuve historique de la porte A. Il prouve de la même
façon que chaque exigence humaine s’ouvre sur une `HumanAttestation` et sur elle seule, et qu’une
attestation posée sur une clé vérifiable par la machine ne compte pour rien.

Conséquence mesurée : un apprenant **avec un relecteur humain** peut désormais satisfaire toutes les
exigences propres de la porte D et les exigences humaines de B et C ; ce qui ferme encore B et C est
exactement l’ensemble des clés vérifiables sans canal admis — `api.functional`, `tests.unit`,
`tests.integration`, `docker`, `ci`, `authn-authz`, `deployment`. Un apprenant **sans relecteur**
lit sur `/mastery` et `/human-review` exactement ce qui lui manque, et pourquoi aucune machine ne le
remplacera.

**Limite du quiz, assumée** : seule une réussite est persistée, si bien qu’une réponse juste au
cinquième essai vaut la première. À 5 % de poids et sous la règle « accumulation de quiz faciles →
poids maximal 5 % » de la matrice anti-contournement, l’effet reste borné.

## Persistance et audit

`PracticeLearningAttempts`, `SqlLearningAttempts` et les observations DebugLab existantes sont append-only. Une identité ou un diagnostic rejoué avec un contenu différent est refusé. `MasteryProjections` conserve un snapshot, la politique sérialisée, la révision de politique et la révision quotidienne des preuves. La clé unique profil/politique/preuves rend le recalcul concurrent idempotent.

La date UTC entre dans la révision de calcul afin que récence et expiration soient recalculées sans réécrire les anciennes projections. Un changement de politique reçoit une nouvelle version/révision ; un ancien snapshot reste lié à sa politique figée et n’est jamais réinterprété.

## Matrice anti-contournement

| Tentative de contournement | Réponse v1 |
|---|---|
| accumulation de quiz faciles | poids maximal 5 %, aucune preuve autonome |
| H1 à H4 | score plafonné à 90/80/70/60 |
| solution consultée | pratique du même exercice à zéro ; exercice exclu des comptes autonomes |
| déclarations manuelles/aléatoires | conservées comme contexte mais non admissibles |
| même exercice en boucle | rendement décroissant et variété minimale de trois exercices |
| preuve ancienne | pondération réduite puis exclusion à 90 jours ; preuve autonome récente exigée |
| moyenne élevée avec compétence faible | seuil par domaine et conditions de porte exactes |
| composante absente | zéro, sans redistribution |
| faux examen ou faux livrable | type de vérification serveur obligatoire |
| rejeu/modification d’observation | unicité, append-only et refus fermé |
| auto-attestation | le relecteur ne peut pas porter le nom du profil ; un profil sans nom ne peut rien enregistrer |
| attestation sur une clé vérifiable par la machine | grille inexistante au protocole, et clé hors de `HumanJudgementKeys` : vaut zéro |
| rejeu d’une même attestation | unicité profil/exigence/date, et un doublon n’ajouterait rien aux portes |

## Vérifications manuelles

Contrôler sur `/mastery` un profil vide, un profil avec déclaration manuelle, un profil avec réussite assistée et un profil avec preuves automatiques partielles. Dans chaque cas, vérifier les composantes absentes à zéro, les motifs de blocage, les portes fermées et l’absence de commande de modification du score.

## Commandes

```powershell
dotnet build --no-restore
dotnet test --no-build --filter "Category=MasteryAntiGaming"
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
```
