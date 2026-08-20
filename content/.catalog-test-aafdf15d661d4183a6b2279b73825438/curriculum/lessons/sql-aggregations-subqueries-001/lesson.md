# Agrégations et sous-requêtes

## Objectif observable

À la fin de cette leçon, vous saurez nommer le grain d'un résultat agrégé avant de l'écrire, choisir
entre sous-requête corrélée, jointure agrégée et fonction de fenêtrage selon la question posée, et
prédire ce que devient une valeur absente dans chaque agrégat.

## Prérequis

- Avoir lu `sql-joins-001` et savoir prédire la cardinalité d'une jointure.
- Savoir écrire un `GROUP BY` simple sur le schéma du laboratoire.

## Intuition

Un `GROUP BY` répond toujours à la même question : *une ligne du résultat, ça représente quoi ?* La
réponse s'appelle le **grain**. Grouper par ville donne un grain « ville » ; grouper par ville et par
mois donne un grain « ville-mois ».

Presque toutes les erreurs d'agrégation viennent d'un grain non énoncé : on agrège sur un niveau et
on interprète le résultat sur un autre.

## Explication

**La règle du `GROUP BY`.** Toute colonne projetée doit être soit dans le `GROUP BY`, soit dans une
fonction d'agrégation. Ce n'est pas une lourdeur du langage : c'est la seule façon d'avoir une réponse
définie. Si vous groupez par ville et projetez le nom du client, quelle valeur le moteur devrait-il
choisir parmi les dix clients de la ville ? La question n'a pas de réponse, donc le moteur refuse.

**Les agrégats traitent les valeurs absentes différemment de ce qu'on croit.** `COUNT(*)` compte les
lignes, y compris celles pleines de valeurs absentes. `COUNT(colonne)` ne compte que les valeurs
renseignées. `SUM`, `AVG`, `MIN` et `MAX` **ignorent** les valeurs absentes — ce qui a une conséquence
souvent manquée : `AVG` divise par le nombre de valeurs présentes, pas par le nombre de lignes. Une
moyenne calculée sur une colonne à moitié vide n'est donc pas la moyenne qu'on croit.

Autre conséquence : `SUM` sur un ensemble vide retourne une valeur absente, pas zéro. Sur un `LEFT JOIN`
qui n'a rien trouvé, il faut donc un repli explicite pour afficher `0` plutôt qu'une case vide.

**Trois façons de répondre à « le total par client ».**

La *jointure agrégée* calcule d'abord au grain voulu, puis joint — c'est le motif vu dans
`sql-joins-001`. Lisible, efficace, et il généralise bien à plusieurs agrégats.

La *sous-requête corrélée* place un calcul dans la projection, réévalué pour chaque ligne externe.
Elle se lit très bien pour un agrégat unique, mais son coût croît avec le nombre de lignes externes.
Les moteurs modernes savent souvent la réécrire en jointure, mais pas toujours : c'est une écriture
à vérifier quand le volume grandit.

La *fonction de fenêtrage* — `SUM(...) OVER (PARTITION BY ...)` — calcule un agrégat **sans** réduire
le nombre de lignes. C'est ce qu'il faut quand on veut à la fois le détail et le total : afficher
chaque commande avec le chiffre d'affaires du client, ou calculer la part de chaque ligne dans son
total. Avec un `GROUP BY`, le détail disparaît ; avec une fenêtre, il reste.

**Le critère de choix.** Ai-je besoin de réduire les lignes ? `GROUP BY`. Ai-je besoin de garder le
détail **et** un agrégat ? Fenêtrage. Ai-je besoin d'un seul agrégat lisible dans la projection, sur
un volume modéré ? Sous-requête corrélée.

**`HAVING` filtre après regroupement.** Une condition de ligne mise dans `HAVING` fonctionne mais fait
regrouper des lignes qu'on jette ensuite — c'est le point vu dans `sql-select-filters-001`. Le réflexe :
si la condition peut s'écrire sans agrégat, elle appartient à `WHERE`.

**`COUNT(DISTINCT ...)` répond à une question différente.** Après une jointure vers une table de
détail, `COUNT(*)` compte les lignes jointes et `COUNT(DISTINCT o.OrderId)` compte les commandes.
Confondre les deux est la source la plus fréquente de tableaux de bord faux.

