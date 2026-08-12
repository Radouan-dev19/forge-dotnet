# Choisir un hébergement à partir du besoin

## Objectif observable

À la fin de cette leçon, vous saurez partir des contraintes réelles d'un service pour choisir un mode
d'hébergement, énoncer ce que chaque option prend en charge à votre place, et défendre un choix par son
coût total plutôt que par sa nouveauté.

## Prérequis

- Avoir lu `ci-deployment-gates-001` et savoir ce qu'un déploiement exige.
- Avoir lu `docker-images-layers-001` et savoir ce qu'un artefact conteneurisé contient.

## Intuition

Le choix d'hébergement se décide par ce que vous **ne voulez pas** exploiter vous-même. Chaque niveau
de service prend en charge une part supplémentaire — le système, l'exécution, la mise à l'échelle, les
révisions — et vous retire en échange une part de contrôle.

La question utile n'est donc jamais « quelle est la meilleure option » mais « quelles contraintes ce
service a-t-il, et quelle est l'option la plus simple qui les satisfait ».

## Explication

**Partir des contraintes, jamais du catalogue.** Six questions suffisent le plus souvent. Le service
est-il déjà conteneurisé ? A-t-il besoin de plusieurs révisions simultanées, pour un déploiement
progressif ? Doit-il tourner en continu ou par déclenchement ? Quelles sont ses exigences de latence de
démarrage ? Combien d'instances au maximum ? Et quel budget mensuel est acceptable ?

Les réponses éliminent la plupart des options sans qu'aucune comparaison de fonctionnalités ne soit
nécessaire.

**Un service applicatif géré est le choix par défaut.** Il exécute une application web sans que vous
gériez le système : correctifs, certificats, mise à l'échelle de base. Il accepte un artefact publié
ou une image. Pour un service web classique, c'est l'option la plus simple qui satisfait le besoin, et
la simplicité est une propriété qui se paie tous les mois.

**Un hébergement de conteneurs géré ajoute les révisions et l'échelle fine.** Il devient pertinent
quand deux conditions se rencontrent : le service est **déjà conteneurisé**, et il a réellement besoin
de plusieurs révisions coexistantes — pour basculer progressivement le trafic, comme le décrit
`ci-deployment-gates-001`. Une seule des deux conditions ne le justifie pas : conteneuriser pour
obtenir des révisions dont on n'a pas l'usage ajoute du travail sans contrepartie.

C'est exactement la règle que l'exercice de cette leçon fait écrire.

**L'exécution déclenchée convient au travail intermittent.** Une tâche qui s'exécute à réception d'un
message ou selon un calendrier n'a pas besoin d'un service allumé en permanence. Le modèle facturé à
l'usage est alors nettement moins cher — au prix d'une latence au premier appel après une période
d'inactivité, qui doit être acceptable pour l'usage visé.

**Une machine virtuelle est le dernier recours.** Elle donne tout le contrôle et vous laisse toute la
charge : correctifs, sécurisation, supervision, mise à l'échelle. Elle se justifie par une contrainte
précise — un composant système particulier, une licence, une exigence réseau — jamais par confort.

**Le coût total n'est pas le prix affiché.** Il comprend le temps d'exploitation, le coût d'un incident
que vous devez traiter vous-même, et le coût de migration si le choix se révèle mauvais. Une option à
trente euros par mois qui demande deux heures d'attention mensuelle est plus chère qu'une option à
quatre-vingts euros qui n'en demande aucune.

**Le choix se documente avec ses alternatives.** Ce qui a été retenu, ce qui a été écarté, et sur quel
critère. Sans cette trace, la décision sera rejouée à chaque arrivée dans l'équipe, et personne ne
saura si la contrainte qui la justifiait existe encore.

## Exemple commenté

La décision, ramenée à sa règle :

```csharp
public static string HostingChoice(bool requiresContainerRevisions, bool alreadyHasContainer)
{
    // Les deux conditions ensemble. Avoir besoin de révisions sans être conteneurisé
    // signifie d'abord conteneuriser ; être conteneurisé sans besoin de révisions
    // ne justifie pas la complexité supplémentaire.
    return requiresContainerRevisions && alreadyHasContainer ? "container-apps" : "app-service";
}
```

La grille de décision, appliquée à trois services réels :

```text
Service                         Conteneurisé  Révisions  Continu  Choix
------------------------------  ------------  ---------  -------  ---------------------
API de commandes                non           non        oui      service applicatif géré
Passerelle de paiement          oui           oui        oui      conteneurs gérés
Export nocturne de facturation  non           non        non      exécution déclenchée
```

Aucune ligne n'a demandé de comparer des listes de fonctionnalités : les contraintes ont suffi.

Et la trace de décision, telle qu'elle doit être écrite :

```text
Décision : service applicatif géré pour l'API de commandes.

Contraintes retenues
  - trafic continu, latence de premier appel non acceptable
  - pas de besoin de révisions simultanées identifié à ce jour
  - artefact publié, pas d'image de conteneur maintenue

Écarté : conteneurs gérés — le besoin de révisions n'existe pas, et maintenir une
image ajouterait une charge sans contrepartie mesurable.
Écarté : machine virtuelle — aucune contrainte système ne l'exige.

À revoir si : un déploiement progressif devient nécessaire, ou si le service
doit cohabiter avec un composant qui impose une image.
```

