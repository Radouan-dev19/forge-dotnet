# Index et plans sans coûts fragiles

## Objectif observable

À la fin de cette leçon, vous saurez concevoir un index à partir d'un motif de requête plutôt qu'au
hasard, lire un plan d'exécution en cherchant des propriétés stables plutôt que des chiffres, et
énoncer ce qu'un index coûte en écriture.

## Prérequis

- Avoir lu `sql-isolation-001` et savoir raisonner sur les effets de la concurrence.
- Savoir écrire un filtre sargable, notion vue dans `sql-select-filters-001`.

## Intuition

Un index est une structure ordonnée qui permet d'atteindre directement les lignes recherchées au lieu
de parcourir la table. C'est l'équivalent exact de l'index d'un livre : il n'existe que pour un
**motif de consultation** donné.

Il n'y a donc pas de « bon index » dans l'absolu. Il y a des index qui servent une requête précise, et
qui ralentissent toutes les écritures.

## Explication

**Recherche contre parcours.** Un `SEEK` navigue directement dans l'index jusqu'aux lignes voulues ;
son coût dépend du nombre de lignes retournées. Un `SCAN` lit la structure entière ; son coût dépend
de la taille de la table. Sur trois lignes, la différence est nulle ; sur dix millions, c'est la
différence entre une requête instantanée et une requête qui expire.

Un parcours n'est pas toujours un défaut : si la requête retourne la moitié de la table, le parcourir
est plus efficace que faire des millions de recherches. Le moteur choisit en fonction de la
**sélectivité** estimée, c'est-à-dire de la proportion de lignes attendue.

**L'ordre des colonnes décide de l'utilité.** Un index composite sur `(CustomerId, OrderDate)` sert un
filtre sur `CustomerId` seul, et un filtre sur `CustomerId` **et** `OrderDate`. Il ne sert pas un
filtre sur `OrderDate` seul, car la colonne de tête manque — comme un annuaire trié par nom puis
prénom ne sert à rien pour chercher un prénom.

La règle : mettez en tête la colonne utilisée avec une égalité, ensuite celle utilisée avec un
intervalle, puis celles servant au tri. Un index qui couvre aussi l'ordre demandé évite au moteur de
trier, ce qui est souvent le gain le plus visible.

**L'index couvrant supprime le second aller-retour.** Quand un index contient toutes les colonnes dont
la requête a besoin, le moteur n'a pas à retourner chercher la ligne dans la table. Les colonnes de
filtre et de tri vont dans la clé de l'index ; celles dont on a seulement besoin en projection vont
dans les colonnes incluses, qui n'alourdissent pas la structure de tri.

C'est directement lié à `SELECT *` : projeter toutes les colonnes rend presque impossible d'être
couvert, ce qui est une raison de plus de nommer les colonnes.

**Un index coûte à chaque écriture.** Chaque `INSERT`, `UPDATE` et `DELETE` doit maintenir tous les
index de la table. Cinq index sur une table écrite intensivement, c'est cinq structures à mettre à
jour à chaque ligne. L'index consomme aussi de l'espace et de la mémoire cache — de l'espace que les
données utiles n'occupent plus.

D'où l'arbitrage réel : un index se justifie par un motif de lecture **fréquent et mesuré**, pas par
l'intuition qu'il « pourrait servir ». Et un index qui n'est jamais utilisé est un coût pur : les
moteurs exposent des vues d'utilisation qui permettent de les repérer.

**Lire un plan par propriétés, pas par chiffres.** Le coût affiché est une estimation relative,
dépendante des statistiques, de la version et du matériel. S'en servir comme critère de non-régression
produit des tests instables qui échouent sans raison.

Ce qui est stable, ce sont les **propriétés** : l'opérateur est-il un `SEEK` ou un `SCAN` ? L'index
attendu est-il celui employé ? Y a-t-il un opérateur de tri qu'un index aurait pu éviter ? Le nombre
de lignes estimé est-il du même ordre que le nombre réel ? C'est ce dernier point qui révèle des
statistiques obsolètes — un écart de plusieurs ordres de grandeur explique la plupart des mauvais
plans.

