# Messagerie : consommateur idempotent, outbox, lettres mortes

## Objectif observable

À la fin de cette leçon, vous saurez décrire ce que fait un consommateur idempotent face à un
message dupliqué, expliquer pourquoi le motif outbox résout l'écriture-puis-publication qui ne peut
pas être atomique autrement, et dire à quoi sert une file de lettres mortes face à un message
empoisonné.

## Prérequis

- Avoir lu `senior-idempotency-001` : la clé d'idempotence et la livraison au moins une fois.
- Savoir qu'un courtier de messages redélivre un message tant que le consommateur ne l'a pas
  acquitté.

## Intuition

Une file de messages livre au moins une fois : c'est sa force — rien n'est perdu — et sa contrainte
— tout peut arriver deux fois. Le consommateur hérite donc de trois obligations que la file ne
remplira jamais à sa place. Il doit reconnaître un doublon et ne pas le retraiter. Il doit s'assurer
que ce qu'il change en base et ce qu'il publie comme message ne divergent jamais, même si le
processus meurt entre les deux. Et il doit avoir un plan pour un message qu'il ne parviendra
*jamais* à traiter, sinon ce message bloque la file derrière lui. Ces trois obligations donnent les
trois motifs de cette leçon : consommateur idempotent, outbox, lettres mortes.

## Explication

**Le consommateur idempotent reconnaît ce qu'il a déjà fait.** Puisque la file redélivre, le
consommateur reçoit parfois un message qu'il a déjà traité — après un acquittement perdu, une
redélivrance après plantage. Il ne peut pas empêcher le doublon d'arriver ; il peut le rendre
inoffensif. Pour cela, chaque message porte un identifiant stable, et le consommateur tient une
trace des identifiants déjà traités. À la réception, il regarde d'abord cette trace. Message inconnu :
il traite, enregistre l'identifiant, acquitte. Message déjà vu : il n'exécute pas l'effet métier une
seconde fois, mais il acquitte quand même — car l'absence d'acquittement provoquerait une nouvelle
redélivrance, donc une boucle. Acquitter un doublon sans le retraiter est le comportement correct,
pas un raccourci.

**Le motif outbox résout une atomicité impossible.** Un consommateur doit souvent faire deux choses
en réaction à un message : modifier la base *et* publier un nouveau message pour la suite. Ces deux
actions visent deux systèmes différents — la base et le courtier — et aucune transaction ne les
couvre ensemble. Deux ordres, deux pièges. Si l'on publie d'abord puis que la base échoue, on a
annoncé un fait qui n'a pas eu lieu. Si l'on écrit d'abord puis que la publication échoue, on a un
fait réel que personne n'apprendra. Le motif outbox supprime le choix. Au lieu de publier
directement, le consommateur écrit le message à publier dans une *table outbox de la même base*,
dans la *même transaction* que la modification métier. Comme les deux écritures sont dans une seule
transaction locale, elles réussissent ou échouent ensemble : l'atomicité est retrouvée là où elle
existe vraiment. Un processus séparé — le relayeur — lit ensuite la table outbox et publie chaque
message vers le courtier, en marquant ceux qui sont partis. Ce relayeur publie au moins une fois —
il peut redoubler si son propre acquittement se perd — ce qui referme la boucle : les consommateurs
en aval doivent donc être idempotents. L'outbox ne supprime pas les doublons, il supprime la
*divergence* entre l'état et les messages.

**La file de lettres mortes isole l'incurable.** Certains messages échouent à chaque tentative :
charge malformée, référence vers une donnée qui n'existera jamais, bogue déclenché uniquement par ce
contenu. On les appelle des messages empoisonnés. Laissés dans la file, ils sont redélivrés sans
fin ; comme beaucoup de files préservent l'ordre, un empoisonné en tête *bloque tout ce qui le suit*
— un seul message casse le flux entier. La file de lettres mortes est la sortie de secours : après
un nombre borné de tentatives, le courtier déplace le message vers une file distincte, réservée aux
échecs, et laisse le flux normal repartir. Le message n'est ni perdu ni retraité en boucle : il est
mis de côté pour inspection humaine ou rejeu ultérieur après correction. Le compteur de tentatives
est essentiel — sans lui, on ne distingue pas un échec transitoire, qui mérite un réessai, d'un
échec définitif, qui mérite la mise à l'écart.

**Les trois motifs répondent à trois questions distinctes.** Le consommateur idempotent répond à
« que faire d'un message que j'ai déjà traité ». L'outbox répond à « comment ne pas laisser diverger
ce que je change et ce que je publie ». Les lettres mortes répondent à « que faire d'un message que
je ne traiterai jamais ». On les emploie souvent ensemble parce que la livraison au moins une fois
crée les trois problèmes à la fois, mais chacun reste une décision indépendante, à justifier
séparément.

## Exemple commenté

Le noyau décidable est la décision du consommateur — traiter, acquitter un doublon, ou envoyer en
lettres mortes — noyau de l'exercice guidé :

```csharp
public enum ConsumerAction { Process, AckDuplicate, DeadLetter }

// Décide l'action à partir de trois faits : déjà traité ? nombre de tentatives ? seuil ?
public static ConsumerAction Decide(
    bool alreadyProcessed, int deliveryAttempt, int maxAttempts)
{
    // Déjà traité : c'est un doublon. On acquitte sans réexécuter l'effet métier.
    if (alreadyProcessed)
        return ConsumerAction.AckDuplicate;

    // Trop de tentatives : message empoisonné. On l'isole en lettres mortes.
    if (deliveryAttempt >= maxAttempts)
        return ConsumerAction.DeadLetter;

    // Nouveau et sous le seuil : on traite normalement.
    return ConsumerAction.Process;
}
```

