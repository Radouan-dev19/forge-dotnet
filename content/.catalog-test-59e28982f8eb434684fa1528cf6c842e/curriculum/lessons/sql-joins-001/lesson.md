# Jointures et cardinalités

## Objectif observable

À la fin de cette leçon, vous saurez prédire le nombre de lignes que produit une jointure avant de
l'exécuter, expliquer pourquoi un total se met à doubler après l'ajout d'une table, et choisir entre
`JOIN`, `EXISTS` et `IN` selon la question posée.

## Prérequis

- Avoir lu `sql-select-filters-001` et savoir situer une clause dans l'ordre logique d'évaluation.
- Connaître le schéma du laboratoire : `Customers`, `Products`, `Orders`, `OrderLines`.

## Intuition

Une jointure ne « rassemble » pas des tables : elle produit **une ligne par couple correspondant**.
C'est la seule phrase à retenir, et elle explique tous les résultats surprenants.

Si un client a trois commandes, joindre `Customers` et `Orders` produit trois lignes pour ce client —
son nom y apparaît trois fois. Ce n'est pas un défaut : c'est la définition. L'erreur commence quand
on agrège sans en tenir compte.

## Explication

**Trois formes, trois questions.** `INNER JOIN` ne garde que les couples qui correspondent des deux
côtés : un client sans commande disparaît. `LEFT JOIN` conserve toutes les lignes de gauche et
complète par des valeurs absentes à droite : le client sans commande apparaît, avec `NULL` partout où
la commande aurait dû être. `CROSS JOIN` produit tous les couples possibles — utile pour générer des
combinaisons, catastrophique par accident.

**La multiplication des lignes est le piège numéro un.** Joindre `Orders` et `OrderLines` produit une
ligne par **ligne de commande**, pas par commande. Toute agrégation portant sur une colonne de
`Orders` compte alors chaque commande autant de fois qu'elle a de lignes.

C'est ainsi qu'un chiffre d'affaires se met à doubler après qu'un développeur a « juste ajouté une
jointure pour récupérer le nom du produit ». Le symptôme est un total trop grand, sans erreur, et il
survit longtemps parce que personne ne recalcule à la main.

Deux remèdes. Agréger **avant** de joindre, en calculant le total par commande dans une sous-requête
ou une expression de table, puis joindre le résultat déjà au bon grain. Ou utiliser
`COUNT(DISTINCT o.OrderId)` et `SUM` sur la colonne du bon niveau. Le premier remède est plus lisible
et se généralise ; le second dépanne.

**Le réflexe qui évite le piège : nommer le grain.** Avant d'écrire une jointure, énoncez une phrase :
*« après cette jointure, une ligne représente… »*. Une ligne de commande ? Un couple client-commande ?
Si la réponse ne correspond pas à ce que vous voulez agréger, c'est là qu'il faut corriger.

**`LEFT JOIN` et `WHERE` s'annulent facilement.** Placer une condition sur la table de droite dans
`WHERE` élimine les lignes complétées par des valeurs absentes, ce qui transforme silencieusement le
`LEFT JOIN` en `INNER JOIN`. Si la condition doit s'appliquer **avant** la conservation des lignes de
gauche, elle va dans le `ON` ; si elle doit filtrer le résultat final, elle va dans `WHERE`. C'est une
distinction que beaucoup de développeurs expérimentés ne savent pas énoncer.

Le cas particulier utile : `LEFT JOIN` suivi de `WHERE colonne_de_droite IS NULL` est le motif
canonique de l'anti-jointure — « les clients qui n'ont aucune commande ». C'est une exception
volontaire à la règle précédente, et elle se reconnaît immédiatement.

**`JOIN`, `EXISTS`, `IN` : trois outils, trois intentions.** Utilisez `JOIN` quand vous avez besoin
des **colonnes** de l'autre table. Utilisez `EXISTS` quand vous voulez seulement savoir s'il existe au
moins une correspondance : le moteur s'arrête au premier résultat trouvé, et surtout la jointure ne
multiplie pas les lignes. Utilisez `IN` sur une liste courte de valeurs connues — mais préférez
`NOT EXISTS` à `NOT IN` dès qu'une valeur absente est possible, pour la raison vue dans
`sql-select-filters-001`.