**Les sous-requêtes ont trois positions, trois sens.** Dans `WHERE` avec `IN` ou `EXISTS`, elles
filtrent. Dans `FROM`, elles produisent une source dérivée — c'est ce que fait une expression de table,
vue dans `sql-cte-transactions-001`. Dans `SELECT`, elles produisent une valeur par ligne. Une
sous-requête scalaire qui retournerait plusieurs lignes provoque une erreur d'exécution : c'est un
risque à considérer dès que sa clause de restriction n'est pas garantie unique.

## Exemple commenté

```sql
-- Grain énoncé avant d'écrire : une ligne = un client.
-- On agrège au grain « commande » dans la source dérivée, puis on remonte au grain « client ».
SELECT   c.CustomerId,
         c.Name,
         COUNT(o.OrderId)              AS OrderCount,   -- Ignore les absents : 0 si aucune commande.
         COALESCE(SUM(o.Total), 0)     AS Revenue,      -- SUM sur ensemble vide = absent, d'où le repli.
         MAX(o.OrderDate)              AS LastOrderDate -- Absent si le client n'a jamais commandé.
FROM     dbo.Customers AS c
LEFT JOIN dbo.Orders   AS o ON o.CustomerId = c.CustomerId AND o.Status = 'Paid'
GROUP BY c.CustomerId, c.Name
HAVING   COUNT(o.OrderId) > 0 OR c.IsActive = 1        -- Condition de groupe, volontairement.
ORDER BY Revenue DESC, c.CustomerId;
```

La condition sur le statut est dans le `ON` et non dans le `WHERE` : sans cela, le `LEFT JOIN`
redeviendrait un `INNER JOIN` et les clients sans commande payée disparaîtraient.

Le fenêtrage, quand on veut le détail **et** l'agrégat :

```sql
-- Une ligne par commande, enrichie du total de son client et de sa part dans ce total.
-- Le GROUP BY aurait supprimé le détail ; la fenêtre le conserve.
SELECT   o.OrderId,
         o.CustomerId,
         o.Total,
         SUM(o.Total) OVER (PARTITION BY o.CustomerId)                       AS CustomerRevenue,
         o.Total * 100.0 / SUM(o.Total) OVER (PARTITION BY o.CustomerId)     AS SharePercent
FROM     dbo.Orders AS o
WHERE    o.Status = 'Paid'
ORDER BY o.CustomerId, o.OrderId;
```

## Contre-exemple et erreur fréquente

```sql
SELECT   c.City,
         c.Name,                                     -- Ni groupée, ni agrégée : quelle valeur ?
         AVG(o.Total)             AS AverageOrder,
         COUNT(*)                 AS OrderCount
FROM     dbo.Customers AS c
JOIN     dbo.Orders     AS o ON o.CustomerId = c.CustomerId
JOIN     dbo.OrderLines AS l ON l.OrderId    = o.OrderId
GROUP BY c.City
HAVING   o.Status = 'Paid';                          -- Condition de ligne placée après regroupement.
```

Quatre défauts, dont trois silencieux si le moteur est permissif.

`c.Name` n'est ni groupée ni agrégée : la requête est ambiguë par construction. Certains moteurs la
rejettent, d'autres retournent une valeur arbitraire parmi le groupe — et personne ne sait laquelle.

La jointure vers `OrderLines` multiplie les lignes. `AVG(o.Total)` calcule donc la moyenne d'une
population dupliquée : les commandes à nombreuses lignes pèsent plus lourd. La moyenne obtenue n'a
aucun sens métier, et rien ne le signale.

`COUNT(*)` compte les lignes jointes, pas les commandes. Il faudrait `COUNT(DISTINCT o.OrderId)`.

Enfin, `o.Status = 'Paid'` est une condition de ligne : dans `HAVING`, elle porte sur une colonne
indisponible après regroupement — erreur franche — et de toute façon elle aurait dû réduire le volume
**avant** l'agrégation, dans `WHERE`.

## Vérification de compréhension

Énoncez le grain de la requête d'exemple, puis dites ce que retourne `AVG` sur une colonne dont la
moitié des valeurs sont absentes.

