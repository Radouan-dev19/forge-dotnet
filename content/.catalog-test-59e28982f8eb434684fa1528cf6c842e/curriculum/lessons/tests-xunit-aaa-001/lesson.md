# xUnit et structure Arrange Act Assert

## Objectif observable

À la fin de cette leçon, vous saurez écrire un test dont le nom énonce la règle vérifiée, structurer
son corps en trois temps lisibles, et reconnaître les trois façons de rendre une suite de tests peu
fiable.

## Prérequis

- Avoir lu `security-owasp-api-001` et savoir qu'un contrôle sans test n'est pas une garantie.
- Avoir lu `csharp-exceptions-nullable-001` et savoir ce qu'une méthode signale en cas d'erreur.

## Intuition

Un test est un document exécutable. Quelqu'un qui lit son nom doit comprendre la règle sans ouvrir le
corps, et quelqu'un qui lit son corps doit comprendre l'échec sans lancer le débogueur.

Cela impose une contrainte simple : **un test vérifie un comportement**. Un test qui en vérifie six
échoue sur le premier et masque les cinq autres — vous ne saurez jamais combien de choses sont
réellement cassées.

## Explication

**Les trois temps.** *Arrange* prépare l'état et les données. *Act* déclenche l'action, en une seule
ligne. *Assert* vérifie le résultat. Cette structure n'est pas une cérémonie : elle rend visible le
défaut le plus courant — un test qui agit plusieurs fois et ne sait plus lequel de ses appels a
échoué.

Si la partie *Act* comporte plus d'une ligne, c'est en général qu'il y a deux tests à écrire.

**Le nom énonce la règle.** Une bonne convention nomme le sujet, la condition et le résultat attendu :
`Quantite_AuDessusDuMaximum_EstRefusee`. Ce nom apparaît tel quel dans le rapport d'échec. Comparé à
`Test1` ou `TestQuantite`, il transforme une liste d'échecs en liste de régressions compréhensibles.

**Le résultat attendu est écrit en dur.** Recalculer le résultat dans le test avec la même formule que
le code testé ne prouve rien : les deux se trompent ensemble. Écrire `Assert.Equal(4.9m, cost)` est
moins élégant et beaucoup plus utile.

**Trois causes de non-fiabilité.** *L'état partagé* : deux tests qui écrivent dans la même variable
statique passent seuls et échouent ensemble, ou selon l'ordre d'exécution. *L'horloge* : un test qui
lit l'heure courante échoue un jour donné, à une heure donnée. *La dépendance externe* : réseau,
système de fichiers, base réelle — chacune apporte sa part d'échecs qui ne signalent aucune
régression.

Un test qui échoue par intermittence est pire qu'un test absent : l'équipe apprend à ignorer le rouge,
et le jour où le rouge est réel, personne ne le regarde.

**L'horloge s'injecte.** Une règle qui lit l'heure courante n'est pas testable. La règle reçoit la date
observée en paramètre, et c'est l'appelant qui fournit l'heure réelle. Le test peut alors placer le
temps exactement où il veut, y compris sur la frontière.

**Chaque test crée son propre état.** En xUnit, une nouvelle instance de la classe de test est
construite pour chaque méthode : le constructeur sert de préparation, et libérer une ressource se fait
par la méthode de libération. Ce modèle rend l'isolation naturelle — à condition de ne rien mettre en
statique.

**Ce qu'on ne teste pas.** Les propriétés triviales, le code de la plateforme, les correspondances
sans logique. Un test doit pouvoir échouer pour une raison intéressante ; s'il ne le peut pas, il ne
fait que coûter du temps de maintenance.

## Exemple commenté

La structure en trois temps, avec un nom qui énonce la règle :

```csharp
public sealed class QuantityRuleTests
{
    [Fact]
    public void Quantite_AuDessusDuMaximum_EstRefusee()
    {
        // Arrange : l'état nécessaire, et rien de plus.
        const int quantity = 1_001;

        // Act : une seule ligne. Deux lignes ici signaleraient deux tests.
        bool accepted = OrderRules.IsValidQuantity(quantity);

        // Assert : le résultat attendu est écrit en dur, pas recalculé.
        Assert.False(accepted);
    }
}
```

La règle rendue testable en recevant la date observée :

```csharp
// L'horloge n'est pas lue ici : elle est fournie. Le test peut placer
// « aujourd'hui » exactement sur la frontière, et le résultat est reproductible.
public static bool IsExpired(DateOnly expiresOn, DateOnly observedOn) => expiresOn < observedOn;
```

```csharp
[Fact]
public void Expiration_LeJourMeme_NestPasEncoreExpiree()
{
    DateOnly expiresOn = new(2026, 8, 5);
    DateOnly observedOn = new(2026, 8, 5);

    bool expired = Subscription.IsExpired(expiresOn, observedOn);

    // La frontière exacte : le jour de l'échéance, l'abonnement est encore valide.
    Assert.False(expired);
}
```

Et l'isolation par construction, sans aucun état statique :

```csharp
public sealed class CartTests
{
    private readonly Cart _cart;

    // Une nouvelle instance par méthode de test : chaque test part d'un panier neuf.
    // Un champ statique ici ferait échouer les tests selon leur ordre d'exécution.
    public CartTests() => _cart = new Cart();

    [Fact]
    public void Panier_ApresAjout_ContientUnArticle()
    {
        _cart.Add(new Item("ref-1", 2));

        Assert.Single(_cart.Items);
    }
}
```

