# Budget de requêtes tenu

Ce projet inverse la consigne habituelle : le squelette **fonctionne déjà**. Ses trois méthodes
rendent les bonnes valeurs — et un nombre de requêtes SQL qui trahit leur forme : une interrogation
par élément parcouru, le défaut dit « N plus un ». C'est votre point de départ mesuré, l'« avant ».
Votre travail est l'« après » : **le même résultat, au même volume de données, sous le budget de
requêtes** que chaque jalon impose. L'amélioration de performance se prouve ici de façon
déterministe — on compte des allers-retours au lieu de chronométrer — et à résultat strictement
identique, parce qu'une optimisation qui change le résultat n'en est pas une.

## Ce qui vous est fourni

Le modèle `Shelf`, `Item`, `Movement` et son contexte, le jeu de données de référence, et
`CommandCounter`, l'intercepteur qui compte les commandes SQL réellement envoyées — le même
principe que la base commandes du mini-ERP. **Ne les modifiez pas** : le compteur est votre preuve,
le jeu de données rend les cas reproductibles.

Chaque méthode rend `valeur|requetes`, où `requetes` est le compte de l'intercepteur pendant
l'appel. SQLite stocke les décimaux en texte et ne sait pas les agréger : les totaux se calculent
en mémoire, sur des lignes chargées — c'est leur chargement qui se compte.

Vous pouvez ajouter jusqu'à trois fichiers à côté du rendu.

## Le contrat

```csharp
public static string ShelfValue(int shelfId);
public static string ShelfRank(int take);
public static string ItemPage(int page, int size);
```

### `ShelfValue`

La valeur d'un rayon est la somme, sur ses articles, du prix unitaire multiplié par la quantité
totale de ses mouvements, à deux décimales. Budget : **une requête**. Le squelette en émet deux
plus une par article. Un rayon absent lève `ArgumentOutOfRangeException`.

### `ShelfRank`

Le classement des rayons par nombre de mouvements décroissant, égalités départagées par le nom
croissant, tronqué aux `take` premiers et joint par des virgules. Budget : **une requête**, quel
que soit le nombre de rayons — le squelette en émet une plus une par rayon. Un `take` nul ou
négatif lève `ArgumentOutOfRangeException` avant toute requête.

### `ItemPage`

Les identifiants d'articles triés croissants, page `page` (à partir de 1) de taille `size`, joints
par des virgules — `aucun` si la page tombe au-delà des données. Budget : **une requête**, le tri,
le saut et la taille s'exécutant côté base — le squelette relit chaque article de la tranche un par
un. Une page ou une taille non strictement positive lève `ArgumentOutOfRangeException`.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable contre une base réelle.
Chaque cas vérifie **le couple valeur et coût** : la version de départ passe la valeur et échoue le
coût, ce qui est exactement la définition du travail demandé. Les trois suites vertes font du
projet un livrable vérifié — il satisfait alors l'exigence **performance mesurée** de la porte D.

## Ce qui n'est pas mesuré

Le temps d'exécution en millisecondes, volatile par nature, et le comportement à un volume mille
fois supérieur. La grille les observe ; sachez dire pourquoi compter les allers-retours est une
mesure plus fiable qu'un chronomètre sur ce jeu de données.
