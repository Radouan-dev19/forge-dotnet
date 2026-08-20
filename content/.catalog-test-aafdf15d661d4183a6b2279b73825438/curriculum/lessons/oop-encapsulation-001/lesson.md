# Classes et invariants encapsulés

## Objectif observable

À la fin de cette leçon, vous saurez identifier l'invariant d'un objet métier, le faire respecter dès
la construction, et écrire une classe dont aucun appelant ne peut produire un état impossible.

## Prérequis

- Avoir lu `edge-cases-001` et savoir écrire une table de cas avant l'implémentation.
- Savoir déclarer une classe, un constructeur et une propriété.

## Intuition

Un invariant est une phrase qui doit rester vraie pendant toute la vie d'un objet : *le stock réservé
ne dépasse jamais le stock disponible*, *une quantité de ligne est strictement positive*. Ce n'est pas
une règle de validation d'écran, c'est une propriété de l'objet lui-même.

L'encapsulation ne consiste pas à écrire `private` devant les champs. Elle consiste à faire en sorte
qu'aucune séquence d'appels publics ne puisse rendre l'invariant faux.

## Explication

**L'invariant se pose à la construction.** Un objet ne doit jamais exister dans un état invalide, même
transitoirement. Cela signifie que le constructeur valide, et qu'il n'existe pas de constructeur sans
paramètre qui laisserait l'objet à moitié rempli en attendant des affectations. Si la construction
peut échouer pour une raison métier plutôt que pour une faute d'appelant, une méthode de fabrique
statique retournant un résultat explicite est préférable à une exception.

**Le vrai test est la surface publique.** Posez-vous la question : *existe-t-il une suite d'appels
publics qui rend ma phrase fausse ?* Un champ `private` accompagné d'une propriété
`public int Stock { get; set; }` ne protège rien : la propriété rouvre exactement la porte que le
champ fermait. De même, exposer `public List<OrderLine> Lines { get; }` laisse l'appelant appeler
`Lines.Clear()` — la référence est en lecture seule, pas la collection.

La forme qui tient : une collection interne privée, exposée en `IReadOnlyList<T>`, et des méthodes
métier `AddLine` / `RemoveLine` qui revalident l'invariant à chaque changement.

**Les méthodes portent le vocabulaire du domaine.** `order.Cancel(reason)` dit ce qui se passe et
permet de refuser l'annulation d'une commande déjà expédiée. `order.Status = "cancelled"` déplace
cette décision chez tous les appelants, qui l'implémenteront chacun à leur façon — ou l'oublieront.
Le symptôme classique est la règle dupliquée dans trois écrans, avec trois comportements légèrement
différents.

**Distinguer valeur et entité.** Un montant, une plage de dates, une adresse sont des *valeurs* :
elles n'ont pas d'identité propre, deux instances égales sont interchangeables, et elles gagnent à
être immuables. Un `record` ou un `readonly struct` convient. Une commande, un client sont des
*entités* : elles ont un identifiant et un cycle de vie, leur état évolue, et c'est là que les
méthodes métier ont leur place.

Pour une valeur, l'immuabilité rend l'invariant définitif : validé une fois à la construction, il ne
peut plus être rompu. C'est la forme la plus simple d'encapsulation, et souvent la meilleure.

**L'encapsulation se teste.** Le test utile n'est pas « la propriété retourne bien ce que j'ai
mis » : c'est « la séquence d'appels qui devrait être impossible échoue effectivement ». Réserver
plus que le stock disponible doit lever, et le stock doit rester inchangé après l'échec. Cette
seconde assertion compte autant que la première : une exception levée après une mutation partielle
laisse l'objet dans un état incohérent.

## Exemple commenté

```csharp
public sealed class StockItem
{
    private int _reserved;

    public StockItem(string sku, int available)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(available);

        Sku = sku;
        Available = available;
        // Invariant établi dès la construction : 0 <= _reserved <= Available.
    }

    public string Sku { get; }

    public int Available { get; }

    // Lecture seule : aucun appelant ne peut écrire la réservation directement.
    public int Reserved => _reserved;

    public int Remaining => Available - _reserved;

    public void Reserve(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        // La vérification précède la mutation : en cas de refus, l'état reste intact.
        if (quantity > Remaining)
        {
            throw new InvalidOperationException(
                $"Réservation de {quantity} impossible : {Remaining} unité(s) disponible(s).");
        }

        _reserved += quantity;
    }
}
```

Aucune séquence d'appels publics ne peut rendre `_reserved` négatif ou supérieur à `Available`. Le
refus survient **avant** la mutation, donc un appel qui échoue laisse l'objet exactement dans son
état précédent.