**Les suggestions d'index sont des indices, pas des ordres.** Le moteur propose parfois un index pour
la requête analysée, sans considérer les autres requêtes ni le coût d'écriture. Appliquer ces
suggestions mécaniquement produit des tables avec quinze index redondants. Une suggestion se lit, se
compare aux index existants — souvent, il suffit d'ajouter une colonne incluse à un index déjà là — et
se mesure.

**Un index ne rattrape pas un prédicat non sargable.** `WHERE YEAR(OrderDate) = 2026` ne peut utiliser
aucun index sur `OrderDate`. Avant d'ajouter un index, vérifiez toujours que la requête est écrite de
façon à pouvoir en profiter : c'est gratuit, et c'est souvent tout ce qui manquait.

## Exemple commenté

Le motif de requête, puis l'index qui le sert :

```sql
-- Motif fréquent : filtre par client, intervalle de dates, tri par date, projection de trois colonnes.
SELECT   o.OrderId, o.OrderDate, o.Total
FROM     dbo.Orders AS o
WHERE    o.CustomerId = 2
  AND    o.OrderDate >= '2026-02-01'
  AND    o.OrderDate <  '2026-03-01'
ORDER BY o.OrderDate;
```

```sql
-- CustomerId en tête : c'est l'égalité. OrderDate ensuite : intervalle ET tri, donc pas de tri à faire.
-- Total en colonne incluse : nécessaire à la projection, inutile au tri de l'index.
CREATE INDEX IX_Orders_Customer_Date
    ON dbo.Orders (CustomerId, OrderDate)
    INCLUDE (Total);
```

Les propriétés attendues du plan, celles qu'un test de non-régression peut vérifier sans fragilité :

```text
- Opérateur       : Index Seek sur IX_Orders_Customer_Date   (et non Table Scan)
- Tri             : aucun opérateur Sort               (l'ordre vient de l'index)
- Aller-retour    : aucun Key Lookup                   (l'index est couvrant)
- Estimation      : lignes estimées du même ordre que les lignes réelles
```

Aucune de ces quatre propriétés ne dépend du matériel ni de la version : ce sont elles qu'on assert,
jamais un coût ni une durée.

## Contre-exemple et erreur fréquente

```sql
-- Un index par colonne, ajoutés au fil des tickets de lenteur.
CREATE INDEX IX_Orders_CustomerId ON dbo.Orders (CustomerId);
CREATE INDEX IX_Orders_OrderDate  ON dbo.Orders (OrderDate);
CREATE INDEX IX_Orders_Status     ON dbo.Orders (Status);
CREATE INDEX IX_Orders_Total      ON dbo.Orders (Total);

-- Et la requête censée en profiter :
SELECT   *
FROM     dbo.Orders AS o
WHERE    YEAR(o.OrderDate) = 2026
  AND    o.CustomerId = 2
ORDER BY o.Total DESC;
```

Rien ne fonctionne comme espéré, et pour quatre raisons distinctes.

`YEAR(OrderDate)` rend le prédicat non sargable : l'index sur `OrderDate` est inutilisable, quelle que
soit sa qualité.

Quatre index sur une colonne unique ne servent pas une requête à deux critères aussi bien qu'un seul
index composite. Le moteur en choisira un et devra vérifier le reste ligne par ligne.

`SELECT *` empêche tout index d'être couvrant : après la recherche, il faut retourner chercher chaque
ligne complète dans la table.

`IX_Orders_Status` sur une colonne à trois valeurs distinctes est très peu sélectif : le moteur
préférera presque toujours un parcours. Cet index ne servira jamais et sera pourtant maintenu à chaque
écriture.

Bilan : les écritures sont quatre fois plus coûteuses, l'espace occupé a augmenté, et la requête n'est
pas plus rapide. La correction commence par réécrire le prédicat — ce qui est gratuit — puis par
créer un seul index composite.

## Vérification de compréhension

Pour un filtre sur `Status` et un tri sur `OrderDate`, donnez l'ordre des colonnes de l'index et
justifiez-le.

