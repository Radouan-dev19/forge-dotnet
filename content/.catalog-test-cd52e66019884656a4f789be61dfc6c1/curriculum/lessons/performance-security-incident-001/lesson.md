# Incident : mesurer, contenir, comprendre

## Objectif observable

À la fin de cette leçon, vous saurez conduire un incident en séparant l'endiguement de l'analyse,
mesurer avant d'optimiser, et rédiger un compte rendu qui produit un changement plutôt qu'un
responsable.

## Prérequis

- Avoir lu `observability-alerts-costs-001` et savoir ce qu'une alerte déclenche.
- Avoir lu `ci-deployment-gates-001` et savoir revenir en arrière.

## Intuition

Un incident se conduit en deux temps, dans cet ordre : **rétablir le service**, puis **comprendre**.
Les inverser est l'erreur la plus coûteuse — chercher la cause pendant que les utilisateurs sont
bloqués rallonge la panne sans améliorer l'analyse.

La même discipline vaut pour la performance : on mesure d'abord, on optimise ensuite. Une optimisation
décidée sur une intuition améliore en général ce qui n'était pas le problème.

## Explication

**Endiguer d'abord.** Revenir à la version précédente, désactiver un indicateur, limiter le débit,
couper une fonctionnalité non essentielle. Ces gestes ne réparent rien : ils rendent le service. Ils
doivent être connus et répétés à l'avance, sans quoi ils sont improvisés au pire moment.

Le corollaire est important : conserver ce qui permettra l'analyse **avant** de rétablir. Une capture
des journaux, un instantané des métriques, l'identifiant de corrélation de quelques requêtes fautives.
Un retour arrière effacé sans traces laisse un incident non expliqué, qui reviendra.

**Une seule personne coordonne.** Elle ne débogue pas : elle décide, tient la chronologie et
communique. Sans ce rôle, trois personnes appliquent trois correctifs contradictoires, et personne ne
sait ce qui a produit le rétablissement.

**Communiquer tôt, factuellement.** Ce qui est impacté, depuis quand, ce qui est fait, et quand la
prochaine information sera donnée. Ne pas promettre de délai de résolution avant de connaître la
cause : une échéance manquée coûte plus que le silence.

**La chronologie s'écrit pendant, pas après.** Heure, observation, action, effet. Reconstituée le
lendemain, elle est fausse — la mémoire réordonne et oublie. Écrite pendant, elle est le seul document
qui permette de savoir ce qui a réellement rétabli le service.

**Mesurer avant d'optimiser.** L'intuition sur l'origine d'une lenteur est fausse la plupart du temps.
La démarche : mesurer l'état actuel, identifier l'étape qui consomme le temps — les traces de
`observability-correlation-001` le montrent —, formuler une hypothèse, appliquer **un** changement,
mesurer à nouveau.

Appliquer trois optimisations d'un coup et constater une amélioration n'apprend pas laquelle a agi, et
laisse deux changements dont on ignore le coût.

**Les causes fréquentes de lenteur sont connues.** Un accès en boucle à une dépendance là où un appel
groupé suffisait — le défaut vu dans `ef-core-data-access-001`. Une absence d'index sur une colonne
filtrée. Une pagination absente qui rapatrie tout. Un appel bloquant dans un chemin asynchrone, comme
dans `api-async-cancellation-001`. Un cache mal placé ou sans expiration. Les chercher dans cet ordre
est plus efficace que d'ouvrir un profileur.

**Un incident de sécurité change les priorités.** L'endiguement passe avant tout : révoquer les
accès, changer les secrets exposés, isoler ce qui doit l'être. Et il ne faut **pas** effacer les
traces : elles sont nécessaires à la compréhension de l'étendue, et parfois à des obligations de
notification. Un secret exposé est compromis, comme le rappelle
`ci-artifacts-variables-secrets-001` — la seule réponse est de le changer.

**Le compte rendu vise le système, pas les personnes.** Il décrit l'impact, la chronologie, la cause
technique, ce qui a permis au défaut d'atteindre la production, et les actions correctives avec un
responsable et une échéance. « Untel a oublié » n'est jamais une cause : la question est pourquoi le
système a permis cet oubli, et quel contrôle l'aurait attrapé.