## Contre-exemple et erreur fréquente

```csharp
public class StockItem
{
    public string Sku { get; set; } = "";
    public int Available { get; set; }
    public int Reserved { get; set; }

    public void Reserve(int quantity)
    {
        Reserved += quantity;
        if (Reserved > Available)
        {
            throw new InvalidOperationException("Stock insuffisant.");
        }
    }
}
```

Trois failles. Les setters publics permettent `item.Reserved = 9999` sans passer par aucune règle :
l'invariant n'existe que dans les commentaires. Le constructeur implicite autorise un objet sans SKU
et sans stock. Et surtout, `Reserve` mute **avant** de vérifier : après une exception, `Reserved` a
déjà été augmenté. Un appelant qui attrape l'exception et poursuit travaille désormais sur un objet
incohérent, et le prochain appel échouera pour une raison qui n'a plus de rapport avec sa cause.

La correction n'est pas d'ajouter une validation dans les setters, mais de les supprimer : tant que
l'état est modifiable de l'extérieur, la classe ne peut rien garantir.

## Vérification de compréhension

Énoncez en une phrase l'invariant de `StockItem`, puis citez la séquence d'appels que la version
fautive rendait possible et que la version correcte interdit.

:::quiz
id=oop-encapsulation-001-check
question=Qu'est-ce qui prouve qu'une classe encapsule réellement son invariant ?
option=Tous ses champs sont déclarés private
option=Aucune séquence d'appels publics ne permet d'atteindre un état qui rend l'invariant faux
option=Chaque propriété possède un setter contenant une validation
correct=1
success=Correct : la garantie se juge sur la surface publique complète, y compris les collections exposées et l'ordre entre vérification et mutation.
retry=Relisez le passage sur la surface publique : un champ privé doublé d'un setter public ne protège rien.
:::

## Exercice guidé

Ouvrez `csharp-stock-reservation-001` dans `/practice`, puis procédez ainsi.

1. Écrivez l'invariant en une phrase, avant toute ligne de code.
2. Listez les appels publics que vous allez exposer, et pour chacun ce qu'il doit refuser.
3. Implémentez en vérifiant systématiquement avant de muter.
4. Écrivez le test qui prouve que l'état est inchangé après un appel refusé.

## Exercice autonome

Concevez une classe `DateRange` représentant une plage de dates métier.

Décidez avant de coder : l'invariant, si les bornes sont incluses, si une plage d'un seul jour est
valide, si la classe est une valeur ou une entité, et si elle doit être immuable. Justifiez chaque
choix par un cas d'usage concret.

## Débogage

Un ticket indique : « Après un échec de réservation, l'article affiche un stock restant négatif. »

1. **Symptôme** : une valeur impossible apparaît après une opération qui a pourtant échoué.
2. **Hypothèse** : la mutation précède la vérification dans la méthode métier.
3. **Preuve** : posez un point d'arrêt sur la ligne qui lève l'exception et lisez l'état de l'objet à
   ce moment. Si l'état est déjà modifié, l'hypothèse est confirmée.
4. **Prévention** : déplacez la vérification avant la mutation, et ajoutez un test qui compare l'état
   avant et après un appel refusé.

## Entretien

Question posée à voix haute : *comment décidez-vous qu'une règle appartient à l'objet plutôt qu'au
service qui l'utilise ?*

Une réponse solide s'appuie sur un critère observable : si la règle doit tenir quel que soit
l'appelant, elle appartient à l'objet. Elle cite un cas de règle dupliquée dans plusieurs écrans et ce
que la remontée dans le domaine a permis de supprimer.

## Résumé

- Un invariant est une phrase vraie pendant toute la vie de l'objet.
- Il s'établit à la construction et se revalide à chaque mutation.
- Un setter public annule la protection que le champ privé promettait.
- Vérifier avant de muter garantit qu'un refus laisse l'état intact.
- Une valeur gagne à être immuable ; une entité porte des méthodes métier.

## Cartes de révision

Question : pourquoi exposer une collection en `IReadOnlyList<T>` ne suffit-il pas toujours ? Réponse
attendue : si l'instance retournée est la liste interne, un transtypage rouvre l'écriture ; il faut
une copie ou une collection en lecture seule.

Question : quelle assertion complète un test de refus ? Réponse attendue : que l'état de l'objet est
identique avant et après l'appel refusé.

## Test de maîtrise

Sans relire, concevez une classe représentant un compte prépayé avec un solde qui ne peut jamais
devenir négatif. Énoncez l'invariant, listez les méthodes publiques, indiquez pour chacune ce qu'elle
refuse, et écrivez le test qui prouve qu'un débit refusé laisse le solde inchangé.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
