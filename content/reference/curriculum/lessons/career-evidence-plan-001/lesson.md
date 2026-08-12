# Transformer le parcours en preuves

## Objectif observable

À la fin de cette leçon, vous saurez distinguer une affirmation d'une preuve, constituer un dossier que
quelqu'un d'autre peut vérifier sans vous, et formuler ce que vous savez faire sans le surestimer ni le
minimiser.

## Prérequis

- Avoir lu `final-defense-english-001` et savoir défendre une décision.
- Avoir lu `final-project-evidence-001` et savoir produire un artefact vérifiable.

## Intuition

« Je connais .NET » est une affirmation. Elle ne dit ni ce que vous avez construit, ni ce que vous
avez décidé, ni ce que vous avez appris d'un échec. Elle est invérifiable, donc elle ne pèse rien.

« Voici un service qui traite ce parcours, voici la décision d'architecture que j'ai prise et ce que
j'ai écarté, voici l'incident que j'ai diagnostiqué et ce que j'ai changé ensuite » est une preuve.
Elle se vérifie, elle se discute, et elle survit à l'entretien.

Ce cours ne garantit ni un emploi, ni une rémunération, ni une promotion. Ce qu'il produit, si vous
faites le travail, ce sont des preuves — et ce qu'on en fait ensuite dépend d'un marché, d'un contexte
et de circonstances qu'aucun cours ne contrôle.

## Explication

**Une preuve a trois propriétés.** Elle est **vérifiable** — quelqu'un peut l'ouvrir et la lire. Elle
est **datée** — elle situe le moment où vous saviez faire cela. Et elle est **attribuable** — on
distingue ce que vous avez fait de ce qui vous a été fourni.

Un dépôt public dont vous pouvez expliquer chaque décision est une preuve. Une liste de technologies
n'en est pas une.

**Le dossier se construit pendant, pas après.** Reconstituer six mois de travail à la fin donne un
document appauvri : les décisions sont oubliées, les incidents résumés à « ça n'a pas marché ». Un
journal tenu au fil du parcours — décision, contexte, résultat — coûte quelques minutes par semaine et
constitue la matière brute de tout le reste.

**Ce que ce cours produit concrètement.** Un projet final avec ses jalons et leurs preuves. Un
historique de commits lisible. Des exercices résolus avec les cas de bordure que vous avez identifiés.
Des incidents traités dans les scénarios de diagnostic, avec leur chronologie. Des décisions
consignées au format complet. C'est un dossier honnête, et c'est déjà plus que ce que présentent
beaucoup de candidats.

**Ce que ce cours ne produit pas.** L'expérience d'un système en production avec de vrais
utilisateurs, la coordination d'une équipe, la contrainte d'une exploitation à long terme, et la
connaissance d'un domaine métier réel. Ces manques se nomment plutôt qu'ils ne se masquent : les
nommer dans un entretien vous distingue plus sûrement que de prétendre le contraire.

**Formuler sans surestimer ni minimiser.** Trois niveaux honnêtes. *J'ai fait* — je l'ai construit et
je peux l'expliquer. *J'ai lu et pratiqué* — je connais les principes et j'ai écrit du code d'exercice.
*Je n'ai pas fait* — je sais ce que c'est et je n'y ai pas touché.

Le troisième niveau est celui qui donne du crédit aux deux premiers. Un candidat qui reconnaît une
limite est cru sur le reste.

**Le progrès se mesure sur des observables.** Un exercice résolu sans indice là où il en fallait trois.
Un défaut trouvé en dix minutes là où il en fallait deux heures. Un jalon dont l'estimation était juste
à un demi-jour près. Ces mesures valent mieux qu'un sentiment de progression, qui est notoirement peu
fiable dans les deux sens.

**Un plan se révise sur des faits.** Choisir un axe pour les huit prochaines semaines, définir la
preuve qui montrera qu'il est acquis, et vérifier à échéance. Un plan sans preuve attendue n'est qu'une
intention ; un plan jamais relu n'est qu'un document.