L'ordre des tests importe : on vérifie le doublon *avant* le compteur de tentatives, car un doublon
déjà traité ne doit jamais partir en lettres mortes.

## Contre-exemple et erreur fréquente

Le code fautif publie hors de la transaction métier :

```csharp
// FAUTIF : deux systèmes, aucune atomicité. Si Publish échoue après SaveChanges,
// l'état est modifié mais personne n'apprend le fait.
await db.SaveChangesAsync();          // écriture métier validée
await broker.PublishAsync(evt);       // publication séparée : peut échouer seule
```

Le symptôme est une divergence silencieuse : la base dit que la commande est payée, mais l'événement
« commande payée » n'est jamais parti, donc l'expédition n'a jamais lieu. La correction écrit le
message dans une table outbox, dans la même transaction, et laisse un relayeur publier :

```csharp
// CORRIGÉ : l'événement est écrit dans l'outbox, dans la MÊME transaction que l'état.
db.Orders.Update(order);
db.Outbox.Add(new OutboxMessage(evt));  // même contexte, même transaction
await db.SaveChangesAsync();            // les deux réussissent ou échouent ensemble
// Un relayeur séparé lira Outbox et publiera vers le courtier, au moins une fois.
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : pourquoi un consommateur qui reconnaît un doublon doit-il
quand même acquitter le message plutôt que l'ignorer sans acquittement ?

:::quiz
id=senior-messaging-001-check
question=Quel problème le motif outbox résout-il exactement ?
option=Il empêche définitivement tout message d'être livré en double
option=Il rend atomiques ensemble une écriture en base et une publication de message, en écrivant le message dans une table de la même base, dans la même transaction
option=Il accélère la publication en évitant le passage par le courtier
correct=1
success=Exact : l'outbox ramène les deux écritures dans une seule transaction locale, puis un relayeur publie ; les doublons restent possibles et sont gérés par des consommateurs idempotents.
retry=Repensez au fait que base et courtier sont deux systèmes qu'aucune transaction commune ne couvre.
:::

## Exercice guidé

Ouvrez l'exercice `senior-consumer-decision-001` dans `/practice`, puis procédez ainsi.

1. Vérifiez d'abord si l'identifiant du message figure déjà parmi les traités ; si oui, acquittez le
   doublon.
2. Sinon, comparez le nombre de tentatives de livraison au seuil maximal.
3. Au-delà du seuil, dirigez le message vers la file de lettres mortes ; en deçà, traitez-le.
4. Prédisez l'action pour chaque cas, dont un doublon déjà traité et un message à sa tentative de
   trop.

## Exercice autonome

Pour un consommateur qui reçoit des événements « paiement confirmé » et doit créer une expédition,
décrivez les trois protections : quel identifiant rend le consommateur idempotent, ce qu'il écrit
dans une outbox et pourquoi, et à partir de combien de tentatives un message part en lettres mortes.
Précisez qui inspecte la file de lettres mortes et ce qu'il peut en faire.

## Débogage

Un ticket indique : « Une file s'est arrêtée d'avancer cette nuit ; tous les messages en attente
derrière un certain point ne sont jamais traités, et le service redémarre en boucle. »

1. **Symptôme** : la file n'avance plus au-delà d'un message précis, et le consommateur plante de
   façon répétée.
2. **Hypothèse** : un message empoisonné en tête de file échoue à chaque tentative, est redélivré
   sans fin et bloque tout ce qui le suit, faute de file de lettres mortes.
3. **Preuve** : observer que le même identifiant de message revient à chaque redémarrage et que le
   compteur de tentatives augmente sans jamais aboutir à une mise à l'écart.
4. **Prévention** : configurer un seuil de tentatives et une file de lettres mortes pour isoler le
   message empoisonné, puis débloquer le flux et inspecter l'écarté à froid.

## Entretien

Question posée à voix haute : *votre consommateur modifie la base et publie un événement ; comment
évitez-vous que l'un ait lieu sans l'autre ?*

Une réponse solide nomme le problème — deux systèmes, aucune transaction commune — puis décrit
l'outbox : le message écrit dans la même transaction que l'état, un relayeur qui publie ensuite au
moins une fois. Elle enchaîne sur la conséquence — les consommateurs en aval doivent être idempotents
— et mentionne la file de lettres mortes pour les messages qui échouent définitivement.

## Résumé

- Un consommateur idempotent acquitte un doublon sans réexécuter l'effet métier.
- L'outbox ramène écriture et publication dans une seule transaction locale, puis un relayeur publie.
- L'outbox supprime la divergence état/message, pas les doublons : d'où l'exigence d'idempotence.
- La file de lettres mortes isole un message empoisonné après un nombre borné de tentatives.
- Les trois motifs répondent à trois questions distinctes et se justifient séparément.

## Cartes de révision

Question : pourquoi ne peut-on pas rendre atomiques ensemble une écriture en base et une publication
vers un courtier sans outbox ? Réponse attendue : parce que ce sont deux systèmes différents
qu'aucune transaction unique ne couvre ; l'outbox écrit le message dans la même base et la même
transaction que l'état, puis un relayeur le publie séparément.

Question : que fait la file de lettres mortes et pourquoi le compteur de tentatives est-il
essentiel ? Réponse attendue : elle isole un message empoisonné après un nombre borné de tentatives
pour débloquer le flux ; le compteur distingue un échec transitoire, qui mérite un réessai, d'un
échec définitif, qui mérite la mise à l'écart.

## Test de maîtrise

Sans relire, décrivez les trois obligations qu'une livraison au moins une fois impose au consommateur
et le motif qui répond à chacune. Puis expliquez pas à pas le fonctionnement de l'outbox et pourquoi
il n'élimine pas le besoin de consommateurs idempotents.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
