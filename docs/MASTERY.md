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

La banque compte **230 cartes pour 115 exercices**. Les quatre domaines critiques dont la pratique
passe par des exercices — C#, débogage, API, tests — sont couverts intégralement, et deux règles de
`ReviewCardQualityTests` le tiennent : tout exercice d’un de ces domaines porte ses cartes, et le
domaine déclaré par une carte est celui déduit de la première compétence de son exercice. Une carte
mal classée alimenterait la rétention d’un domaine que l’apprenant n’a pas travaillé.

Le domaine SQL fait exception : sa pratique passe par des scénarios, non par des exercices, aucun
exercice ne porte de compétence `sql.*`, et aucune carte ne l’alimente à ce jour. Le couvrir demande
une clé de carte généralisée de l’exercice vers l’élément pratiqué, quel qu’il soit.

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
clés produites, soit parmi les **dix-neuf** clés déclarées sans producteur, et ce dernier nombre ne
peut que descendre.

Deux clés en sont sorties, toutes deux au-delà de la porte A. `code-review` : `project-code-review-001`
(piste senior S31) fait noter des diffs à défauts plantés par des suites d’acceptation. `ef-core` :
`project-orders-database-001` fait écrire trois méthodes qui interrogent et modifient une **vraie
base** — le bac à sable embarque `Microsoft.EntityFrameworkCore` et `Microsoft.Data.Sqlite` parmi ses
assemblies approuvées, ce qui rend l’exécution réelle et non simulée. Les trois suites vérifient
respectivement la traversée du modèle, le nombre de requêtes SQL émises (compté par un intercepteur,
non estimé) et la persistance d’une écriture relue depuis un contexte neuf.

#### Diagnostic des dix-neuf clés restantes

L’inventaire ne se contente plus de compter : chaque clé sans producteur porte **ce qui lui manque et
pourquoi**, et le test refuse une justification trop courte pour être actionnable. Le classement suit
un principe unique — *un accomplissement ne s’attribue que sur une preuve qui exerce le même artefact
que son intitulé nomme*. Les exercices `api-*`, `docker-*`, `ci-*` et `tests-*` sont des fonctions
pures qui raisonnent **sur** leur sujet sans le pratiquer ; en tirer l’accomplissement correspondant
fabriquerait le faux signal que cette politique existe pour empêcher.

| Blocage | Clés | Ce qu’il faut |
|---|---:|---|
| Contenu vérifiable manquant | **13** | un livrable exécuté et vérifié côté serveur |
| Jugement humain requis | **6** | rien de ce que du code peut produire |

Les six clés du second groupe — Git propre, présentation de 10 minutes, entretien blanc, architecture
pragmatique, anglais, défense finale — ne descendront jamais par du code. Les compter comme une dette
technique conduirait à fabriquer une preuve automatique de ce qui ne s’automatise pas, par exemple un
entretien noté par une suite de tests.

Ce qui **peut** être fait pour elles est écrit dans [`HUMAN_REVIEW.md`](HUMAN_REVIEW.md) : une grille
observable par exigence, le format de preuve accepté, et une procédure d’attestation qui vit
**entièrement hors du système de maîtrise**. Aucune attestation n’entre dans la base, ne change un
score ni n’ouvre une porte — `IsEligible` refuse `ManualDeclaration` sans condition, et c’est la
garantie qui rend le protocole publiable sans risque de faux signal.

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

### Portes B, C et D

Elles restent fermées. Dix-neuf de leurs exigences d’accomplissement n’ont aucun producteur, ce que
`ProjectAchievementTests` consigne clé par clé, avec le diagnostic de ce qui manque à chacune. Deux
seulement sont satisfaites — `code-review` et `ef-core` — et un apprenant ne lit donc pas « Porte B —
bloquée » par accident de configuration, mais parce que ses autres exigences ne sont pas satisfiables
en l’état.

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
