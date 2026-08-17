# Base commandes mini-ERP

Trois méthodes qui interrogent et modifient une **vraie base de données**. Le bac à sable embarque
EF Core et SQLite : ce que vous écrivez ici est exécuté contre un moteur réel, et non simulé.

## Ce qui vous est fourni

Le squelette contient le modèle — `Customer`, `Order`, `OrderLine`, `OrdersContext` — le jeu de
données de référence, et un intercepteur qui compte les commandes SQL réellement envoyées. **Ne les
modifiez pas** : le jeu de données est ce qui rend les cas reproductibles, et le changer ferait
échouer vos propres tests.

Vous pouvez ajouter jusqu'à trois fichiers à côté du rendu.

## Le contrat

```csharp
public static string OrderSummary(int orderId);
public static int    LoadRoundTrips(int customerId);
public static string ApplyDiscount(int orderId, decimal rate);
```

### `OrderSummary`

Rend `client|nombreDeLignes|total`, le total étant la somme des prix unitaires multipliés par leurs
quantités, à deux décimales. Une commande absente lève `ArgumentOutOfRangeException`.

Note utile : SQLite stocke les décimaux en texte et **ne sait pas les additionner**. L'agrégation se
fait donc en mémoire, sur les lignes chargées — ce qui suppose de les avoir chargées.

### `LoadRoundTrips`

Charge un client, ses commandes et leurs lignes, puis rend le **nombre de commandes SQL envoyées**,
tel que l'intercepteur fourni l'a compté. Chargé naïvement, ce nombre croît avec les données : c'est
le défaut dit « N plus un ». Bien chargé, il ne bouge pas.

Un client absent lève `ArgumentOutOfRangeException`.

### `ApplyDiscount`

Applique le taux à **chaque prix unitaire**, arrondi à deux décimales, les demis s'éloignant de zéro.
Enregistre, puis relit dans un **contexte neuf** et rend le total obtenu.

La relecture dans un contexte distinct est le point : depuis le contexte qui vient d'écrire, l'entité
est encore suivie en mémoire et rendrait la bonne valeur même si l'enregistrement n'avait pas eu
lieu. Seul un contexte neuf prouve que l'écriture est allée jusqu'à la base.

Le taux appartient à l'intervalle de zéro à un inclus ; en dehors, `ArgumentOutOfRangeException`
avant toute écriture. Une commande absente est refusée de la même façon.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable contre une base réelle. Les
trois doivent être vertes pour que le projet compte comme livrable vérifié — et il satisfait alors
l'exigence **EF Core** de la porte B.

## Ce qui n'est pas mesuré

Le choix des index, la stratégie de migration, et ce que vous feriez d'un jeu de données mille fois
plus gros. La grille les observe ; préparez-vous à en parler.
