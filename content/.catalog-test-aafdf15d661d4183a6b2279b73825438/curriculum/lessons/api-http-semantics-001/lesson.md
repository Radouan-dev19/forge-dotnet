# HTTP : méthodes, statuts et représentations

## Objectif observable

À la fin de cette leçon, vous saurez choisir la méthode et le code de statut d'une opération à partir
de sa sémantique plutôt que de l'habitude, et vous saurez dire lesquelles de vos opérations sont
sûres, idempotentes, ou ni l'une ni l'autre.

## Prérequis

- Avoir lu `ef-core-data-access-001` et savoir distinguer une requête traduite d'un objet en mémoire.
- Savoir lire une requête et une réponse HTTP brutes.

## Intuition

HTTP n'est pas un tuyau : c'est un contrat que tout le monde connaît déjà. Les navigateurs, les
serveurs mandataires, les caches et les clients de vos collègues se comportent différemment selon la
méthode et le statut que vous choisissez.

Respecter cette sémantique, ce n'est pas du purisme : c'est ce qui permet à une infrastructure que
vous ne contrôlez pas de faire ce que vous attendez d'elle.

## Explication

**Sûr, idempotent, ni l'un ni l'autre.** Une méthode est *sûre* si elle ne modifie rien d'observable :
`GET`, `HEAD`, `OPTIONS`. Elle est *idempotente* si l'exécuter dix fois produit le même état final
qu'une seule fois : les méthodes sûres, plus `PUT` et `DELETE`. `POST` n'est ni sûr ni idempotent, et
c'est précisément pour cela qu'il sert de méthode par défaut quand aucune autre ne convient.

La conséquence est très concrète : un client qui perd la réponse d'un `PUT` peut le rejouer sans
risque, alors qu'un `POST` rejoué crée un doublon. C'est la raison pour laquelle une création par
`POST` gagne à accepter une clé d'idempotence fournie par l'appelant.

**`PUT` remplace, `PATCH` modifie partiellement.** `PUT` demande de remplacer la représentation
entière : les champs absents sont effacés, ce qui est la source classique de perte de données quand un
client envoie un objet incomplet. `PATCH` transmet une modification partielle, et son format doit être
déclaré — sans quoi le serveur et le client n'ont pas le même contrat.

**Le statut est une réponse à une question précise.** `200` pour un succès qui renvoie une
représentation, `201` pour une création — accompagné d'un en-tête `Location` pointant la ressource
créée — `204` pour un succès sans corps, typiquement une suppression.

Côté erreurs, la distinction qui compte est celle de la **responsabilité**. `400` : la requête est
mal formée ou invalide, c'est l'appelant qui doit changer quelque chose. `401` : aucune identité
prouvée. `403` : identité connue mais droits insuffisants. `404` : la ressource n'existe pas — ou ne
doit pas être révélée. `409` : la requête est valide mais entre en conflit avec l'état courant, par
exemple une version périmée. `422` : syntaxe correcte mais contenu métier inacceptable. `500` : c'est
vous, et le client ne peut rien y faire.

Confondre `400` et `500` a un coût mesurable : les alertes se déclenchent sur les erreurs serveur, et
noyer des erreurs d'appelant dans les `500` rend la supervision inutile.

**`404` contre `403` est une décision de sécurité.** Répondre `403` sur une ressource existante mais
interdite révèle son existence. Sur une ressource dont la simple existence est une information —
l'identifiant d'un client, par exemple — `404` est le bon choix. C'est un arbitrage entre clarté et
divulgation, à trancher explicitement, pas par défaut.

**Les en-têtes font partie du contrat.** `Content-Type` déclare le format envoyé, `Accept` le format
souhaité. `Location` accompagne toute création. `Retry-After` accompagne un `429` ou un `503` et
indique au client quand réessayer — sans lui, il réessaiera immédiatement et aggravera la situation.

**Ce qui ne doit jamais être dans une réponse.** Une trace d'exception, un nom de table, une chaîne de
connexion, un identifiant interne de session. Le corps d'erreur sert à corriger l'appel, pas à
documenter votre infrastructure. Ce point est développé dans `api-validation-problem-details-001`.

## Exemple commenté

Une opération de création, avec la sémantique complète :

```text
POST /orders HTTP/1.1
Content-Type: application/json
Idempotency-Key: 6f1c0b7e-9f2a-4c31-9d5b-7c2f0a1e4b88

{ "customerId": 2, "lines": [ { "productId": 4, "quantity": 2 } ] }

HTTP/1.1 201 Created
Location: /orders/5
Content-Type: application/json

{ "orderId": 5, "status": "Open", "total": 60.00 }
```

`201` et non `200`, parce qu'une ressource est née. `Location` permet au client de la relire sans
deviner l'itinéraire. La clé d'idempotence rend le rejeu sûr : si le client perd la réponse et
renvoie la même requête, le serveur retourne la commande déjà créée au lieu d'en créer une seconde.

Le même raisonnement en code, réduit à la décision de statut :

```csharp
// Le statut découle de ce qui s'est passé, pas d'un choix par défaut.
public static int StatusForCreation(bool alreadyExisted, bool conflictsWithCurrentState) =>
    conflictsWithCurrentState ? 409   // requête valide, mais l'état courant s'y oppose
    : alreadyExisted          ? 200   // rejeu idempotent : la ressource existait déjà
                              : 201;  // création effective
```

Et la distinction de responsabilité, qui décide de ce que voit la supervision :

