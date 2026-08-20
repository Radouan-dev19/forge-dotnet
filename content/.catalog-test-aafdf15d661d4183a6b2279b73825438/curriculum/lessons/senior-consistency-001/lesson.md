# Cohérence éventuelle et compensation

## Objectif observable

À la fin de cette leçon, vous saurez expliquer ce qu'une transaction locale cesse de garantir dès
qu'une opération traverse plusieurs services, définir la cohérence éventuelle, et décrire comment
une saga rattrape un échec partiel par des actions de compensation exécutées en ordre inverse.

## Prérequis

- Avoir lu `senior-messaging-001` : le consommateur idempotent et la publication d'événements.
- Savoir ce qu'une transaction de base de données garantit : tout ou rien, sur une seule base.

## Intuition

Sur une seule base, une transaction est un filet magique : plusieurs écritures réussissent ensemble
ou sont toutes annulées, et personne ne voit jamais un état intermédiaire. Dès que l'opération
touche deux services aux bases distinctes, ce filet disparaît. Il n'existe pas de « tout ou rien »
qui recouvre deux bases séparées par un réseau, sans se payer d'un verrou distribué qui grippe tout
le système. On ne peut donc plus *empêcher* l'état intermédiaire ; on peut seulement le *rattraper*.
Ce renversement — de la prévention vers la réparation — est le cœur de la cohérence dans un système
réparti. On n'annule pas, on compense.

## Explication

**Ce qu'une transaction locale cessait de couvrir.** Sur une base unique, débiter un compte et
créditer un autre tient dans une transaction : si le crédit échoue, le débit est annulé
automatiquement, et aucun observateur ne voit de l'argent disparu entre les deux. Cette garantie
repose sur le fait qu'un seul gestionnaire de transactions contrôle toutes les écritures. Répartissez
les deux comptes sur deux services aux bases distinctes, et ce gestionnaire unique n'existe plus.
Chaque service ne peut valider ou annuler que *ses propres* écritures. Le débit peut être validé et
confirmé chez le premier service alors que le crédit échoue chez le second : l'état intermédiaire
« argent débité mais non crédité » devient possible, et rien dans l'infrastructure ne le corrige
tout seul. C'est ce que l'on perd exactement en franchissant une frontière de service : l'annulation
automatique et coordonnée de plusieurs écritures.

**La cohérence éventuelle nomme ce que l'on garde.** On n'obtient plus la cohérence *immédiate* — à
aucun instant on ne voit d'état incohérent. On vise la cohérence *éventuelle* : après un échec ou un
délai, le système converge vers un état cohérent, mais il traverse des états transitoires
observables entre-temps. Pendant une fenêtre, la commande peut être « payée » côté paiement et
encore « en attente » côté expédition ; le système se rejoindra, mais pas au même instant. Accepter
la cohérence éventuelle, c'est accepter que « pour l'instant, ces deux services ne sont pas
d'accord, et c'est normal ». Ce n'est pas un défaut à masquer : c'est une propriété à concevoir, à
rendre visible dans le modèle métier — un statut « en cours de traitement » plutôt qu'un mensonge
« terminé ».

**La saga remplace la transaction par une suite d'étapes réparables.** Une saga est une opération
métier découpée en une séquence d'étapes locales, chacune dans son propre service, chacune validée
individuellement. Tant que les étapes réussissent, la saga avance. Quand une étape échoue, on ne
peut pas « annuler » les précédentes — elles sont déjà validées, définitivement, chez leurs
services. On exécute alors, pour chacune des étapes déjà réussies, une *action de compensation* : une
nouvelle opération dont l'effet métier neutralise celui de l'étape. On n'efface pas le débit ; on
émet un remboursement. On n'annule pas la réservation de stock ; on la libère. La compensation est
une action métier à part entière, avec sa propre trace, pas un retour en arrière technique.

