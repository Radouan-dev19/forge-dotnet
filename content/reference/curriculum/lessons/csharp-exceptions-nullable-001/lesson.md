# Exceptions et nullable sans ambiguïté

## Objectif observable

À la fin de cette leçon, vous saurez décider si une situation donnée relève d'une exception ou d'une
valeur absente modélisée, et vous saurez lire un avertissement de nullabilité sans le faire taire
avec l'opérateur `!`.

## Prérequis

- Avoir lu `oop-interfaces-composition-001` et savoir injecter une dépendance.
- Savoir lever et attraper une exception en C#.

## Intuition

Deux situations très différentes se ressemblent en surface. *« Ce client n'a aucune commande »* est
une réponse normale du domaine. *« On m'a passé une quantité négative »* est une faute de l'appelant.

La première se modélise par une valeur : collection vide, référence nullable documentée. La seconde se
refuse par une exception. Confondre les deux produit soit des exceptions dans le chemin nominal, soit
des données inventées qui échouent trois étapes plus loin.

## Explication

**Le critère de décision.** Posez la question : *un appelant correct peut-il rencontrer cette
situation ?* Si oui, c'est une valeur — l'absence fait partie du contrat. Si non, c'est une exception —
le contrat a été violé. Une recherche qui ne trouve rien est normale ; un identifiant `null` passé à
une méthode qui exige un identifiant ne l'est pas.

Un corollaire utile : les exceptions ne doivent pas servir de contrôle de flux. Si le chemin nominal
de votre application lève et rattrape régulièrement, la modélisation est à revoir. C'est la différence
entre `TryGetValue`, qui répond par un booléen à une absence attendue, et l'indexeur, qui lève parce
que la clé aurait dû être là.

**Choisir le bon type d'exception.** `ArgumentNullException`, `ArgumentOutOfRangeException` et
`ArgumentException` désignent une faute d'appelant. `InvalidOperationException` désigne un appel
correct mais fait au mauvais moment — annuler une commande déjà expédiée. Une exception métier
dédiée se justifie lorsque l'appelant doit pouvoir la distinguer pour réagir différemment ; sinon,
elle ajoute un type sans bénéfice.

**Le message d'exception s'adresse à celui qui débogue.** « Erreur » n'aide personne. Un bon message
nomme la valeur reçue et l'attente violée : *« Réservation de 12 impossible : 5 unité(s)
disponible(s). »* Il ne contient en revanche jamais de secret, de jeton ni de donnée personnelle : le
message finira dans un journal.

**Ne jamais avaler une exception.** `catch (Exception) { return 0; }` remplace une cause identifiable
par une donnée fausse d'apparence valide. Le symptôme réapparaît plus loin, sans lien visible avec sa
cause, et le temps de diagnostic est multiplié. Si vous attrapez, faites l'une des trois choses :
traiter réellement le cas, enrichir puis relancer avec `throw;`, ou laisser passer. Notez que `throw;`
préserve la pile d'origine alors que `throw ex;` la réinitialise et détruit l'information.

**Le nullable est un contrat vérifié par le compilateur.** Avec le contexte nullable activé,
`Customer?` annonce que la valeur peut être absente et le compilateur exige que vous le vérifiiez
avant usage. `Customer` annonce l'inverse. L'opérateur `!` — dit *null-forgiving* — dit au compilateur
« fais-moi confiance ». Chaque usage est une promesse non vérifiée : quand elle est fausse, on obtient
une `NullReferenceException` à l'endroit exact que l'avertissement signalait.

Il existe des cas légitimes, typiquement après une vérification que le compilateur ne peut pas suivre.
Ils méritent alors un commentaire justifiant la promesse. Le réflexe d'ajouter `!` pour faire taire un
avertissement est, lui, une dette immédiate.

Deux opérateurs remplacent avantageusement `!` : `?.` propage l'absence au lieu de planter, et `??`
fournit une valeur de repli explicite.

## Exemple commenté

```csharp
public sealed class OrderBook
{
    private readonly Dictionary<string, Order> _orders = new(StringComparer.Ordinal);

    // Absence attendue : un identifiant inconnu est un cas normal, signalé par le type nullable.
    public Order? Find(string orderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);   // Faute d'appelant : exception.
        return _orders.TryGetValue(orderId, out Order? order) ? order : null;
    }

    // Contrat différent, annoncé par le nom : l'absence devient ici une violation.
    public Order GetRequired(string orderId)
    {
        return Find(orderId)
            ?? throw new InvalidOperationException($"Commande « {orderId} » introuvable.");
    }
}
```

Côté appelant, l'absence se traite sans jamais risquer de `NullReferenceException` :

```csharp
Order? order = book.Find(id);
string label = order?.Reference ?? "Commande inconnue";
```

Deux méthodes pour deux contrats, chacun lisible depuis sa signature. `Find` retourne `Order?` parce
que l'absence est prévue ; `GetRequired` retourne `Order` parce que l'appelant a affirmé qu'elle
existait.

