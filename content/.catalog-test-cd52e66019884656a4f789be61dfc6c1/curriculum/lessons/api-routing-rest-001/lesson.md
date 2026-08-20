# Routage REST et ressources stables

## Objectif observable

À la fin de cette leçon, vous saurez concevoir un itinéraire qui nomme une ressource plutôt qu'une
action, choisir entre un identifiant technique et un identifiant métier en connaissant le coût de
chacun, et faire évoluer une API sans casser ses appelants.

## Prérequis

- Avoir lu `api-http-semantics-001` et savoir déduire une méthode de sa sémantique.
- Savoir déclarer un itinéraire dans un contrôleur.

## Intuition

Un itinéraire désigne **une chose**, pas une opération sur cette chose. L'opération est déjà portée
par la méthode HTTP. Écrire `/orders/5` et laisser `GET`, `PUT` ou `DELETE` décider de l'intention
supprime toute la moitié du vocabulaire qu'on serait tenté d'inventer.

Le second réflexe est de considérer une URL publiée comme un engagement : quelqu'un l'a écrite dans
son code, et vous ne savez pas qui.

## Explication

**Nommer des ressources, au pluriel, en minuscules.** `/orders`, `/orders/5`, `/orders/5/lines`. Le
pluriel est une convention, mais s'y tenir évite d'avoir à se demander à chaque fois. La casse est
normalisée parce que les URL sont sensibles à la casse dans leur partie chemin, et qu'un client qui
écrit `/Orders` obtiendrait un `404` incompréhensible.

**La hiérarchie exprime l'appartenance, pas la navigation.** `/orders/5/lines` se justifie parce
qu'une ligne n'existe pas hors de sa commande. En revanche `/customers/2/orders/5/lines/3` n'apporte
rien de plus que `/orders/5/lines/3` : au-delà de deux niveaux, l'itinéraire devient fragile sans
gagner en clarté. La règle pratique : imbriquer quand l'enfant n'a pas de sens seul, sinon exposer
une collection racine avec un filtre.

**Le filtre appartient à la chaîne de requête.** `/orders?status=paid&city=Paris` plutôt que
`/orders/paid/paris`. La différence n'est pas cosmétique : un filtre est optionnel, combinable et
d'arité variable, alors qu'un segment de chemin est positionnel et obligatoire. Ce point est
développé dans `api-pagination-filtering-sorting-001`.

**Les actions qui ne sont pas des ressources.** Certaines opérations ne se laissent pas modéliser
comme la modification d'un état : expédier, annuler, relancer. Deux réponses acceptables. Soit on
modélise l'état comme une sous-ressource — `PUT /orders/5/status` avec la nouvelle valeur — soit on
assume un sous-chemin d'action, `POST /orders/5/shipments`, en le nommant comme la **chose créée**
plutôt que comme le verbe. Ce qu'il faut éviter, c'est le verbe nu : `/orders/ship`.

**Identifiant technique ou identifiant métier.** Exposer une clé de base de données séquentielle
révèle le volume et permet l'énumération : un appelant peut essayer `/orders/1` à `/orders/1000`.
Exposer une référence métier — un numéro de commande — la fige comme contrat public et interdit de la
changer. Un identifiant opaque, non devinable et sans signification, évite les deux problèmes au prix
d'un index supplémentaire.

Le choix dépend de la sensibilité : sur des données publiques, un identifiant séquentiel est sans
conséquence ; sur des commandes clients, il ne l'est pas. Quel que soit le choix, l'autorisation reste
obligatoire — un identifiant imprévisible n'est pas un contrôle d'accès.

**Faire évoluer sans casser.** Ajouter un champ optionnel dans une réponse est compatible ; en
retirer un, en renommer un, ou rendre obligatoire un paramètre qui ne l'était pas ne l'est pas. Quand
la rupture est inévitable, la version se porte dans l'itinéraire — `/v2/orders` — ce qui est lisible,
traçable dans les journaux et facile à router. Un en-tête de version est plus élégant mais invisible
dans une URL partagée et plus facile à oublier côté client.

Le vrai coût d'une version n'est pas de la créer : c'est de maintenir les deux. Une API qui en compte
cinq n'a pas versionné, elle a repoussé.

**Ce qui ne doit pas figurer dans une URL.** Un jeton, un mot de passe, une donnée personnelle. Les
URL sont journalisées par les serveurs, les serveurs mandataires et l'historique du navigateur. Tout
ce qui est sensible passe dans un en-tête ou un corps.

## Exemple commenté

Un ensemble d'itinéraires cohérents pour la même ressource :

```text
GET    /orders                 liste, filtrable et paginée
POST   /orders                 crée, répond 201 avec Location
GET    /orders/{id}            lit une commande
PUT    /orders/{id}            remplace la représentation entière
DELETE /orders/{id}            supprime, répond 204
GET    /orders/{id}/lines      lignes de la commande : elles n'existent pas hors d'elle
POST   /orders/{id}/shipments  crée une expédition : la chose créée, pas le verbe
```

Aucun verbe n'apparaît. L'intention est entièrement portée par la méthode.

La normalisation d'un itinéraire reçu, telle que l'exercice de la semaine la demande :

```csharp
// Un client peut envoyer « /Orders/ », « orders » ou «  /ORDERS  ».
// La forme canonique est en minuscules, sans barre de bordure.
public static string NormalizeRoute(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    // Trim des blancs, puis des barres de bordure uniquement : les barres internes
    // séparent des segments et doivent être conservées.
    return value.Trim().Trim('/').ToLowerInvariant();
}
```

