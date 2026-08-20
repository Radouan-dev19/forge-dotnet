# Alertes actionnables et coûts bornés

## Objectif observable

À la fin de cette leçon, vous saurez écrire une alerte qui déclenche une action précise, distinguer un
symptôme visible par l'utilisateur d'une cause interne, et poser un garde-fou de coût qui refuse une
dépense avant qu'elle ne soit engagée.

## Prérequis

- Avoir lu `observability-correlation-001` et savoir quels signaux existent.
- Avoir lu `azure-hosting-choice-001` et savoir d'où vient un coût récurrent.

## Intuition

Une alerte a un seul critère de qualité : *quelqu'un fait-il quelque chose de précis quand elle se
déclenche ?* Si la réponse est non, elle ne devrait pas exister — et elle est activement nuisible,
parce qu'elle apprend à ignorer les notifications.

Le coût obéit à la même logique. Il ne se surveille pas après coup : il se borne avant, par une
décision qui refuse d'engager une dépense hors budget.

## Explication

**Alerter sur les symptômes, pas sur les causes.** Un taux d'erreur qui monte, une latence qui sort de
son budget, une file qui s'allonge sans se vider : ce sont des symptômes que l'utilisateur ressent.
Une utilisation processeur à quatre-vingts pour cent n'en est pas un — elle peut être parfaitement
normale, ou signaler un problème, et rien dans le chiffre ne le dit.

L'alerte sur symptôme réveille quand quelque chose ne va **pas** pour quelqu'un. L'alerte sur cause
réveille souvent pour rien, et manque les incidents qui n'ont pas cette cause.

**Chaque alerte a un seuil, une durée et une action.** Le seuil vient d'un budget annoncé, pas d'une
impression. La durée évite de réveiller pour un pic de dix secondes. Et l'action décrit ce que fait la
personne alertée — sans quoi elle recevra une notification qu'elle ne peut que constater.

Une alerte sans action associée est un bruit qui fera ignorer les suivantes.

**Distinguer ce qui réveille de ce qui attend.** Toutes les alertes ne justifient pas une notification
immédiate. Un service indisponible réveille ; un certificat qui expire dans trente jours crée une
tâche. Confondre les deux fatigue l'équipe et rend le réveil moins efficace quand il compte.

**La fatigue d'alerte est mesurable.** Le nombre de déclenchements par semaine et la proportion de
ceux qui ont donné lieu à une action réelle sont deux indicateurs à suivre. Une alerte dont personne
n'a rien fait depuis trois mois se corrige ou se supprime — la laisser en place n'est pas neutre.

**Le coût se borne avant, pas après.** Trois mécanismes se complètent. Un budget déclaré avec des
alertes à des pourcentages successifs. Des limites de mise à l'échelle : un nombre maximal
d'instances, une taille maximale de niveau de service. Et une décision explicite avant toute création
de ressource : le coût estimé tient-il dans le budget, et existe-t-il un plan de suppression ?

**Le plan de suppression est la condition oubliée.** Une ressource créée pour un essai, sans date ni
responsable de suppression, devient permanente. C'est la première cause de dérive des coûts, avant
toute question de dimensionnement. C'est pourquoi l'exercice de cette leçon exige un plan de
suppression **en plus** du respect du budget : un coût acceptable sans plan de suppression reste
refusé.

**Étiqueter pour pouvoir attribuer.** Chaque ressource porte son service, son environnement et son
responsable. Sans ces étiquettes, une facture qui augmente ne se rattache à rien, et le seul recours
est une enquête manuelle. C'est le même besoin d'identification que pour un artefact dans
`ci-artifacts-variables-secrets-001`.

**La rétention est un coût.** Conserver tous les journaux pendant deux ans coûte souvent plus que le
service qui les produit. La durée se décide par usage : quelques semaines pour le diagnostic
courant, plus longtemps pour ce qui est légalement requis, et un échantillonnage pour les traces à fort
volume.

## Exemple commenté

Le garde-fou de coût, qui exige les deux conditions :

```csharp
public static string CostGuardrail(
    decimal estimatedDailyCost,
    decimal dailyBudget,
    bool deletionPlanReady)
{
    // Un coût négatif ou un budget nul ne décrivent rien d'exploitable.
    if (estimatedDailyCost < 0 || dailyBudget <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(estimatedDailyCost));
    }

    // Le plan de suppression d'abord : une ressource temporaire sans date
    // de fin devient permanente, et c'est la première cause de dérive.
    if (!deletionPlanReady)
    {
        return "block";
    }

    return estimatedDailyCost <= dailyBudget ? "allow" : "block";
}
```

Une alerte complète, avec ses trois composants :

```text
Alerte : latence utilisateur dégradée

Signal    latence au 95e centile sur /orders
Seuil     750 ms — budget annoncé dans le contrat de service, pas une impression
Durée     5 minutes consécutives — un pic isolé ne réveille personne
Gravité   réveille (le service est rendu, mais hors de son engagement)

Action    1. Vérifier le taux d'erreur : s'il est non nul, traiter les erreurs d'abord
          2. Consulter les traces de la période pour localiser l'étape lente
          3. Si la lenteur vient d'une dépendance, appliquer le repli documenté
          4. Sinon, revenir à la version précédente et analyser ensuite

Sans les quatre lignes d'action, cette alerte n'est qu'une notification.
```

Et la comparaison entre alerte sur symptôme et alerte sur cause :

```text
Sur symptôme (utile)                      Sur cause (à éviter seule)
----------------------------------------  ----------------------------------------
taux d'erreur > 1 % pendant 5 min         processeur > 80 % pendant 5 min
latence p95 > budget pendant 5 min        mémoire > 70 %
file non vidée depuis 10 min              nombre de connexions > 100
service indisponible                      redémarrage de processus
```