## Contre-exemple et erreur fréquente

```csharp
public decimal GetTotal(string orderId)
{
    try
    {
        Order order = _orders[orderId]!;      // L'indexeur lève, le ! masque l'avertissement.
        return order.Lines.Sum(line => line.Amount);
    }
    catch (Exception)
    {
        return 0m;                            // Une commande inconnue vaut désormais 0 EUR.
    }
}
```

Trois défauts s'enchaînent. L'indexeur lève une `KeyNotFoundException` sur une absence pourtant
attendue : une exception sert de contrôle de flux. Le `!` fait taire l'avertissement au lieu de
traiter le cas. Et le `catch` général convertit **toute** défaillance — clé absente, mais aussi bug
de calcul ou dépassement — en un total de zéro parfaitement plausible.

Le rapport comptable affichera 0 EUR sans aucune trace. Le jour où quelqu'un s'en aperçoit, la pile
d'origine a disparu depuis longtemps.

La correction utilise `TryGetValue` pour l'absence attendue, laisse remonter ce qui est réellement
anormal, et retourne `decimal?` ou lève selon le contrat voulu.

## Vérification de compréhension

Pour la recherche d'un client par courriel, dites si l'absence est une valeur ou une exception, puis
donnez la signature qui l'exprime.

:::quiz
id=csharp-exceptions-nullable-001-check
question=Quel critère décide entre modéliser une absence et lever une exception ?
option=La fréquence : au-delà d'un cas sur cent, on modélise
option=Un appelant correct peut-il rencontrer la situation ? Si oui c'est une valeur, sinon c'est une violation de contrat
option=Le type de retour : les types référence modélisent, les types valeur lèvent
correct=1
success=Correct : l'absence attendue fait partie du contrat et se modélise ; la violation de contrat se refuse par une exception.
retry=Relisez le critère de décision et l'opposition entre TryGetValue et l'indexeur d'un dictionnaire.
:::

## Exercice guidé

Ouvrez `csharp-nullable-fallback-001` dans `/practice`, puis procédez ainsi.

1. Classez chaque situation du sujet en « absence attendue » ou « violation de contrat ».
2. Écrivez la signature qui exprime ce classement, avant le corps.
3. Implémentez sans utiliser l'opérateur `!`.
4. Vérifiez qu'aucun avertissement de nullabilité ne subsiste, et que le compilateur ne réclame rien.

## Exercice autonome

Écrivez une méthode qui retourne la dernière commande payée d'un client.

Décidez avant de coder : le comportement pour un client inexistant, pour un client sans commande
payée, et pour un identifiant vide. Justifiez pour chacun le choix entre valeur et exception, puis
écrivez la signature correspondante.

## Débogage

Un ticket indique : « Le récapitulatif affiche 0 EUR pour certaines commandes, sans erreur dans les
journaux. »

1. **Symptôme** : une valeur plausible mais fausse, et aucune trace.
2. **Hypothèse** : un `catch` général convertit une défaillance en valeur par défaut.
3. **Preuve** : cherchez les `catch (Exception)` du chemin exécuté, puis posez un point d'arrêt sur le
   bloc et lisez le type réel de l'exception attrapée.
4. **Prévention** : supprimez le repli silencieux, traitez l'absence attendue par `TryGetValue`, et
   ajoutez un test sur un identifiant inconnu qui vérifie le comportement voulu.

## Entretien

Question posée à voix haute : *quand utilisez-vous l'opérateur `!` en C# ?*

Une réponse solide reconnaît qu'il s'agit d'une promesse non vérifiée, cite le cas légitime — une
vérification que le compilateur ne peut pas suivre — et explique pourquoi le faire taire par réflexe
déplace le problème vers l'exécution.

## Résumé

- L'absence qu'un appelant correct peut rencontrer se modélise ; la violation de contrat se refuse.
- Les exceptions ne servent pas de contrôle de flux dans le chemin nominal.
- Un message d'exception nomme la valeur et l'attente, jamais un secret.
- `throw;` préserve la pile ; `throw ex;` la détruit.
- L'opérateur `!` est une promesse : justifiez-la ou remplacez-la par `?.` et `??`.

## Cartes de révision

Question : que masque un `catch (Exception)` qui retourne une valeur par défaut ? Réponse attendue :
la cause réelle, remplacée par une donnée plausible qui échouera plus tard sans lien visible.

Question : quelle différence de contrat entre `Order? Find(id)` et `Order GetRequired(id)` ? Réponse
attendue : la première annonce une absence prévue, la seconde affirme que l'appelant garantit la
présence.

## Test de maîtrise

Sans relire, écrivez les deux signatures d'un dépôt de clients : une recherche tolérante et une
lecture obligatoire. Indiquez pour chacune ce qui est refusé et comment, puis écrivez les trois cas de
test qui distinguent identifiant vide, identifiant inconnu et identifiant valide.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