```csharp
public static int StatusForFailure(string kind) => kind switch
{
    "validation"   => 400,   // l'appelant doit corriger sa requête
    "unauthorized" => 401,   // aucune identité prouvée
    "forbidden"    => 403,   // identité connue, droits insuffisants
    "notfound"     => 404,   // absente, ou volontairement non révélée
    "conflict"     => 409,   // version périmée, état incompatible
    _              => 500,   // notre faute : rien que l'appelant puisse corriger
};
```

## Contre-exemple et erreur fréquente

```csharp
[HttpPost("/api/getOrderById")]          // Un verbe dans l'itinéraire, et POST pour une lecture.
public IActionResult GetOrder([FromBody] int id)
{
    Order? order = _store.Find(id);
    if (order is null)
    {
        // Statut 200 pour une absence : le client doit lire le corps pour savoir si ça a marché.
        return Ok(new { success = false, message = "Commande introuvable" });
    }

    return Ok(new { success = true, data = order });
}
```

Quatre défauts, tous à conséquence réelle.

`POST` pour une lecture supprime toute possibilité de cache, empêche les navigateurs et les serveurs
mandataires de rejouer la requête en sécurité, et interdit de partager l'URL. Une lecture est sûre :
c'est un `GET`.

Le verbe dans l'itinéraire duplique l'information déjà portée par la méthode. Ce point est développé
dans `api-routing-rest-001`.

Le `200` sur une absence est le plus coûteux : tout client doit désormais examiner le corps pour
savoir si l'appel a réussi. Les outils de supervision comptent un succès, les tableaux de bord sont
faux, et le premier client écrit dans un autre langage se trompera.

Enfin, l'enveloppe `{ success, data }` réinvente ce que le statut exprime déjà, et double le travail
de chaque consommateur.

## Vérification de compréhension

Pour « annuler une commande », dites quelle méthode vous choisissez, si elle est idempotente, et quel
statut vous renvoyez lorsque la commande était déjà annulée.

:::quiz
id=api-http-semantics-001-check
question=Un client perd la réponse d'un appel et le rejoue à l'identique. Quelle méthode garantit que l'état final est le même qu'après un seul appel ?
option=POST, qui est conçu pour la création et gère le rejeu automatiquement
option=PUT, qui est idempotent : le rejouer produit le même état final qu'un appel unique
option=Aucune : tout rejeu doit être empêché côté client
correct=1
success=Correct : l'idempotence est une propriété de la méthode. POST ne l'a pas, d'où l'intérêt d'une clé d'idempotence lorsqu'on veut rendre une création rejouable.
retry=Relisez la distinction entre sûr et idempotent, puis demandez-vous ce que produit un second appel identique.
:::

## Exercice guidé

Ouvrez `api-method-idempotency-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, la liste des méthodes que vous jugez idempotentes et pourquoi.
2. Implémentez la décision, en traitant explicitement l'entrée absente et la casse.
3. Comparez votre liste au résultat des cas cachés et notez tout écart.
4. Ouvrez ensuite `api-http-status-map-001` pour relier la méthode au statut retourné.

Le laboratoire `content/labs/api-mini-erp/` porte un contrôleur complet à lire après l'exercice.

## Exercice autonome

Concevez le contrat HTTP complet d'une opération « expédier une commande ».

Décidez avant d'écrire : la méthode et sa justification, l'itinéraire, le statut de succès, ce que
vous renvoyez si la commande est déjà expédiée, si elle n'existe pas, si l'appelant n'a pas le droit,
et si l'état interdit l'expédition. Justifiez chaque choix par la sémantique, pas par l'habitude.

## Débogage

Un ticket indique : « Le tableau de bord affiche 100 % de succès alors que les utilisateurs signalent
des erreurs. »

1. **Symptôme** : la supervision et l'expérience réelle divergent complètement.
2. **Hypothèse** : les erreurs sont renvoyées avec un statut de succès et un indicateur dans le corps.
3. **Preuve** : relevez les statuts réellement émis sur les appels signalés comme fautifs. Un `200`
   accompagné d'un corps d'erreur confirme l'hypothèse.
4. **Prévention** : faire porter l'issue par le statut, et ajouter un test qui vérifie le code retourné
   pour chaque famille d'échec.

## Entretien

Question posée à voix haute : *quelle différence faites-vous entre `401` et `403`, et entre `403` et
`404` ?*

Une réponse solide oppose absence de preuve d'identité et droits insuffisants, puis explique que le
choix entre `403` et `404` est un arbitrage de divulgation : révéler qu'une ressource existe est
parfois une fuite d'information. Elle donne un cas où elle a tranché dans un sens et un cas dans
l'autre.

## Résumé

- Sûr, idempotent ou ni l'un ni l'autre : la méthode se déduit de cette propriété.
- `PUT` remplace la représentation entière ; les champs absents sont effacés.
- Le statut porte l'issue ; un succès qui contient une erreur casse la supervision.
- `403` contre `404` est une décision de divulgation, pas une préférence.
- Une réponse d'erreur ne documente jamais l'infrastructure.

## Cartes de révision

Question : pourquoi une création par `POST` gagne-t-elle à accepter une clé d'idempotence ? Réponse
attendue : `POST` n'est pas idempotent, donc un rejeu après perte de réponse créerait un doublon.

Question : que perd-on en utilisant `POST` pour une lecture ? Réponse attendue : le cache, le rejeu
sûr par l'infrastructure et la possibilité de partager l'URL.

## Test de maîtrise

Sans relire, écrivez le contrat HTTP de quatre opérations sur une ressource « facture » : lister,
lire, créer, annuler. Pour chacune, donnez la méthode, sa propriété d'idempotence, le statut de
succès, et les statuts d'échec avec la responsabilité qu'ils désignent.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