**Toujours qualifier les colonnes.** Avec deux tables portant chacune une colonne `Name`, une
projection non qualifiée échoue ou, pire, désigne l'autre que celle attendue lors d'une évolution du
schéma. Un alias court par table — `o`, `c`, `l` — et chaque colonne préfixée : c'est la convention qui
rend une requête relisible six mois plus tard.

## Exemple commenté

Le piège de la multiplication, puis sa correction :

```sql
-- FAUX : une ligne par ligne de commande, donc o.Total est compté autant de fois
-- que la commande a de lignes. Le chiffre d'affaires est surévalué, sans erreur.
SELECT   c.Name, SUM(o.Total) AS Revenue
FROM     dbo.Customers AS c
JOIN     dbo.Orders     AS o ON o.CustomerId = c.CustomerId
JOIN     dbo.OrderLines AS l ON l.OrderId    = o.OrderId
GROUP BY c.Name;

-- CORRECT : on agrège d'abord au grain « commande », puis on joint le résultat déjà agrégé.
-- Après la jointure, une ligne représente un couple client-commande, et jamais davantage.
WITH LineTotals AS
(
    SELECT   l.OrderId, SUM(l.Quantity * l.UnitPrice) AS LineTotal
    FROM     dbo.OrderLines AS l
    GROUP BY l.OrderId
)
SELECT   c.Name, SUM(t.LineTotal) AS Revenue
FROM     dbo.Customers AS c
JOIN     dbo.Orders    AS o ON o.CustomerId = c.CustomerId
JOIN     LineTotals    AS t ON t.OrderId    = o.OrderId
GROUP BY c.Name
ORDER BY Revenue DESC, c.Name;
```

L'anti-jointure, motif canonique à reconnaître :

```sql
-- Les clients sans aucune commande : on conserve la gauche, puis on ne garde
-- que les lignes où la droite est restée vide.
SELECT   c.CustomerId, c.Name
FROM     dbo.Customers AS c
LEFT JOIN dbo.Orders   AS o ON o.CustomerId = c.CustomerId
WHERE    o.OrderId IS NULL
ORDER BY c.CustomerId;
```

Et la différence entre `ON` et `WHERE` sur un `LEFT JOIN` :

```sql
-- Conserve tous les clients ; ceux sans commande payée ont un compte de zéro.
SELECT   c.Name, COUNT(o.OrderId) AS PaidOrders
FROM     dbo.Customers AS c
LEFT JOIN dbo.Orders   AS o ON o.CustomerId = c.CustomerId AND o.Status = 'Paid'
GROUP BY c.Name;

-- Le WHERE élimine les lignes complétées : le LEFT JOIN redevient un INNER JOIN.
SELECT   c.Name, COUNT(o.OrderId) AS PaidOrders
FROM     dbo.Customers AS c
LEFT JOIN dbo.Orders   AS o ON o.CustomerId = c.CustomerId
WHERE    o.Status = 'Paid'
GROUP BY c.Name;
```

## Contre-exemple et erreur fréquente

```sql
SELECT   c.Name,
         COUNT(*)     AS OrderCount,
         SUM(o.Total) AS Revenue
FROM     dbo.Customers AS c
LEFT JOIN dbo.Orders    AS o ON o.CustomerId = c.CustomerId
LEFT JOIN dbo.OrderLines AS l ON l.OrderId   = o.OrderId
WHERE    o.OrderDate >= '2026-01-01'
GROUP BY c.Name;
```

Trois erreurs se cumulent, et le résultat paraît plausible.

La jointure sur `OrderLines` multiplie les lignes. `SUM(o.Total)` compte chaque commande une fois par
ligne de commande : le chiffre d'affaires est surévalué d'un facteur égal au nombre moyen de lignes.
Et `l` n'est même pas utilisée dans la projection — la jointure est un reste de modification, ce qui
est le cas le plus fréquent en pratique.

`COUNT(*)` compte les lignes du résultat joint, pas les commandes. Il faudrait
`COUNT(DISTINCT o.OrderId)`.

Enfin, la condition sur `OrderDate` placée dans `WHERE` annule les deux `LEFT JOIN` : les clients sans
commande disparaissent, alors que l'intention de départ était visiblement de les conserver. Si l'on
voulait les garder avec un total nul, la condition devait aller dans le `ON`.

## Vérification de compréhension