:::quiz
id=sql-aggregations-subqueries-001-check
question=Vous voulez afficher chaque commande avec le chiffre d'affaires total de son client, sans perdre le détail des commandes. Quel outil convient ?
option=Un GROUP BY sur le client, qui calcule le total et conserve les lignes de détail
option=Une fonction de fenêtrage avec PARTITION BY sur le client, qui calcule l'agrégat sans réduire les lignes
option=Un HAVING sur le total du client, appliqué après le regroupement
correct=1
success=Correct : le regroupement réduit les lignes, la fenêtre non. C'est précisément la situation où le fenêtrage est le bon outil.
retry=Relisez le critère de choix : la question est de savoir si vous devez réduire le nombre de lignes ou conserver le détail.
:::

## Exercice guidé

Ouvrez le scénario `sql-customer-revenue-001` dans `/sql-lab`, puis procédez ainsi.

1. Écrivez le grain attendu en une phrase avant toute requête.
2. Choisissez entre jointure agrégée, sous-requête corrélée et fenêtrage, et justifiez.
3. Prédisez le nombre de lignes, puis exécutez et comparez.
4. Validez contre la référence, puis réécrivez la même réponse avec une autre des trois approches.

Les scénarios `sql-customer-having-001` et `sql-orders-above-average-001` prolongent l'exercice sur le
filtrage de groupes et la sous-requête.

## Exercice autonome

Écrivez la requête qui retourne, pour chaque catégorie de produit, le chiffre d'affaires, le nombre de
commandes distinctes concernées, et le produit le plus vendu de la catégorie.

Décidez avant d'écrire : le grain, la façon d'obtenir le produit le plus vendu sans casser ce grain,
le traitement d'une égalité, et ce que vous retournez pour une catégorie sans vente.

## Débogage

Un ticket indique : « La moyenne du panier affichée est très supérieure à la réalité. »

1. **Symptôme** : la moyenne est biaisée vers le haut, le total global est correct.
2. **Hypothèse** : une jointure vers une table de détail duplique les commandes, et les commandes à
   nombreuses lignes pèsent plusieurs fois dans la moyenne.
3. **Preuve** : comparez `COUNT(*)` et `COUNT(DISTINCT OrderId)` sur la requête sans regroupement. Un
   écart confirme la duplication.
4. **Prévention** : agréger au grain « commande » avant de calculer la moyenne, et ajouter un test qui
   compare la moyenne obtenue au rapport entre total et nombre de commandes distinctes.

## Entretien

Question posée à voix haute : *quelle différence entre un `GROUP BY` et une fonction de fenêtrage ?*

Une réponse solide oppose la réduction du nombre de lignes à sa conservation, donne un cas d'usage de
chaque, et sait dire qu'une fenêtre permet d'obtenir simultanément le détail et un agrégat — ce qui
demanderait sinon deux requêtes ou une jointure supplémentaire.

## Résumé

- Énoncer le grain avant d'écrire évite la majorité des erreurs d'agrégation.
- Toute colonne projetée est groupée ou agrégée : sinon la réponse n'est pas définie.
- `SUM`, `AVG`, `MIN` et `MAX` ignorent les valeurs absentes ; `AVG` divise par les présentes.
- `SUM` sur un ensemble vide retourne une absence, pas zéro.
- `GROUP BY` réduit les lignes, le fenêtrage les conserve.

## Cartes de révision

Question : pourquoi `COUNT(*)` est-il trompeur après une jointure vers une table de détail ? Réponse
attendue : il compte les lignes jointes, pas les entités ; il faut compter en distinct sur la clé du
bon grain.

Question : que retourne `SUM` quand aucune ligne ne correspond ? Réponse attendue : une valeur
absente, qu'il faut remplacer explicitement par zéro si l'on veut l'afficher.

## Test de maîtrise

Sans relire, écrivez la requête qui retourne pour chaque ville le chiffre d'affaires, le nombre de
clients ayant commandé, et la part de la ville dans le chiffre d'affaires national. Énoncez le grain,
justifiez l'outil retenu pour la part nationale, et expliquez comment vous évitez qu'une jointure de
détail ne fausse le total.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