Un compte rendu sans action datée n'est qu'un récit.

## Exemple commenté

La décision d'ordre, au moment où elle se pose :

```csharp
public static string NextIncidentStep(bool evidenceCaptured, bool serviceRestored)
{
    // Les preuves d'abord : un retour arrière efface l'état fautif, et sans capture
    // préalable l'incident restera inexpliqué — donc il reviendra.
    if (!evidenceCaptured)
    {
        return "capture-evidence";
    }

    // Puis rendre le service. L'analyse n'a lieu qu'ensuite, sur les preuves prises
    // à l'étape précédente : chercher la cause pendant la panne l'allonge sans rien
    // améliorer de l'analyse.
    return serviceRestored ? "analyse" : "contain";
}
```

Une chronologie écrite pendant l'incident :

```text
09:12  Alerte : taux d'erreur /orders à 12 % sur 5 min.        (observation)
09:13  Coordination prise par A. Communication interne émise.  (action)
09:15  Journaux capturés, identifiants de corrélation relevés  (preuve conservée AVANT
       sur 5 requêtes fautives, instantané des métriques.       tout rétablissement)
09:18  Retour à la version 1.4.7.                              (endiguement)
09:24  Taux d'erreur revenu à 0,1 %. Service rendu.            (effet mesuré)
09:30  Communication : service rétabli, analyse en cours.
10:40  Cause identifiée sur les preuves de 09:15 : une requête
       sans index déclenchait un délai sous charge.
```

Six minutes entre le retour arrière et le service rendu, parce que le geste était répété. Les preuves
de neuf heures quinze sont ce qui a permis l'analyse une heure plus tard.

Et le compte rendu, dans sa forme utile :

```text
Impact      /orders indisponible par intermittence, 09:12 à 09:24, ~ 340 requêtes en échec.

Preuves     identifiants de corrélation b7c1e2f4a9, 3d81c04e7f ; instantané métriques 09:15 ;
            plan d'exécution de la requête fautive.

Cause       la migration 1.4.8 ajoute un filtre sur une colonne non indexée. Sous charge,
            le balayage dépasse le budget de temps de l'appel.

Pourquoi ce défaut a atteint la production
            les tests d'intégration s'exécutent sur un jeu de 200 lignes ; le balayage
            y est instantané. Aucun contrôle ne compare le plan d'exécution.

Actions     1. Ajouter l'index — A. — livré le 12/08
            2. Jeu de données de volume réaliste dans la suite d'intégration — B. — 26/08
            3. Alerte sur latence p95 par point d'entrée — C. — 19/08
```

## Contre-exemple et erreur fréquente

```text
09:12  Alerte reçue.
09:13  Trois personnes commencent à chercher la cause. Aucune coordination.
09:20  Redémarrage d'une instance par l'une. Modification de configuration par une autre.
09:35  Le service semble revenu. Personne ne sait lequel des deux gestes a agi.
09:36  Journaux purgés « pour repartir propre ».
09:40  Aucune communication n'a été émise. Le support découvre par un client.
Lendemain : compte rendu rédigé de mémoire.
```

Et le compte rendu produit :

```text
Cause : erreur humaine, un développeur a poussé une migration non testée.
Action : être plus vigilant lors des revues.
```

Six défauts.

L'analyse a commencé avant l'endiguement : les utilisateurs sont restés bloqués vingt-trois minutes de
plus que nécessaire.

Aucune coordination : deux gestes simultanés, et le rétablissement n'est attribuable à aucun des deux.
Le même incident se reproduira sans qu'on sache quoi faire.

La purge des journaux détruit les preuves. L'analyse devient impossible, et sur un incident de
sécurité elle serait fautive.

L'absence de communication fait découvrir la panne au support par un client — la pire façon.

La chronologie reconstituée de mémoire est fausse dans le détail, c'est-à-dire là où elle servirait.

Enfin, « erreur humaine » et « être plus vigilant » ne sont ni une cause ni une action. La vraie
question est pourquoi une migration non testée pouvait atteindre la production, et le seul correctif
utile est un contrôle qui l'aurait refusée — une porte, au sens de `ci-deployment-gates-001`.