**L'ordre inverse n'est pas un détail.** Les compensations s'exécutent dans l'ordre *inverse* des
étapes réussies. Si la saga a fait A, puis B, puis C, et que D échoue, on compense C, puis B, puis A.
La raison est la dépendance : une étape tardive s'appuie souvent sur les précédentes, et compenser A
avant C reviendrait à retirer un fondement sur lequel la compensation de C compte encore. Défaire une
pile se fait par le sommet, jamais par la base. Deux exigences accompagnent cet ordre. Les
compensations doivent être *idempotentes*, car un réessai peut relancer une compensation déjà
effectuée. Et il faut décider quoi faire d'une compensation qui *elle-même* échoue : réessai borné,
puis alerte humaine, car un système ne peut pas toujours se réparer seul, et prétendre le contraire
est dangereux.

**La saga n'est pas une transaction, et le vocabulaire ne doit pas le laisser croire.** Une saga ne
donne ni isolation ni annulation instantanée. Pendant son déroulé, d'autres opérations peuvent voir
les états intermédiaires. Choisir une saga, c'est accepter ce coût en échange de l'absence de verrou
distribué. Un ingénieur qui présente une saga comme « une transaction entre services » se trompe et
trompe son équipe : c'est un protocole de réparation, pas un filet magique.

## Exemple commenté

Le noyau décidable est l'ordre de compensation — noyau de l'exercice guidé :

```csharp
// Étapes réussies dans l'ordre d'exécution ; on renvoie l'ordre de compensation.
// Règle : compenser dans l'ordre INVERSE des étapes réussies.
public static IReadOnlyList<string> CompensationOrder(IReadOnlyList<string> completedSteps)
{
    var order = new List<string>(completedSteps);
    order.Reverse(); // le sommet de la pile d'abord : dernière étape réussie, compensée en premier
    return order;
}

// Exemple : étapes [ "Debit", "ReserveStock", "CreateShipment" ], puis échec de l'étape suivante.
// CompensationOrder renvoie [ "CreateShipment", "ReserveStock", "Debit" ].
```

Compenser d'abord la dernière étape réussie garantit qu'on ne retire jamais un fondement encore
utilisé par la compensation d'une étape plus tardive.

## Contre-exemple et erreur fréquente

Le code fautif compense dans l'ordre direct :

```csharp
// FAUTIF : compensations dans l'ordre d'exécution, pas dans l'ordre inverse.
foreach (var step in completedSteps) // "Debit", puis "ReserveStock", puis "CreateShipment"
{
    await Compensate(step); // on rembourse AVANT de libérer le stock et d'annuler l'envoi
}
```

Le symptôme est une incohérence subtile : on rembourse le client alors que l'expédition n'est pas
encore annulée, si bien qu'un colis peut partir pour une commande déjà remboursée. La correction
inverse l'ordre :