Un client a deux commandes, l'une de trois lignes et l'autre d'une ligne. Combien de lignes produit
la jointure des trois tables pour ce client, et que vaut alors une somme naïve de son total ?

:::quiz
id=sql-joins-001-check
question=Vous ajoutez une jointure vers OrderLines à une requête qui sommait déjà Orders.Total par client. Que devient le résultat ?
option=Il est inchangé, la jointure supplémentaire n'ajoute que des colonnes
option=Il est surévalué : chaque commande est comptée autant de fois qu'elle a de lignes
option=Il est sous-évalué, car seules les commandes ayant des lignes sont conservées
correct=1
success=Correct : une jointure produit une ligne par couple correspondant. La somme porte alors sur des lignes dupliquées, sans qu'aucune erreur ne soit signalée.
retry=Relisez le passage sur la multiplication des lignes, et énoncez la phrase « après cette jointure, une ligne représente… ».
:::

## Exercice guidé

Ouvrez le scénario `sql-paid-customer-join-001` dans `/sql-lab`, puis procédez ainsi.

1. Avant d'écrire, énoncez la phrase « après cette jointure, une ligne représente… ».
2. Prédisez le nombre de lignes du résultat à partir des cardinalités du jeu de données.
3. Écrivez la requête en qualifiant chaque colonne, puis exécutez et comparez à votre prédiction.
4. Validez contre la référence, puis ajoutez volontairement une jointure vers `OrderLines` et observez
   l'effet sur le total.

Le scénario `sql-customers-without-orders-001` propose l'anti-jointure sur le même jeu de données.

## Exercice autonome

Écrivez la requête qui retourne, pour chaque produit, le nombre de commandes distinctes où il apparaît
et la quantité totale vendue — en incluant les produits jamais commandés, avec des valeurs à zéro.

Décidez avant d'écrire : le sens de la jointure, si la condition sur le statut va dans `ON` ou dans
`WHERE`, comment vous évitez la multiplication, et ce que vous retournez pour un produit sans vente.

## Débogage

Un ticket indique : « Le chiffre d'affaires du tableau de bord a doublé du jour au lendemain, sans
nouvelle commande. »

1. **Symptôme** : le total est un multiple du total attendu, les commandes n'ont pas changé.
2. **Hypothèse** : une jointure ajoutée récemment multiplie les lignes avant l'agrégation.
3. **Preuve** : exécutez la requête sans le `GROUP BY` et comparez son nombre de lignes au nombre de
   commandes. Un écart égal au nombre moyen de lignes par commande confirme l'hypothèse.
4. **Prévention** : agréger au bon grain avant de joindre, et ajouter un test qui compare le total
   global à la somme des totaux par client.

## Entretien

Question posée à voix haute : *quand utilisez-vous `EXISTS` plutôt qu'une jointure ?*

Une réponse solide part de l'intention : ai-je besoin des colonnes de l'autre table, ou seulement de
savoir qu'une correspondance existe ? Elle mentionne que `EXISTS` ne multiplie pas les lignes, qu'il
s'arrête au premier résultat, et que `NOT EXISTS` évite le piège de la valeur absente de `NOT IN`.

## Résumé

- Une jointure produit une ligne par couple correspondant : tout le reste en découle.
- Avant d'écrire, énoncez ce qu'une ligne représentera après la jointure.
- Agréger avant de joindre évite la multiplication des totaux.
- Une condition sur la table de droite placée dans `WHERE` annule le `LEFT JOIN`.
- `JOIN` pour les colonnes, `EXISTS` pour la présence, `NOT EXISTS` plutôt que `NOT IN`.

## Cartes de révision

Question : quel motif retourne « les lignes de gauche sans correspondance à droite » ? Réponse
attendue : un `LEFT JOIN` suivi d'un `WHERE` sur une colonne de droite `IS NULL`.

Question : pourquoi `COUNT(*)` est-il trompeur après une jointure vers une table de détail ? Réponse
attendue : il compte les lignes du résultat joint, pas les entités ; il faut compter en distinct sur
la clé du bon grain.

## Test de maîtrise

Sans relire, écrivez la requête qui retourne pour chaque ville le nombre de clients, le nombre de
commandes payées et le chiffre d'affaires correspondant, en conservant les villes sans commande.
Justifiez le placement de chaque condition entre `ON` et `WHERE`, et expliquez comment vous garantissez
que le chiffre d'affaires n'est pas multiplié.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