**Ce qui ne va pas dans le dossier.** Un code copié présenté comme le vôtre. Une compétence affichée
que vous ne pourriez pas défendre dix minutes. Un projet dont vous ne savez plus expliquer une
décision. Chacune de ces trois choses se découvre en entretien, et coûte alors bien plus que ce qu'elle
aurait rapporté.

## Exemple commenté

Le test qui sépare une affirmation d'une preuve :

```csharp
public static bool IsEvidence(bool verifiableByAnother, bool dated, bool attributable)
{
    // Les trois ensemble. Vérifiable mais non daté : on ne sait pas quand vous saviez.
    // Daté mais non attribuable : on ne sait pas ce que vous avez fait vous-même.
    // Attribuable mais non vérifiable : c'est une affirmation, pas une preuve.
    return verifiableByAnother && dated && attributable;
}
```

Le dossier, tel qu'il se présente :

```text
Dossier — service de commandes (projet final)

Ce que j'ai construit
  Parcours complet : création, vérification de stock, persistance, consultation,
  expédition. Un déployable, quatre couches, dépendances orientées vers le domaine.

Décisions que je peux défendre
  003  persistance relationnelle plutôt que documentaire — invariant commande/lignes
  007  un seul déployable — développeur seul, profil de charge unique
  011  autorisation vérifiée sur la ressource, pas seulement sur l'action

Preuves
  - suite de tests : 148 tests, dont 12 sur les frontières de la règle de remise
  - chronologie d'un incident diagnostiqué : latence due à un accès en boucle,
    mesure avant et après, un seul changement appliqué
  - historique de commits : un commit par intention, messages expliquant le pourquoi

Ce que je n'ai pas fait
  - exploitation d'un service avec des utilisateurs réels sur la durée
  - travail en équipe sur une base de code partagée
  - domaine métier réel avec ses exceptions non écrites
```

Et le plan, révisable sur une preuve attendue :

```text
Axe des huit prochaines semaines : tests d'intégration sur base réelle

Pourquoi     c'est le niveau que je maîtrise le moins ; mes preuves actuelles sont
             surtout unitaires.

Preuve attendue à la semaine 8
             une suite d'intégration sur base jetable, avec isolation par exécution,
             restauration vérifiée, et un test qui échoue si une contrainte d'unicité
             est retirée du schéma.

Vérification semaine 4  la base jetable est en place et la suite tourne en local.
Vérification semaine 8  la suite tourne dans la chaîne de construction, en parallèle.

Si la preuve n'est pas produite : identifier l'obstacle réel plutôt que reconduire
l'axe à l'identique.
```

## Contre-exemple et erreur fréquente

```text
Profil

  Compétences : C#, .NET, ASP.NET Core, EF Core, SQL, Docker, CI/CD, Azure,
                microservices, architecture hexagonale, DDD, CQRS, event sourcing,
                Kafka, Redis, GraphQL, Terraform, sécurité applicative

  Expérience  : formation intensive de 24 semaines

  Projets     : « application de gestion complète » (dépôt privé, non partageable)

  Objectif    : poste d'architecte, rémunération cible en forte hausse
```

Cinq défauts.

La liste de dix-sept technologies contient nécessairement des éléments que le candidat ne pourrait pas
défendre dix minutes. Un entretien en teste deux ou trois au hasard, et la découverte d'un seul
élément non maîtrisé jette un doute sur les seize autres.

« Formation intensive » sans dire ce qui a été construit ne donne aucune matière. L'interlocuteur ne
peut rien vérifier ni rien discuter.

Le projet en dépôt privé non partageable annule sa valeur de preuve : ce qui ne peut pas être ouvert
n'est qu'une affirmation de plus.

Aucune limite n'est nommée. L'absence de « ce que je n'ai pas fait » retire du crédit à tout le reste,
parce qu'un lecteur expérimenté sait qu'un parcours de vingt-quatre semaines a nécessairement des
manques.

Enfin, viser un poste d'architecte au sortir d'une formation confond un objectif à long terme avec une
étape atteignable. Le coût n'est pas l'ambition : c'est que la candidature ne correspond à aucun
poste réel, et n'obtient donc aucun entretien.