```csharp
// CORRIGÉ : ordre inverse, la dernière étape réussie compensée en premier.
foreach (var step in CompensationOrder(completedSteps)) // "CreateShipment", "ReserveStock", "Debit"
{
    await Compensate(step); // on annule l'envoi, puis libère le stock, puis rembourse
}
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : quand une saga a réussi trois étapes et que la quatrième
échoue, pourquoi ne peut-on pas simplement « annuler » les trois premières comme une transaction le
ferait ?

:::quiz
id=senior-consistency-001-check
question=Pourquoi les actions de compensation d'une saga s'exécutent-elles dans l'ordre inverse des étapes réussies ?
option=Parce que l'ordre inverse est plus rapide à parcourir en machine
option=Parce qu'une étape tardive s'appuie souvent sur les précédentes ; défaire une pile par le sommet évite de retirer un fondement encore utilisé
option=Parce que le courtier de messages n'accepte que l'ordre inverse
correct=1
success=Exact : on défait une pile par le sommet. Compenser la dernière étape réussie en premier évite de retirer un fondement dont dépend encore la compensation d'une étape plus tardive.
retry=Repensez à une pile d'étapes dépendantes : par quel bout la défait-on sans casser une dépendance ?
:::

## Exercice guidé

Ouvrez l'exercice `senior-compensation-001` dans `/practice`, puis procédez ainsi.

1. Représentez la liste des étapes réussies dans leur ordre d'exécution.
2. Produisez l'ordre de compensation en inversant cette liste.
3. Vérifiez que la dernière étape réussie figure en première position de l'ordre de compensation.
4. Prédisez l'ordre de compensation pour une saga de trois étapes dont la quatrième échoue.

## Exercice autonome

Pour une commande qui débite le compte, réserve le stock puis planifie une livraison, concevez la
saga complète : les trois étapes locales, leurs actions de compensation respectives, l'ordre dans
lequel elles s'exécutent si la planification échoue, et le statut visible côté client pendant la
fenêtre de cohérence éventuelle. Indiquez ce qui se passe si une compensation échoue à son tour.

## Débogage

Un ticket indique : « Des clients sont remboursés mais reçoivent quand même leur colis, seulement
lors des échecs de commande en fin de parcours. »

1. **Symptôme** : remboursement effectué et expédition non annulée pour une même commande en échec.
2. **Hypothèse** : les compensations s'exécutent dans l'ordre direct, si bien que le remboursement
   part avant l'annulation de l'expédition, laissant une fenêtre où le colis peut être expédié.
3. **Preuve** : tracer l'ordre réel des compensations d'une commande en échec et le comparer à
   l'ordre inverse attendu des étapes réussies.
4. **Prévention** : compenser strictement en ordre inverse, rendre chaque compensation idempotente,
   et alerter un humain quand une compensation échoue plutôt que d'arrêter à mi-chemin.

## Entretien

Question posée à voix haute : *votre opération de commande touche trois services ; comment
garantissez-vous la cohérence sans transaction distribuée ?*

Une réponse solide commence par ce que l'on perd — l'annulation automatique et coordonnée — puis
introduit la cohérence éventuelle comme propriété assumée et visible dans le modèle. Elle décrit la
saga comme une suite d'étapes locales avec compensations en ordre inverse, insiste sur l'idempotence
des compensations et le traitement d'une compensation qui échoue, et refuse de présenter la saga
comme une transaction.

### Le nom en entretien

La cohérence éventuelle se dit **eventual consistency**, la saga garde son nom en anglais — **saga
pattern**, orchestrée ou chorégraphiée — et ses gestes inverses se disent **compensating actions**.
Les garanties de session ont aussi leurs noms : **monotonic reads**, **read-your-writes**. Côté
outils, l'industrie .NET associe les sagas orchestrées à **MassTransit** et **NServiceBus** — deux
cadriciels de messagerie qui portent l'automate de saga pour vous — et le motif de publication
fiable se dit **transactional outbox**. Aucun de ces noms n'est une dépendance du parcours : c'est
le lexique dans lequel un entretien senior posera exactement les questions de cette semaine.

## Résumé

- Franchir une frontière de service fait perdre l'annulation automatique et coordonnée de plusieurs
  écritures.
- La cohérence éventuelle assume des états intermédiaires observables avant convergence.
- Une saga découpe l'opération en étapes locales validées individuellement.
- On ne peut pas annuler une étape validée : on la compense par une action métier inverse.
- Les compensations s'exécutent en ordre inverse et doivent être idempotentes.

## Cartes de révision

Question : que perd-on exactement quand une opération passe d'une base unique à deux services
distincts ? Réponse attendue : le gestionnaire de transactions unique qui annulait ensemble toutes
les écritures ; chaque service ne peut plus valider ou annuler que les siennes, rendant possible un
état intermédiaire que rien ne corrige seul.

Question : qu'est-ce qu'une action de compensation et en quoi diffère-t-elle d'un retour en arrière
technique ? Réponse attendue : c'est une nouvelle opération métier dont l'effet neutralise celui
d'une étape déjà validée — un remboursement plutôt qu'un débit effacé — avec sa propre trace, car
l'étape d'origine ne peut plus être annulée.

## Test de maîtrise

Sans relire, expliquez ce qu'une transaction locale garantissait et ce que sa disparition ouvre
entre services. Puis décrivez une saga de trois étapes, ses compensations, leur ordre d'exécution en
cas d'échec, et pourquoi cet ordre est inverse.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
