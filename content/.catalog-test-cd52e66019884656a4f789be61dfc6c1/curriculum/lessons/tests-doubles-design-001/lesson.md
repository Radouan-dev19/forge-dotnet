# Doubles de test par intention

## Objectif observable

À la fin de cette leçon, vous saurez choisir entre un stub, un fake et un spy à partir de ce que le
test doit prouver, et vous saurez reconnaître le double qui rend une suite fragile sans rien vérifier
de plus.

## Prérequis

- Avoir lu `tests-boundaries-theories-001` et savoir choisir des cas.
- Avoir lu `oop-interfaces-composition-001` et savoir substituer une implémentation.

## Intuition

Un double remplace une dépendance réelle. La question n'est pas *quelle bibliothèque* mais **quelle
intention** : que doit prouver ce test ?

Trois intentions, trois familles. *Fournir une réponse* pour que le code sous test puisse continuer :
c'est un stub. *Se comporter comme la vraie chose*, en plus simple : c'est un fake. *Constater qu'un
appel a bien eu lieu*, parce que c'est justement l'effet attendu : c'est un spy.

Choisir la mauvaise famille est ce qui produit ces suites de tests qui cassent à chaque modification
sans jamais détecter de régression.

## Explication

**Le stub répond, sans mémoire.** Il retourne une valeur fixe pour que le code sous test atteigne le
comportement à vérifier. Il ne vérifie rien lui-même. C'est le double le plus courant, parce que la
plupart des tests s'intéressent au **résultat**, pas au trajet.

**Le fake se comporte.** C'est une implémentation réelle mais simplifiée : un dépôt en mémoire, une
horloge fixée, une file en liste. Il permet de tester une séquence complète — écrire puis relire —
sans dépendance externe. C'est souvent le meilleur choix quand plusieurs tests partagent la même
dépendance : un fake bien écrit se remplace tout seul.

**Le spy constate.** Il retient ce qu'on lui a demandé, et le test vérifie ensuite qu'un appel a eu
lieu. C'est légitime **quand l'appel est le comportement attendu** : envoyer un courriel, publier un
message, écrire dans un journal d'audit. Dans ces cas, il n'y a pas de valeur de retour à examiner —
l'effet de bord *est* le résultat.

**La règle de choix.** Si le test peut vérifier un résultat retourné, il ne doit pas vérifier un appel.
Vérifier l'appel en plus du résultat n'ajoute aucune garantie et ajoute une raison de casser. C'est
exactement la ligne tracée par `tests-domain-rules-001` entre règle et implémentation.

**Le double le plus fragile est celui qui vérifie trop.** Exiger qu'une méthode ait été appelée
exactement une fois, dans cet ordre, avec ces arguments précis, fige une implémentation entière. Un
cache ajouté, une fusion de deux appels, un changement d'ordre sans effet : chacun casse le test, et
aucun ne casse le comportement.

**Le double le plus dangereux est celui qui ment.** Un stub qui retourne toujours un succès rend le
test vert quel que soit le code. Un fake dont le comportement diverge de la vraie implémentation fait
passer des tests sur une réalité qui n'existe pas. Un fake mérite ses propres tests, ou mieux : le même
jeu de tests que l'implémentation réelle, exécuté contre les deux.

**Ne doublez que ce qui vous appartient.** Substituer une abstraction que vous avez définie est sain.
Doubler directement un type d'une bibliothèque tierce revient à figer une hypothèse sur son
comportement, hypothèse que la prochaine version invalidera sans prévenir. La forme robuste consiste à
placer votre propre interface devant la dépendance externe, puis à la doubler.

**Ne doublez pas ce qui est pur.** Une règle sans dépendance ne se double jamais : on l'appelle
directement. Introduire une abstraction uniquement pour tester ajoute du code sans rien gagner.

## Exemple commenté

La décision, ramenée à sa question :

```csharp
public static string DoubleKind(bool verifiesInteraction, bool needsBehaviour)
{
    // L'interaction d'abord : quand l'appel lui-même est le résultat attendu,
    // aucune valeur de retour ne peut en témoigner.
    if (verifiesInteraction)
    {
        return "spy";
    }

    // Ensuite le comportement : écrire puis relire exige plus qu'une réponse fixe.
    // Sinon, une simple réponse suffit.
    return needsBehaviour ? "fake" : "stub";
}
```

Les trois familles, sur la même dépendance :

```csharp
// Stub : une réponse fixe, aucune mémoire. Le test s'intéresse au résultat calculé.
private sealed class FixedRateSource : IRateSource
{
    public decimal Get(string currency) => 1.1m;
}

// Fake : un comportement réel, simplifié. Permet d'écrire puis de relire.
private sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<int, Order> _orders = [];
    private int _nextId = 1;

    public int Save(Order order)
    {
        int id = _nextId++;
        _orders[id] = order with { Id = id };
        return id;
    }

    public Order? Find(int id) => _orders.GetValueOrDefault(id);
}

// Spy : retient ce qui a été demandé, parce que l'envoi est le comportement attendu.
private sealed class RecordingNotifier : INotifier
{
    public List<string> Sent { get; } = [];

    public void Notify(string recipient) => Sent.Add(recipient);
}
```

Et l'usage de chacun, avec la justification du choix :

```csharp
[Fact]
public void Commande_Validee_NotifieLeClient()
{
    // Fake pour l'état, spy pour l'effet attendu, stub pour la valeur d'appoint.
    InMemoryOrderRepository repository = new();
    RecordingNotifier notifier = new();
    OrderService service = new(repository, notifier, new FixedRateSource());

    service.Validate(new Order(CustomerEmail: "client@exemple.local"));

    // L'envoi n'a pas de valeur de retour : le constater est le seul moyen de le prouver.
    Assert.Equal(["client@exemple.local"], notifier.Sent);
}
```