## Contre-exemple et erreur fréquente

```csharp
public class Tests
{
    // État partagé entre tous les tests de la classe : l'ordre d'exécution décide du résultat.
    private static readonly Cart Cart = new();

    [Fact]
    public void Test1()
    {
        Cart.Add(new Item("ref-1", 2));
        Assert.Single(Cart.Items);

        Cart.Add(new Item("ref-2", 1));
        Assert.Equal(2, Cart.Items.Count);

        Cart.Remove("ref-1");
        Assert.Single(Cart.Items);

        // Recalcul avec la même formule que le code testé : les deux se trompent ensemble.
        decimal expected = Cart.Items.Sum(item => item.Price * item.Quantity);
        Assert.Equal(expected, Cart.Total);

        // Lecture de l'horloge : ce test échouera le 31 décembre au soir.
        Assert.Equal(DateTime.Now.Year, Cart.CreatedOn.Year);
    }
}
```

Cinq défauts dans une seule méthode.

Le nom `Test1` n'apprend rien. Quand il échoue dans un rapport de construction, il faut ouvrir le code
pour savoir ce qui est cassé.

Le panier statique est partagé : lancer un second test de la même classe le trouvera déjà rempli. La
suite passe ou échoue selon l'ordre, ce qui est la définition d'un test non fiable.

Les quatre assertions successives vérifient quatre comportements. La première qui échoue masque les
trois autres, et le rapport n'indique jamais l'étendue réelle du problème.

Le total recalculé avec `Sum` reproduit la logique testée : si la formule du code est fausse, celle du
test l'est aussi, et le test passe.

`DateTime.Now.Year` fait dépendre le résultat du moment d'exécution. Le test réussira toute l'année et
échouera une nuit par an — au pire moment.

## Vérification de compréhension

Un test passe quand il est lancé seul et échoue quand toute la classe est exécutée. Nommez les deux
causes possibles et dites comment les distinguer.

:::quiz
id=tests-xunit-aaa-001-check
question=Pourquoi écrire le résultat attendu en dur plutôt que de le recalculer dans le test ?
option=Parce qu'une valeur littérale s'exécute plus vite qu'un calcul
option=Parce que recalculer avec la même formule que le code testé fait que les deux se trompent ensemble : le test ne peut plus détecter l'erreur
option=Parce que le cadre de test refuse les expressions dans une assertion
correct=1
success=Correct : un test qui reproduit la logique testée ne vérifie que sa propre cohérence, jamais la justesse du résultat.
retry=Relisez le passage sur le résultat attendu, et demandez-vous ce qui se passe si la formule du code est fausse.
:::

## Exercice guidé

Ouvrez `tests-quantity-rule-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, les quatre valeurs qui encadrent la plage acceptée : juste en dessous du
   minimum, le minimum, le maximum, juste au-dessus.
2. Implémentez la règle en exprimant directement la plage inclusive.
3. Vérifiez les quatre frontières, une assertion par comportement.
4. Enchaînez avec `tests-expiry-clock-001` pour rendre une règle temporelle reproductible.

## Exercice autonome

Écrivez la suite de tests d'une règle « un panier peut être validé ».

Décidez avant d'écrire : la liste des comportements, le nom de chaque test, ce que contient la partie
de préparation, ce qui reste hors du test, la façon dont vous traitez la date de validation, et ce que
vous refusez de tester parce que ce serait sans intérêt.

## Débogage

Un ticket indique : « La construction échoue une fois sur cinq, toujours sur un test différent. »

1. **Symptôme** : échec intermittent, sans lien avec les modifications apportées.
2. **Hypothèse** : de l'état est partagé entre tests, ou une horloge est lue directement.
3. **Preuve** : exécutez les tests dans un ordre différent et cherchez les champs statiques ainsi que
   les lectures d'heure courante.
4. **Prévention** : supprimer tout état statique, injecter l'horloge, et traiter un test intermittent
   comme un défaut réel plutôt que de le relancer.

## Entretien

Question posée à voix haute : *qu'est-ce qui fait qu'un test est bon ?*

Une réponse solide parle de lisibilité du nom, d'un seul comportement vérifié, d'indépendance vis-à-vis
de l'ordre et du moment d'exécution, et sait dire pourquoi un test intermittent est plus nuisible
qu'un test absent.

## Résumé

- Trois temps : préparer, agir une fois, vérifier.
- Le nom énonce sujet, condition et résultat attendu.
- Le résultat attendu s'écrit en dur, jamais recalculé.
- État partagé, horloge et dépendance externe rendent une suite peu fiable.
- Un test intermittent apprend à l'équipe à ignorer le rouge.

## Cartes de révision

Question : que signale une partie *Act* de plusieurs lignes ? Réponse attendue : il y a probablement
deux tests à écrire.

Question : pourquoi une nouvelle instance de classe de test par méthode aide-t-elle ? Réponse
attendue : elle rend l'isolation naturelle, tant qu'aucun champ n'est statique.

## Test de maîtrise

Sans relire, écrivez la suite complète des tests d'une règle de remise : la liste des comportements, le
nom de chaque test, la structure en trois temps, le traitement des frontières, la façon dont vous
évitez l'état partagé et la lecture d'horloge, et ce que vous choisissez de ne pas tester.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