Les signaux de droite sont utiles pour **diagnostiquer**, une fois qu'on sait qu'il y a un problème.
Ils sont mauvais pour **détecter**.

## Contre-exemple et erreur fréquente

```text
Alertes configurées : 47

  processeur > 60 %                       déclenchée 340 fois ce mois, 0 action
  mémoire > 50 %                          déclenchée 210 fois ce mois, 0 action
  toute exception journalisée             déclenchée 1 800 fois ce mois, 2 actions
  redémarrage d'instance                  déclenchée 60 fois ce mois, 0 action
  disque > 40 %                           déclenchée en continu depuis 4 mois

Destinataire : liste de diffusion « équipe », 12 personnes, notification immédiate.
Règle de filtrage dans la messagerie de 9 personnes sur 12 : dossier « alertes », non lu.

Budget : aucun. Ressources créées pour la démonstration de mars : toujours en place.
Étiquettes : aucune. La facture est un total mensuel non ventilé.
```

Cinq défauts.

Les seuils portent sur des causes et sont réglés trop bas. Un processeur à soixante pour cent est
souvent le signe d'une machine correctement dimensionnée, pas d'un incident.

Le rapport entre déclenchements et actions dit tout : deux mille six cents notifications pour deux
actions. Le système ne détecte rien, il conditionne l'équipe à ignorer les notifications.

Neuf personnes sur douze ont créé une règle de filtrage. L'alerte est techniquement active et
pratiquement morte : c'est le pire des états, parce que le tableau de bord affiche une couverture qui
n'existe pas.

Aucun budget ni garde-fou n'existe. Les ressources d'une démonstration vieille de plusieurs mois sont
encore facturées, et personne ne le sait.

L'absence d'étiquettes rend impossible d'attribuer la facture. Une hausse ne se rattache à aucun
service, et la seule réponse possible est une enquête manuelle.

## Vérification de compréhension

Vous héritez de quarante-sept alertes. Décrivez la démarche pour arriver à une dizaine d'alertes
utiles, et le critère qui décide du sort de chacune.

:::quiz
id=observability-alerts-costs-001-check
question=Pourquoi un coût estimé inférieur au budget ne suffit-il pas à autoriser la création d'une ressource ?
option=Parce que l'estimation est toujours inférieure au coût réel constaté
option=Parce que sans plan de suppression, une ressource créée temporairement devient permanente : c'est la première cause de dérive des coûts
option=Parce que le budget doit être validé par une autorité extérieure à l'équipe
correct=1
success=Correct : le garde-fou exige les deux conditions, et le plan de suppression est celle qu'on oublie.
retry=Relisez le passage sur le plan de suppression, et demandez-vous ce que devient une ressource d'essai sans date de fin.
:::

## Exercice guidé

Ouvrez `azure-cost-guardrail-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que doit produire chaque combinaison, y compris un coût nul.
2. Implémentez la règle en refusant d'abord les entrées incohérentes, puis en évaluant le plan de
   suppression avant le budget.
3. Vérifiez la frontière exacte : coût égal au budget, puis coût supérieur d'un centime.
4. Ouvrez ensuite `content/labs/azure-operations/` et relevez les garde-fous qui y sont posés.

## Exercice autonome

Concevez le dispositif d'alerte et de maîtrise des coûts d'un service en production.

Décidez avant d'écrire : la liste des alertes avec signal, seuil, durée, gravité et action de chacune,
celles qui réveillent et celles qui créent une tâche, le budget mensuel et ses paliers d'alerte, les
limites de mise à l'échelle, les étiquettes obligatoires, la durée de conservation par famille de
signal, et l'indicateur qui vous dira si vos alertes sont utiles.

## Débogage

Un ticket indique : « Une panne de deux heures n'a déclenché aucune alerte. »

1. **Symptôme** : un incident réel est passé sous les radars.
2. **Hypothèse** : les alertes portent sur des causes qui n'étaient pas celles-ci, ou le seuil et la
   durée les rendaient insensibles à ce profil de panne.
3. **Preuve** : rejouez les mesures de la période contre les règles d'alerte existantes.
4. **Prévention** : ajouter une alerte sur le symptôme visible par l'utilisateur, avec un seuil issu du
   budget annoncé.

## Entretien

Question posée à voix haute : *comment savez-vous que votre service va mal avant que les utilisateurs
ne le signalent ?*

Une réponse solide alerte sur des symptômes visibles par l'utilisateur, associe une action à chaque
alerte, distingue ce qui réveille de ce qui attend, mesure la fatigue d'alerte, et traite le coût comme
une contrainte bornée en amont.

## Résumé

- Une alerte sans action précise est du bruit qui fera ignorer les suivantes.
- Alerter sur les symptômes ; les causes servent à diagnostiquer, pas à détecter.
- Seuil issu d'un budget, durée qui filtre les pics, gravité qui distingue réveil et tâche.
- Le garde-fou de coût exige budget respecté **et** plan de suppression.
- Sans étiquettes, une facture qui augmente ne se rattache à rien.

## Cartes de révision

Question : quel indicateur mesure la qualité d'un jeu d'alertes ? Réponse attendue : la proportion de
déclenchements ayant donné lieu à une action réelle.

Question : pourquoi la rétention est-elle une décision de coût ? Réponse attendue : conserver tous les
signaux longtemps coûte souvent plus que le service qui les produit.

## Test de maîtrise

Sans relire, décrivez le dispositif complet d'alerte et de maîtrise des coûts d'un service : cinq
alertes avec signal, seuil, durée, gravité et action, la séparation entre réveil et tâche, le budget et
ses paliers, les limites de mise à l'échelle, la règle de création de ressource, les étiquettes
obligatoires, la rétention par famille, et l'indicateur de qualité de vos alertes.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