## Contre-exemple et erreur fréquente

```text
Décision : plateforme d'orchestration de conteneurs à l'échelle, trois nœuds.

Justification : « c'est ce qui se fait », « ça scale », « on aura besoin plus tard ».

Service concerné : une API interne, trente utilisateurs, un pic à quatre requêtes
par seconde, une seule instance suffisante.
```

Quatre défauts.

Le choix ne répond à aucune contrainte du service. Trente utilisateurs et quatre requêtes par seconde
ne demandent ni orchestration, ni mise à l'échelle horizontale automatique.

« On aura besoin plus tard » est une hypothèse non datée et non mesurée. Elle fait payer aujourd'hui,
en complexité et en exploitation, une capacité dont rien n'établit qu'elle servira.

La charge d'exploitation est absente du raisonnement : mises à jour de la plateforme, sécurisation,
supervision, montée de version. Cette charge est récurrente, contrairement au coût de migration qu'on
aurait payé une fois si le besoin apparaissait réellement.

Enfin, aucune alternative n'est documentée. Dans un an, personne ne saura si la contrainte qui a mené
là existait, et le choix ne sera jamais remis en question.

La bonne démarche : choisir l'option la plus simple qui satisfait les contraintes d'aujourd'hui,
documenter les alternatives et la condition qui justifierait d'en changer.

## Vérification de compréhension

Un service traite des messages par lots deux fois par jour, en dix minutes, et n'a aucune exigence de
latence. Dites quel mode d'hébergement vous retenez, ce que vous écartez, et sur quel critère.

:::quiz
id=azure-hosting-choice-001-check
question=Pourquoi un hébergement de conteneurs géré n'est-il justifié que si le service est déjà conteneurisé **et** a besoin de révisions simultanées ?
option=Parce que la facturation n'est avantageuse que lorsque les deux conditions sont réunies
option=Parce qu'une seule des deux conditions fait payer une complexité sans contrepartie : conteneuriser sans usage des révisions, ou maintenir des révisions dont on n'a pas besoin
option=Parce que la plateforme refuse techniquement de déployer un artefact non conteneurisé
correct=1
success=Correct : le critère est la contrainte réelle, pas la disponibilité d'une fonctionnalité.
retry=Relisez le passage sur les deux conditions, et demandez-vous ce qu'apporte chacune prise isolément.
:::

## Exercice guidé

Ouvrez `azure-hosting-decision-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que doit produire chacune des quatre combinaisons possibles.
2. Implémentez la règle comme une conjonction stricte.
3. Vérifiez les deux cas mixtes, où une seule condition est vraie.
4. Ouvrez ensuite `content/labs/azure-operations/` et relevez la façon dont les choix y sont tracés.

## Exercice autonome

Choisissez l'hébergement de trois services : une interface web publique, une tâche de synchronisation
nocturne, et une interface de programmation interne à faible trafic.

Pour chacun, écrivez les contraintes retenues, l'option choisie, les options écartées avec leur
critère d'exclusion, l'estimation du coût mensuel, la charge d'exploitation attendue, et la condition
qui justifierait de revoir la décision.

## Débogage

Un ticket indique : « Notre facture d'hébergement a triplé sans que le trafic augmente. »

1. **Symptôme** : le coût croît indépendamment de l'usage.
2. **Hypothèse** : le mode d'hébergement facture une capacité réservée plutôt que l'usage, ou des
   ressources créées pour un essai n'ont jamais été supprimées.
3. **Preuve** : rapprochez la facturation par ressource de l'usage mesuré de chacune.
4. **Prévention** : choisir le mode de facturation adapté au profil d'usage, et poser un garde-fou de
   coût — le sujet de `observability-alerts-costs-001`.

## Entretien

Question posée à voix haute : *comment choisissez-vous où héberger un service ?*

Une réponse solide part des contraintes du service et non du catalogue, cite l'option la plus simple
qui les satisfait, intègre la charge d'exploitation dans le coût, et sait dire pourquoi une capacité
« pour plus tard » se paie aujourd'hui.

## Résumé

- Le choix part des contraintes du service, jamais du catalogue.
- L'option la plus simple qui satisfait le besoin est le bon défaut.
- Les conteneurs gérés exigent conteneurisation **et** besoin de révisions.
- Le coût total inclut l'exploitation et le coût des incidents à traiter soi-même.
- Une décision se documente avec ses alternatives et sa condition de révision.

## Cartes de révision

Question : quand une machine virtuelle se justifie-t-elle ? Réponse attendue : sur une contrainte
précise — composant système, licence, exigence réseau — jamais par confort.

Question : quel est le coût caché de « on en aura besoin plus tard » ? Réponse attendue : une
complexité et une exploitation payées tous les mois pour une capacité non établie.

## Test de maîtrise

Sans relire, rédigez la décision d'hébergement complète d'un service de facturation : contraintes
relevées, options envisagées, critère d'exclusion de chacune, option retenue, estimation de coût,
charge d'exploitation, effet sur la stratégie de déploiement, et la condition datée qui déclencherait
une révision de ce choix.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