## Contre-exemple et erreur fréquente

```csharp
[Fact]
public void Total_EstCalcule()
{
    var repository = new Mock<IOrderRepository>();
    var rates = new Mock<IRateSource>();
    var logger = new Mock<ILogger<OrderService>>();
    var clock = new Mock<IClock>();

    repository.Setup(r => r.Find(1)).Returns(new Order());
    rates.Setup(r => r.Get("EUR")).Returns(1m);
    clock.Setup(c => c.Now).Returns(new DateTime(2026, 8, 5));

    var service = new OrderService(repository.Object, rates.Object, logger.Object, clock.Object);

    decimal total = service.Total(1);

    Assert.Equal(120m, total);

    // Vérifications d'appels alors que le résultat a déjà été vérifié :
    // aucune garantie de plus, quatre raisons de casser de plus.
    repository.Verify(r => r.Find(1), Times.Once);
    rates.Verify(r => r.Get("EUR"), Times.Once);
    clock.Verify(c => c.Now, Times.AtLeastOnce);
    logger.Verify(l => l.Log(LogLevel.Information, It.IsAny<EventId>(),
        It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

Trois défauts.

Le résultat est déjà vérifié par `Assert.Equal(120m, total)`. Les quatre `Verify` qui suivent ne
prouvent rien de plus sur le comportement, mais figent la façon dont il est obtenu. Mettre le taux en
cache — donc appeler `Get` zéro fois au second passage — fera échouer ce test sans qu'aucun total ne
change.

La vérification du journal est la pire : elle transforme un message de journalisation, qui n'est pas un
contrat, en comportement obligatoire. Reformuler ce message casse le test.

Enfin, quatre doubles pour une seule assertion signale un service qui en fait trop. La difficulté à
préparer le test est ici un diagnostic de conception : la règle de calcul devrait être une fonction
pure, appelée sans aucun double.

La correction : extraire le calcul, le tester directement, et ne conserver un spy que là où l'effet de
bord est réellement le comportement attendu.

## Vérification de compréhension

Pour chacun de ces trois tests, dites quelle famille de double vous utilisez et pourquoi : « le total
appliqué au taux du jour est correct », « une commande enregistrée peut être relue », « la validation
envoie un accusé de réception ».

:::quiz
id=tests-doubles-design-001-check
question=Quand la vérification d'un appel est-elle justifiée plutôt qu'une assertion sur un résultat ?
option=Toujours : vérifier l'appel garantit que la dépendance a bien été sollicitée
option=Quand l'appel lui-même est le comportement attendu — un envoi, une publication, une écriture d'audit — et qu'aucune valeur de retour n'en témoigne
option=Quand la dépendance est lente à exécuter dans un test
correct=1
success=Correct : si un résultat peut être vérifié, le vérifier suffit. Ajouter la vérification d'appel n'ajoute aucune garantie, seulement une raison de casser.
retry=Relisez la règle de choix, et demandez-vous ce que prouve une vérification d'appel quand le résultat est déjà vérifié.
:::

## Exercice guidé

Ouvrez `tests-double-choice-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, la question que vous posez en premier pour trancher.
2. Implémentez la décision en respectant l'ordre de priorité entre les deux critères.
3. Vérifiez le cas où les deux indicateurs sont vrais simultanément.
4. Enchaînez avec `tests-reset-state-001`, qui traite la remise à zéro d'un état de test.

## Exercice autonome

Écrivez les tests d'un service de relance : il lit les factures impayées, calcule une pénalité, écrit
le résultat et envoie une notification.

Décidez avant d'écrire : quelle dépendance reçoit quelle famille de double et pourquoi, ce que vous
testez sans aucun double, ce que vous refusez de vérifier bien que ce soit possible, et le test qui
prouve que votre fake ne ment pas.

## Débogage

Un ticket indique : « Ajouter un cache a fait échouer douze tests, alors que tous les résultats sont
identiques. »

1. **Symptôme** : des échecs massifs sans changement de comportement observable.
2. **Hypothèse** : les tests vérifient le nombre d'appels aux dépendances.
3. **Preuve** : lisez les assertions des tests rouges. Des vérifications d'appel accompagnant une
   assertion de résultat confirment.
4. **Prévention** : supprimer les vérifications d'appel redondantes, et n'en garder que là où l'effet
   de bord est le comportement.

## Entretien

Question posée à voix haute : *quelle différence faites-vous entre un stub et un mock ?*

Une réponse solide part de l'intention plutôt que de la bibliothèque, cite les trois familles avec un
exemple concret de chacune, et sait dire que vérifier un appel quand un résultat suffit fragilise la
suite sans rien garantir.

## Résumé

- Le stub répond, le fake se comporte, le spy constate.
- Si un résultat peut être vérifié, ne vérifiez pas l'appel.
- Un double qui exige un ordre et un nombre d'appels fige l'implémentation.
- Un fake qui diverge de la réalité fait passer des tests sur une fiction.
- Ne doublez ni ce qui est pur, ni directement un type tiers.

## Cartes de révision

Question : que signale un test qui a besoin de quatre doubles ? Réponse attendue : un service qui en
fait trop — la difficulté de préparation est un diagnostic de conception.

Question : comment s'assurer qu'un fake ne ment pas ? Réponse attendue : lui faire subir le même jeu
de tests que l'implémentation réelle.

## Test de maîtrise

Sans relire, décrivez la stratégie de doublure complète d'un service de facturation : chaque
dépendance, la famille retenue, sa justification, ce qui est testé sans double, ce que vous refusez de
vérifier, et le mécanisme qui garantit que vos fakes restent fidèles.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