:::quiz
id=sql-index-plans-001-check
question=Pourquoi ne pas vérifier une non-régression de performance en comparant le coût affiché dans le plan d'exécution ?
option=Parce que le coût n'est pas exposé par le moteur dans les plans d'exécution
option=Parce que c'est une estimation relative dépendant des statistiques, de la version et du matériel : le test serait instable
option=Parce que le coût ne change jamais, même lorsque le plan change complètement
correct=1
success=Correct : on assert des propriétés stables — recherche plutôt que parcours, index employé, absence de tri, ordre de grandeur de l'estimation — jamais un coût ou une durée.
retry=Relisez le passage sur la lecture d'un plan par propriétés, et la liste des quatre éléments qui restent stables.
:::

## Exercice guidé

Ouvrez le scénario `sql-covering-read-001` dans `/sql-lab`, puis procédez ainsi.

1. Écrivez le motif de requête — filtres, tri, projection — avant de concevoir l'index.
2. Déduisez l'ordre des colonnes de clé et la liste des colonnes incluses.
3. Exécutez la requête et notez les quatre propriétés attendues du plan.
4. Validez contre la référence, puis retirez une colonne incluse et observez l'apparition d'un
   aller-retour.

Le scénario `sql-composite-access-001` porte spécifiquement sur l'ordre des colonnes d'un index
composite.

## Exercice autonome

Une requête filtre les commandes par statut et par plage de dates, les trie par montant décroissant,
et projette l'identifiant, la date et le montant.

Concevez l'index. Justifiez l'ordre des colonnes de clé, le choix des colonnes incluses, ce que vous
attendez du plan, et estimez ce que cet index coûtera sur une table écrite mille fois par minute.

## Débogage

Un ticket indique : « La même requête est instantanée en recette et lente en production. »

1. **Symptôme** : la différence porte sur le volume, pas sur la requête.
2. **Hypothèse** : le plan diffère — parcours en production, recherche en recette — parce que la
   sélectivité estimée n'est pas la même, ou parce que l'index manque.
3. **Preuve** : comparez les deux plans sur les quatre propriétés stables. Vérifiez en particulier
   l'écart entre lignes estimées et lignes réelles : un facteur mille signale des statistiques
   obsolètes.
4. **Prévention** : mettre à jour les statistiques, créer l'index manquant, et ajouter un test qui
   assert l'usage d'un `SEEK` plutôt qu'une durée.

## Entretien

Question posée à voix haute : *comment décidez-vous d'ajouter un index ?*

Une réponse solide part d'un motif de requête mesuré comme fréquent et coûteux, vérifie d'abord que le
prédicat est sargable, examine les index existants avant d'en créer un nouveau, et cite explicitement
le coût en écriture et en espace. Elle mentionne aussi qu'un index inutilisé doit être supprimé.

## Résumé

- Un index sert un motif de requête précis ; il n'y en a pas de bon dans l'absolu.
- L'ordre des colonnes décide de l'utilité : égalité, puis intervalle, puis tri.
- Un index couvrant supprime l'aller-retour vers la table ; `SELECT *` l'en empêche.
- Chaque index est maintenu à chaque écriture, et occupe espace et cache.
- On assert des propriétés de plan, jamais un coût ni une durée.

## Cartes de révision

Question : pourquoi un index sur `(CustomerId, OrderDate)` ne sert-il pas un filtre sur `OrderDate`
seul ? Réponse attendue : la colonne de tête manque, comme un annuaire trié par nom pour chercher un
prénom.

Question : que vérifier en premier avant d'ajouter un index pour accélérer une requête ? Réponse
attendue : que le prédicat soit sargable, car aucune fonction appliquée à la colonne ne permet
d'utiliser un index.

## Test de maîtrise

Sans relire, concevez l'index servant une requête qui filtre par ville et par statut, trie par date
décroissante et projette quatre colonnes. Justifiez l'ordre des colonnes, distinguez clé et colonnes
incluses, listez les propriétés de plan que vous vérifieriez, et estimez le coût en écriture.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
