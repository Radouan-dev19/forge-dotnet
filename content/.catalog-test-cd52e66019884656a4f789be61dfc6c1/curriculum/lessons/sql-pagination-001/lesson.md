# Pagination stable et bornée

## Objectif observable

À la fin de cette leçon, vous saurez expliquer pourquoi une pagination par décalage produit des
doublons et des oublis, écrire une pagination par clé de continuation, et choisir entre les deux
selon l'interface à servir.

## Prérequis

- Avoir lu `sql-index-plans-001` et savoir concevoir un index à partir d'un motif de requête.
- Savoir écrire un `ORDER BY` total, notion vue dans `sql-select-filters-001`.

## Intuition

Paginer, c'est répondre à *« donne-moi la suite »*. Deux façons de poser la question, très
différentes.

Par **décalage** : « saute les 40 premières lignes et donne-m'en 20 ». Simple, permet d'aller
directement à la page 7, et devient à la fois lente et fausse à mesure qu'on avance.

Par **clé de continuation** : « donne-moi les 20 lignes qui suivent celle-ci ». Toujours rapide,
toujours cohérente, mais interdit le saut direct à une page arbitraire.

## Explication

**Sans ordre total, il n'y a pas de pagination.** C'est la condition préalable, et elle est
constamment oubliée. Trier par montant décroissant ne suffit pas si deux commandes ont le même
montant : leur ordre relatif n'est pas garanti, il peut différer entre deux exécutions, et la même
ligne apparaît alors sur deux pages pendant qu'une autre n'apparaît jamais.

Le remède est mécanique : ajouter au tri une colonne unique — la clé primaire — comme dernier critère.
`ORDER BY Total DESC, OrderId` est total ; `ORDER BY Total DESC` ne l'est pas.

**Le décalage dérive quand les données bougent.** Entre le chargement de la page 3 et celui de la
page 4, une insertion décale tout : une ligne déjà vue réapparaît. Une suppression fait l'inverse :
une ligne n'est jamais montrée. L'utilisateur ne voit pas d'erreur, il voit un doublon — ou rien du
tout, ce qui est pire car invisible.

Ce n'est pas un défaut d'implémentation : c'est inhérent au fait de compter des positions dans un
ensemble qui change.

**Le décalage coûte de plus en plus cher.** `OFFSET 100000 FETCH NEXT 20` oblige le moteur à produire
puis jeter cent mille lignes avant d'en retourner vingt. La page 1 est instantanée, la page 5 000 est
inutilisable. Le coût croît linéairement avec le numéro de page, ce qui est exactement le contraire de
ce que l'utilisateur attend.

**La clé de continuation compare, elle ne compte pas.** Au lieu de sauter des lignes, on demande
celles qui suivent la dernière vue : `WHERE (Total, OrderId) < (@lastTotal, @lastOrderId)` selon le
sens du tri. Le moteur va directement au bon endroit de l'index — c'est un `SEEK` — et le coût est le
même pour la première page que pour la dix millième.

Le prédicat doit porter sur **exactement** les colonnes du tri, dans le même ordre. En SQL Server,
la forme lisible s'écrit avec un `OR` :

```text
WHERE Total < @lastTotal
   OR (Total = @lastTotal AND OrderId < @lastOrderId)
```

C'est la traduction fidèle de « strictement après la dernière ligne vue », et elle est directement
servie par l'index composite correspondant.

**Le compromis se choisit selon l'interface.** Un tableau d'administration avec numéros de page et
saut direct impose le décalage : l'utilisateur veut aller page 12. Un défilement infini, un export ou
une synchronisation d'API n'ont besoin que de « la suite » : la clé de continuation est meilleure sur
tous les plans.

En pratique, le décalage reste acceptable si l'on borne le nombre de pages atteignables — les moteurs
de recherche du web ne laissent pas aller au-delà de quelques dizaines de pages, précisément pour
cette raison.

**Le comptage total est souvent le vrai coût.** Afficher « page 3 sur 4 271 » exige un `COUNT(*)` sur
l'ensemble filtré, qui peut coûter plus cher que la page elle-même. Trois options : ne pas afficher le
total, l'estimer, ou le calculer séparément avec une mise en cache courte. La question à poser au
métier : ce nombre est-il vraiment utile à l'utilisateur ?

**Bornez toujours la taille de page.** Une taille reçue du client sans borne haute permet à un appelant
de demander un million de lignes et d'épuiser la mémoire du serveur. Une valeur par défaut et un
maximum imposé côté serveur sont non négociables.

## Exemple commenté

Pagination par décalage, correcte dans les limites de ce qu'elle permet :

```sql
-- L'ordre est TOTAL : OrderId départage les montants égaux, sinon une ligne
-- peut apparaître sur deux pages successives.
SELECT   o.OrderId, o.OrderDate, o.Total
FROM     dbo.Orders AS o
WHERE    o.Status = 'Paid'
ORDER BY o.Total DESC, o.OrderId DESC
OFFSET   @skip ROWS FETCH NEXT @take ROWS ONLY;   -- @take borné côté serveur.
```

Pagination par clé de continuation, servie par un `SEEK` quel que soit le rang :

```sql
-- @lastTotal et @lastOrderId proviennent de la DERNIÈRE ligne de la page précédente.
-- Pour la première page, les deux paramètres sont absents et le prédicat est omis.
SELECT   TOP (@take) o.OrderId, o.OrderDate, o.Total
FROM     dbo.Orders AS o
WHERE    o.Status = 'Paid'
  AND    (o.Total < @lastTotal
      OR (o.Total = @lastTotal AND o.OrderId < @lastOrderId))
ORDER BY o.Total DESC, o.OrderId DESC;
```