## Vérification de compréhension

Un service répond en huit secondes au lieu de deux cents millisecondes. Décrivez vos trois premiers
gestes, dans l'ordre, et dites ce que vous vous interdisez tant que la mesure n'est pas faite.

:::quiz
id=performance-security-incident-001-check
question=Pourquoi conserver des preuves avant de rétablir le service ?
option=Parce que le rétablissement est plus rapide lorsque les journaux ont été exportés
option=Parce qu'un retour arrière efface l'état fautif : sans capture préalable, l'incident reste inexpliqué et se reproduira
option=Parce que les preuves doivent être jointes à la communication envoyée aux utilisateurs
correct=1
success=Correct : quelques minutes de capture — journaux, métriques, identifiants de corrélation — rendent possible une analyse une heure plus tard.
retry=Relisez la chronologie de l'exemple, et regardez ce qui se passe à neuf heures quinze avant le retour arrière.
:::

## Exercice guidé

Cette leçon se pratique sur `content/labs/azure-operations/` et sur les scénarios de `/debug`, plutôt
que sur un exercice `/practice` dédié.

1. Choisissez un scénario de `/debug` présentant une dégradation sans erreur, et écrivez votre
   chronologie **pendant** que vous le traitez : heure, observation, action, effet.
2. Notez, avant tout changement, la mesure de départ. Interdisez-vous d'optimiser tant qu'elle n'est
   pas prise.
3. N'appliquez qu'un seul changement, puis mesurez à nouveau. Consignez l'écart.
4. Ouvrez `content/labs/azure-operations/` et comparez votre chronologie à la trame de compte rendu
   qui s'y trouve : impact, preuves, cause, pourquoi le défaut a atteint la production, actions
   datées.

## Exercice autonome

Rédigez le manuel d'incident d'un service que vous connaissez.

Décidez avant d'écrire : les gestes d'endiguement disponibles et leur délai, ce qui est capturé avant
rétablissement, le rôle de coordination et ce qu'il fait, le rythme et le contenu des communications,
la trame de chronologie, la trame de compte rendu, et ce qui change lorsque l'incident est de nature
sécuritaire.

## Débogage

Un ticket indique : « Le service est lent depuis la dernière livraison, sans erreur. »

1. **Symptôme** : dégradation de latence sans échec, corrélée à une livraison.
2. **Hypothèse** : un accès en boucle, un index manquant, ou un appel bloquant introduit par le
   changement.
3. **Preuve** : mesurez d'abord — traces de la période, étape qui consomme le temps, requêtes émises
   par appel. Ne modifiez rien avant.
4. **Prévention** : un seul changement à la fois, mesuré avant et après, et une alerte de latence par
   point d'entrée.

## Entretien

Question posée à voix haute : *racontez-moi un incident que vous avez traité.*

Une réponse solide sépare l'endiguement de l'analyse, mentionne les preuves conservées avant
rétablissement, donne une chronologie, nomme une cause technique plutôt qu'une personne, et cite les
actions correctives datées qui en ont découlé.

## Résumé

- Rétablir d'abord, comprendre ensuite — mais capturer les preuves avant de rétablir.
- Une personne coordonne et ne débogue pas.
- La chronologie s'écrit pendant ; reconstituée, elle est fausse là où elle sert.
- Mesurer, formuler une hypothèse, changer une seule chose, mesurer à nouveau.
- « Erreur humaine » n'est pas une cause ; l'absence de contrôle en est une.

## Cartes de révision

Question : que faire des traces lors d'un incident de sécurité ? Réponse attendue : les conserver —
elles servent à établir l'étendue et parfois à des obligations de notification.

Question : que vaut un compte rendu sans action datée et attribuée ? Réponse attendue : un récit, qui
ne produit aucun changement.

## Test de maîtrise

Sans relire, rédigez le manuel d'incident complet d'un service : gestes d'endiguement et leurs délais,
preuves capturées avant rétablissement, rôle de coordination, rythme et contenu des communications,
trame de chronologie, démarche de mesure pour un problème de performance, trame de compte rendu, et
les spécificités d'un incident de sécurité.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