Et la construction de l'en-tête qui accompagne une création :

```csharp
public static string LocationFor(int id)
{
    // Un identifiant nul ou négatif ne désigne aucune ressource : c'est une faute d'appelant.
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
    return $"/orders/{id}";
}
```

## Contre-exemple et erreur fréquente

```text
POST /api/v1/orderService/createNewOrder
GET  /api/v1/orderService/getOrdersByStatusAndCity/paid/paris
POST /api/v1/orderService/order/5/cancelOrderNow
GET  /api/v1/orderService/getOrder?token=abc123&id=5
```

Quatre itinéraires, quatre problèmes distincts.

`createNewOrder` répète ce que `POST` dit déjà, et le suffixe `Service` expose un détail
d'implémentation qui n'intéresse aucun appelant.

Le filtre en segments de chemin oblige à créer un itinéraire par combinaison. Filtrer par ville seule
demanderait un cinquième itinéraire ; les deux critères deviennent obligatoires alors qu'ils sont
optionnels par nature.

`cancelOrderNow` est un verbe nu, avec en prime un adverbe qui ne veut rien dire dans un contrat. La
forme correcte modélise l'état ou la chose créée.

Le jeton dans la chaîne de requête est le plus grave : il sera écrit en clair dans les journaux du
serveur, dans ceux de tout serveur mandataire, et dans l'historique du navigateur. Une
authentification passe par un en-tête, comme vu dans `security-authentication-001`.

## Vérification de compréhension

Pour « relancer le traitement d'une commande en échec », proposez deux itinéraires acceptables et
dites lequel vous retenez et pourquoi.

:::quiz
id=api-routing-rest-001-check
question=Pourquoi exprimer un filtre par la chaîne de requête plutôt que par des segments de chemin ?
option=Parce que la chaîne de requête est chiffrée alors que le chemin ne l'est pas
option=Parce qu'un filtre est optionnel, combinable et d'arité variable, là où un segment de chemin est positionnel et obligatoire
option=Parce que les segments de chemin sont limités à trois niveaux par la norme HTTP
correct=1
success=Correct : mettre les filtres dans le chemin oblige à créer un itinéraire par combinaison et rend obligatoires des critères qui ne le sont pas.
retry=Relisez le passage sur le filtre : la question est de savoir ce qui se passe quand on veut filtrer sur un seul des deux critères.
:::

## Exercice guidé

Ouvrez `api-route-normalize-001` dans `/practice`, puis procédez ainsi.

1. Listez, avant tout code, les formes d'entrée que vous devez accepter : blancs, casse, barres de
   bordure, barres internes, valeur absente.
2. Implémentez la normalisation en conservant les barres internes.
3. Vérifiez le cas d'une chaîne composée uniquement de barres.
4. Ouvrez ensuite `api-location-header-001` pour construire l'en-tête d'une création.

## Exercice autonome

Concevez l'ensemble des itinéraires d'une ressource « facture » et de ses avoirs.

Décidez avant d'écrire : ce qui est imbriqué et ce qui ne l'est pas, la nature de l'identifiant exposé
et sa justification, l'expression des filtres, la modélisation de l'action « émettre un avoir », et ce
que vous ferez le jour où le format de la réponse doit changer de façon incompatible.

## Débogage

Un ticket indique : « Depuis la mise en production, certains clients reçoivent des 404 sur des
commandes qui existent. »

1. **Symptôme** : l'absence est signalée pour des ressources présentes en base, sans régularité
   apparente.
2. **Hypothèse** : la casse de l'itinéraire ou une barre finale diffère selon les clients.
3. **Preuve** : relevez les chemins exacts reçus dans les journaux et comparez-les à la forme
   attendue. Une différence de casse ou une barre finale confirme.
4. **Prévention** : normaliser l'itinéraire à l'entrée, et ajouter des tests couvrant les variantes de
   casse et de barre de bordure.

## Entretien

Question posée à voix haute : *comment modélisez-vous une action qui n'est pas un simple changement
d'état, comme « relancer un paiement » ?*

Une réponse solide propose les deux options — état en sous-ressource, ou création d'une chose qui
représente la tentative — explique le critère de choix, et sait dire pourquoi le verbe nu dans
l'itinéraire est à éviter. Elle reconnaît aussi qu'aucune modélisation n'est parfaite.

## Résumé

- Un itinéraire nomme une ressource ; la méthode porte l'intention.
- Imbriquer seulement quand l'enfant n'a pas de sens seul.
- Les filtres vont dans la chaîne de requête, jamais dans le chemin.
- L'identifiant exposé est un arbitrage entre énumérabilité et contrat figé.
- Rien de sensible dans une URL : elle finit dans les journaux.

## Cartes de révision

Question : quel est le vrai coût d'une version d'API ? Réponse attendue : maintenir les deux en
parallèle, pas créer la nouvelle.

Question : pourquoi un identifiant imprévisible ne remplace-t-il pas l'autorisation ? Réponse
attendue : il rend l'énumération difficile, il n'empêche pas un appelant légitime d'accéder à une
ressource qui ne lui appartient pas.

## Test de maîtrise

Sans relire, concevez les itinéraires d'une ressource « abonnement » avec ses paiements. Justifiez
l'imbrication, la nature de l'identifiant, la modélisation de l'action « suspendre », l'expression
d'un filtre par statut et par période, et votre stratégie de version.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