L'index qui rend cette requête instantanée découle directement du motif, comme vu dans
`sql-index-plans-001` :

```sql
CREATE INDEX IX_Orders_Status_Total_Id
    ON dbo.Orders (Status, Total DESC, OrderId DESC)
    INCLUDE (OrderDate);
```

`Status` en tête car c'est l'égalité, puis les deux colonnes de tri dans le même ordre et le même
sens que la clause `ORDER BY`, et `OrderDate` en colonne incluse pour rendre l'index couvrant.

## Contre-exemple et erreur fréquente

```sql
-- Taille de page reçue telle quelle, ordre non total, décalage profond.
SELECT   *
FROM     dbo.Orders AS o
ORDER BY o.Total DESC
OFFSET   @skip ROWS FETCH NEXT @take ROWS ONLY;
```

Quatre défauts, dont trois invisibles en recette.

L'ordre n'est pas total : deux commandes de même montant peuvent s'échanger entre deux requêtes.
L'utilisateur voit la même ligne sur deux pages, et une autre disparaît sans que personne ne s'en
aperçoive.

`@take` n'est pas borné : un appelant qui demande un million de lignes obtient un million de lignes,
ou fait tomber le serveur. C'est aussi un vecteur de déni de service trivial sur une API publique.

`OFFSET @skip` sur une valeur élevée fait produire puis jeter des centaines de milliers de lignes. Le
temps de réponse croît avec le numéro de page.

`SELECT *` empêche l'index d'être couvrant et transporte des colonnes inutiles.

En recette, sur trois cents commandes, tout paraît correct. En production, les quatre défauts se
manifestent en même temps, et le premier symptôme rapporté sera le doublon — le moins grave des
quatre.

## Vérification de compréhension

Expliquez pourquoi `ORDER BY Total DESC` seul rend une pagination incorrecte, même si les données ne
changent pas entre deux pages.

:::quiz
id=sql-pagination-001-check
question=Pourquoi une pagination par décalage devient-elle de plus en plus lente à mesure qu'on avance dans les pages ?
option=Parce que le moteur relit l'index depuis le début et jette les lignes sautées avant de retourner la page demandée
option=Parce que la taille des pages augmente automatiquement avec leur numéro
option=Parce que le tri est recalculé une fois de plus pour chaque page atteinte
correct=0
success=Correct : le décalage compte des positions, donc il faut produire toutes les lignes précédentes pour les écarter. Une clé de continuation compare au lieu de compter, et va directement au bon endroit de l'index.
retry=Relisez le passage sur le coût du décalage : la question est de savoir ce que le moteur doit faire des lignes situées avant la page demandée.
:::

## Exercice guidé

Ouvrez le scénario `sql-keyset-total-001` dans `/sql-lab`, puis procédez ainsi.

1. Écrivez l'ordre total retenu, en justifiant la colonne de départage.
2. Écrivez d'abord la version par décalage et notez le nombre de lignes produites puis jetées.
3. Réécrivez en clé de continuation, avec un prédicat portant exactement sur les colonnes du tri.
4. Validez contre la référence, puis comparez les plans des deux versions.

Le scénario `sql-row-number-page-001` propose la même pagination par numérotation de lignes.

## Exercice autonome

Concevez la pagination d'une API listant les commandes d'un client, triées par date décroissante.

Décidez avant d'écrire : l'ordre total, la stratégie retenue et pourquoi, la forme du jeton de
continuation exposé au client, la taille de page par défaut et maximale, et ce que vous répondez si le
client envoie un jeton corrompu ou périmé.

## Débogage

Un ticket indique : « Certaines commandes apparaissent deux fois dans l'export, d'autres manquent. »

1. **Symptôme** : doublons et oublis simultanés, sans erreur, sur un export multi-pages.
2. **Hypothèse** : l'ordre n'est pas total, ou des insertions surviennent pendant la pagination par
   décalage.
3. **Preuve** : comparez le nombre total de lignes exportées au `COUNT(*)` de la source, et cherchez
   des valeurs de tri identiques parmi les doublons constatés.
4. **Prévention** : rendre l'ordre total en ajoutant la clé primaire, puis basculer l'export sur une
   clé de continuation — un export n'a jamais besoin de saut direct.

## Entretien

Question posée à voix haute : *comment pagineriez-vous une API listant des millions d'enregistrements ?*

Une réponse solide écarte le décalage en expliquant ses deux défauts — coût croissant et incohérence
sous écriture concurrente — propose une clé de continuation avec un ordre total, décrit le jeton
exposé au client, et n'oublie ni la borne de taille de page ni la question du comptage total.

## Résumé

- Sans ordre total, la pagination produit doublons et oublis, même sans modification des données.
- Le décalage compte des positions : son coût croît avec le numéro de page.
- La clé de continuation compare : coût constant, servi par un `SEEK`.
- Le décalage reste légitime pour un saut direct à une page, s'il est borné.
- La taille de page se borne côté serveur, toujours.

## Cartes de révision

Question : que doit contenir le prédicat d'une pagination par clé de continuation ? Réponse attendue :
exactement les colonnes du tri, dans le même ordre et le même sens, comparées à la dernière ligne vue.

Question : pourquoi afficher « page 3 sur 4 271 » peut-il coûter plus cher que la page elle-même ?
Réponse attendue : le total exige un comptage sur l'ensemble filtré, indépendant de la page demandée.

## Test de maîtrise

Sans relire, écrivez la pagination par clé de continuation d'une liste triée par date décroissante puis
identifiant, avec l'index correspondant. Justifiez l'ordre total, la forme du prédicat, la borne de
taille de page, et expliquez ce que vous exposez au client comme jeton de continuation.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
