# Azure Service Bus : files, sujets et livraison fiable de messages

Un courtier de messages découple un producteur d'un consommateur : le producteur dépose un
message et repart sans attendre, le consommateur le traite à son rythme. Cela absorbe un pic de
charge sans perdre de travail, et permet à deux services de communiquer sans être disponibles au
même instant. Azure Service Bus est le courtier de messages managé d'Azure, pensé pour de la
messagerie d'entreprise fiable — au contraire d'une simple file de stockage, il gère nativement
sessions, transactions et sujets avec abonnements filtrés.

## Namespace, files et sujets

Un **namespace** Service Bus est la frontière de facturation et de connexion, comparable à un
abonnement Azure pour la messagerie. À l'intérieur, une **file** (queue) livre chaque message à
un seul consommateur : c'est le modèle point à point. Un **sujet** (topic) diffuse chaque message
à plusieurs **abonnements** (subscriptions), chacun pouvant filtrer les messages qui l'intéressent
— c'est le modèle publication-abonnement. Choisir entre les deux dépend d'une question simple :
un seul traitement doit-il consommer ce message, ou plusieurs traitements indépendants doivent-ils
chacun le voir ?

## Anatomie d'un message

Un message porte un corps (les données métier, le plus souvent du JSON), des propriétés
personnalisées, un identifiant, un identifiant de corrélation pour relier une réponse à sa
demande, une durée de vie après laquelle il expire, et éventuellement une date d'envoi différée.
Ces métadonnées permettent de router, corréler ou temporiser un message sans inspecter son corps.

## Deux modes de réception, deux niveaux de garantie

En mode **réception avec verrou différé** (peek-lock, le mode par défaut), un message reçu reste
verrouillé et invisible aux autres consommateurs, mais n'est retiré de la file qu'après un accusé
de réception explicite : le consommateur doit **compléter** le message une fois le traitement
réussi, **l'abandonner** pour le rendre immédiatement disponible à un autre essai, ou
l'envoyer en **lettre morte** s'il est irrécupérable. En mode **réception et suppression
immédiate**, le message est retiré dès sa lecture, avant même d'être traité : un crash du
consommateur entre la lecture et le traitement le perd définitivement. Le premier mode coûte un
aller-retour réseau de plus ; c'est le prix d'une livraison qui survit à un plantage.

## La file de lettres mortes : ce qui sauve un incident

Chaque file et chaque abonnement possède une **file de lettres mortes** (dead-letter queue), où
atterrit un message qui a dépassé son nombre maximal de tentatives de livraison, ou qu'un
consommateur y a explicitement envoyé après une erreur métier reconnue. Sans elle, un message
empoisonné — mal formé, ou qui provoque systématiquement une exception — bloquerait le traitement
en boucle ; avec elle, il est isolé sans perdre le flux normal, et reste disponible pour
inspection et rejeu manuel une fois la cause corrigée.

## Exemple commenté

Envoyer un message, puis le traiter en confirmant sa complétion :

```csharp
await using var client = new ServiceBusClient(connectionString);
ServiceBusSender sender = client.CreateSender("commandes");

var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(nouvelleCommande))
{
    MessageId = nouvelleCommande.Id.ToString(),
    ContentType = "application/json",
};
await sender.SendMessageAsync(message);

ServiceBusProcessor processor = client.CreateProcessor("commandes");
processor.ProcessMessageAsync += async args =>
{
    try
    {
        await TraiterCommandeAsync(args.Message.Body);
        await args.CompleteMessageAsync(args.Message);
    }
    catch (CommandeInvalideException)
    {
        // Erreur métier reconnue : pas la peine de réessayer, direction lettres mortes.
        await args.DeadLetterMessageAsync(args.Message, "commande-invalide");
    }
};
await processor.StartProcessingAsync();
```

## Pièges fréquents

Oublier d'appeler la complétion du message après un traitement réussi le laisse verrouillé
jusqu'à expiration du verrou, puis il redevient disponible et sera **livré une seconde fois** :
un traitement doit donc être conçu pour tolérer une livraison en double, ou s'appuyer sur la
détection de doublons du namespace. Un traitement plus long que la durée du verrou fait expirer
celui-ci pendant l'exécution — le message repart en file alors qu'il est encore en cours de
traitement ailleurs ; la parade est de renouveler le verrou périodiquement pour un traitement
long, ou de garder les traitements courts. Enfin, supposer un ordre de livraison strict sans avoir
activé les **sessions** est une erreur : sans session, Service Bus ne garantit aucun ordre entre
messages d'une même file sous forte concurrence.

## Ce qu'il faut retenir

Une file livre à un seul consommateur, un sujet diffuse à plusieurs abonnements filtrés. Le mode
peek-lock protège contre la perte de message au prix d'une complétion explicite, que le code doit
toujours honorer — sans quoi le message revient. La file de lettres mortes isole un message
défaillant sans bloquer le flux normal, et l'ordre strict n'existe qu'avec des sessions activées.