## Vérification de compréhension

Prenez trois lignes de votre profil actuel. Pour chacune, dites si c'est une affirmation ou une preuve,
et ce qu'il faudrait produire pour la transformer.

:::quiz
id=career-evidence-plan-001-check
question=Pourquoi nommer explicitement ce que vous n'avez pas fait ?
option=Parce que la plupart des recruteurs vérifient systématiquement chaque compétence affichée
option=Parce qu'une limite reconnue rend crédible tout le reste : un lecteur expérimenté sait qu'un parcours a des manques, et une liste sans aucun manque n'est pas crue
option=Parce que les compétences non maîtrisées doivent légalement être signalées
correct=1
success=Correct : le niveau « je n'ai pas fait » est ce qui donne du poids aux deux autres.
retry=Relisez le passage sur les trois niveaux de formulation, et demandez-vous ce que pense un lecteur devant une liste sans aucune limite.
:::

## Exercice guidé

Cette leçon se pratique sur vos propres artefacts, produits pendant les vingt-quatre semaines.

1. Ouvrez `/progress` et relevez trois observables mesurés : exercices résolus sans indice, délai de
   diagnostic sur un scénario, écart entre estimation et réel sur un jalon.
2. Ouvrez votre projet final et listez trois décisions que vous pouvez défendre au format en quatre
   temps.
3. Écrivez la colonne « ce que je n'ai pas fait », avec au moins quatre entrées.
4. Appliquez à chaque élément le test en trois propriétés — vérifiable, daté, attribuable — et corrigez
   ce qui n'en est pas une.

## Exercice autonome

Constituez votre dossier complet.

Produisez : la liste de ce que vous avez construit, cinq décisions défendables, les preuves associées à
chacune, les observables mesurés du parcours, la colonne des limites, et un plan à huit semaines avec
sa preuve attendue et ses deux points de vérification. Faites relire l'ensemble par quelqu'un qui ne
connaît pas le projet et notez ce qu'il ne comprend pas.

## Débogage

Un ticket indique : « Les candidatures ne donnent aucun retour, alors que le travail technique est
réel. »

1. **Symptôme** : un travail existant ne se transmet pas.
2. **Hypothèse** : le dossier présente des affirmations plutôt que des preuves, ou vise des postes sans
   correspondance avec ce qui est démontré.
3. **Preuve** : appliquez le test en trois propriétés à chaque élément, et comparez les postes visés à
   ce que le dossier démontre réellement.
4. **Prévention** : remplacer chaque affirmation par un artefact ouvrable, et aligner les postes visés
   sur les preuves disponibles.

## Entretien

Question posée à voix haute : *qu'avez-vous appris ces derniers mois, et comment le savez-vous ?*

Une réponse solide donne un exemple construit plutôt qu'une liste, cite un observable qui a changé,
nomme un échec et ce qu'il a modifié dans la façon de travailler, et reconnaît spontanément une limite
sans qu'on ait à la chercher.

## Résumé

- Une preuve est vérifiable, datée et attribuable ; une affirmation ne l'est pas.
- Le dossier se constitue pendant le parcours, pas à la fin.
- Trois niveaux honnêtes : j'ai fait, j'ai pratiqué, je n'ai pas fait.
- Le progrès se mesure sur des observables, pas sur un sentiment.
- Ce cours produit des preuves ; il ne garantit ni poste ni rémunération.

## Cartes de révision

Question : que vaut un projet dans un dépôt non partageable ? Réponse attendue : rien comme preuve —
ce qui ne peut être ouvert reste une affirmation.

Question : quel niveau de formulation donne du crédit aux deux autres ? Réponse attendue : « je n'ai
pas fait », parce qu'il montre que la liste a été honnêtement établie.

## Test de maîtrise

Sans relire, constituez votre dossier : ce que vous avez construit, cinq décisions défendables avec
leurs preuves, trois observables mesurés, la colonne des limites, l'alignement entre les postes visés
et ce qui est démontré, et un plan à huit semaines avec sa preuve attendue et ses points de
vérification.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
