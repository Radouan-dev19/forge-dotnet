# SELECT et filtres déterministes

## Objectif observable

À la fin de cette leçon, vous saurez énoncer l'ordre logique d'évaluation d'une requête et vous en
servir pour expliquer une erreur d'alias ou de `WHERE`, et vous saurez écrire un filtre qui reste
exploitable par un index.

## Prérequis

- Avoir lu `sql-relational-constraints-001` et savoir prédire le comportement d'une comparaison sur
  une valeur absente.
- Savoir exécuter une requête dans `/sql-lab` sur le schéma du laboratoire.

## Intuition

Une requête ne s'exécute pas dans l'ordre où elle s'écrit. On écrit `SELECT` en premier, mais le
moteur l'évalue presque en dernier. Comprendre cet ordre logique explique d'un coup une série
d'erreurs qui paraissent arbitraires : pourquoi un alias n'est pas utilisable dans `WHERE`, pourquoi
`HAVING` existe alors que `WHERE` semblait suffire, pourquoi `ORDER BY` peut trier sur une colonne
absente du résultat.

## Explication

**L'ordre logique d'évaluation.** `FROM` d'abord — les sources et leurs jointures. Puis `WHERE`, qui
filtre les lignes individuelles. Puis `GROUP BY`, qui les regroupe. Puis `HAVING`, qui filtre les
**groupes**. Puis `SELECT`, qui projette et où naissent les alias. Puis `DISTINCT`. Puis `ORDER BY`.
Enfin `OFFSET` et `FETCH`.

Tout se déduit de cette liste. Un alias défini dans `SELECT` n'existe pas encore quand `WHERE`
s'évalue : c'est pour cela que `WHERE total_ht > 100` échoue si `total_ht` est un alias de la
projection. À l'inverse, `ORDER BY` s'évaluant après `SELECT`, il peut utiliser les alias — et même
trier sur une colonne qui n'apparaît pas dans le résultat, puisque les lignes sources sont encore
disponibles.

**`WHERE` filtre des lignes, `HAVING` filtre des groupes.** Les deux clauses semblent redondantes
jusqu'à ce qu'on les situe dans l'ordre. Restreindre aux commandes payées est une condition de ligne :
elle va dans `WHERE`, et elle réduit le volume **avant** le regroupement. Ne garder que les clients
ayant plus de trois commandes est une condition de groupe : elle ne peut s'exprimer qu'après
`GROUP BY`, donc dans `HAVING`.

La conséquence pratique est aussi une question de coût : mettre dans `HAVING` une condition qui aurait
pu aller dans `WHERE` fait regrouper des lignes qu'on jette ensuite.

**`SELECT *` n'a pas sa place dans du code.** Il transporte des colonnes inutiles sur le réseau,
empêche un index couvrant de suffire — point développé dans `sql-index-plans-001` — et surtout rend le
code fragile : l'ajout d'une colonne en base change silencieusement la forme du résultat. En
exploration interactive, il est parfait ; dans une requête versionnée, on nomme les colonnes.

**L'ordre fait partie du résultat, ou n'existe pas.** Sans `ORDER BY`, aucun ordre n'est garanti,
même si le moteur en retourne un stable pendant des mois. Un changement de plan, un index ajouté, une
exécution parallèle suffisent à le modifier. Si l'ordre compte pour l'appelant, il s'écrit — c'est
d'autant plus vrai avec la pagination, où un ordre non total produit des doublons entre pages.

**La sargabilité décide de l'usage des index.** Un prédicat est *sargable* — utilisable par une
recherche d'index — quand la colonne apparaît **seule** d'un côté de la comparaison. Dès qu'on lui
applique une fonction, le moteur doit calculer cette fonction sur chaque ligne, donc parcourir toute
la table.

`WHERE YEAR(OrderDate) = 2026` n'est pas sargable. La forme équivalente
`WHERE OrderDate >= '2026-01-01' AND OrderDate < '2027-01-01'` l'est : elle exprime la même chose par
un intervalle sur la colonne brute. C'est la réécriture la plus rentable du métier, et elle est
purement mécanique.

Le même piège existe avec `WHERE UPPER(Name) = 'ADA'` — préférer un collationnement insensible à la
casse — et avec `WHERE Name LIKE '%son'`, dont le joker initial interdit toute recherche indexée. Un
joker final, lui, reste exploitable.

**Les valeurs littérales de date se déterminent.** Écrire `'2026-03-14'` en format ISO est
interprétable sans ambiguïté ; `'14/03/2026'` dépend des réglages de session. C'est le même
raisonnement que dans `strings-dates-001` : ce qui n'est pas explicite dépend de la machine.

**`IN`, `EXISTS` et les valeurs absentes.** `NOT IN` avec une sous-requête qui contient un `NULL` ne
retourne **jamais** de ligne — conséquence directe de la logique ternaire. C'est un piège classique et
silencieux ; `NOT EXISTS` n'a pas ce comportement et doit être préféré dès qu'une valeur absente est
possible.

## Exemple commenté

```sql
-- Ordre d'évaluation : FROM, puis WHERE (lignes), puis GROUP BY, puis HAVING (groupes),
-- puis SELECT (les alias naissent ici), puis ORDER BY (qui peut donc les utiliser).
SELECT   c.City,
         COUNT(*)      AS OrderCount,
         SUM(o.Total)  AS Revenue
FROM     dbo.Orders    AS o
JOIN     dbo.Customers AS c ON c.CustomerId = o.CustomerId
WHERE    o.Status = 'Paid'                    -- Condition de LIGNE : réduit avant le regroupement.
  AND    o.OrderDate >= '2026-01-01'          -- Sargable : la colonne est seule à gauche.
  AND    o.OrderDate <  '2027-01-01'
GROUP BY c.City
HAVING   COUNT(*) > 2                         -- Condition de GROUPE : impossible dans WHERE.
ORDER BY Revenue DESC, c.City;                -- Alias utilisable ici, et ordre total garanti.
```

Deux détails valent d'être notés. La borne haute est **stricte** et porte sur le 1ᵉʳ janvier suivant :
cela couvre correctement les valeurs comportant une heure, ce que `<= '2026-12-31'` manquerait. Et
`ORDER BY` ajoute `c.City` en second critère, ce qui rend l'ordre **total** : sans lui, deux villes de
même chiffre d'affaires pourraient s'échanger d'une exécution à l'autre.

La réécriture sargable, côte à côte :

```sql
-- Non sargable : la fonction empêche toute recherche indexée, la table entière est parcourue.
SELECT OrderId FROM dbo.Orders WHERE YEAR(OrderDate) = 2026;

-- Sargable : le même ensemble de lignes, exprimé par un intervalle sur la colonne brute.
SELECT OrderId FROM dbo.Orders
WHERE  OrderDate >= '2026-01-01' AND OrderDate < '2027-01-01';
```

## Contre-exemple et erreur fréquente

```sql
SELECT   *,
         o.Total * 1.2 AS TotalTtc
FROM     dbo.Orders AS o
WHERE    TotalTtc > 100                       -- L'alias n'existe pas encore à cette étape.
  AND    YEAR(o.OrderDate) = 2026             -- Non sargable.
  AND    o.CustomerId NOT IN (SELECT CustomerId FROM dbo.Customers WHERE IsActive = 0)
ORDER BY 3;                                   -- Tri par position de colonne.
```

Quatre défauts, dont deux silencieux.

La référence à `TotalTtc` dans `WHERE` provoque une erreur franche : l'alias naît dans `SELECT`, qui
s'évalue après. C'est le défaut le moins grave, précisément parce qu'il ne compile pas.

`YEAR(OrderDate)` force un parcours complet. Sur mille lignes, invisible ; sur dix millions, c'est la
requête qui expire.

`NOT IN` sur une sous-requête pouvant contenir une valeur absente ne retourne **rien du tout**, sans
la moindre erreur. Si un seul client a un `CustomerId` nul, le résultat est vide et personne ne
comprend pourquoi. `NOT EXISTS` aurait produit le résultat attendu.

`ORDER BY 3` trie par position : insérer une colonne dans `SELECT` change silencieusement le tri. Et
combiné à `SELECT *`, la position n'est même pas stable dans le temps.

## Vérification de compréhension

Expliquez pourquoi un alias défini dans `SELECT` est utilisable dans `ORDER BY` mais pas dans `WHERE`.

:::quiz
id=sql-select-filters-001-check
question=Pourquoi préférer `WHERE OrderDate >= '2026-01-01' AND OrderDate < '2027-01-01'` à `WHERE YEAR(OrderDate) = 2026` ?
option=Parce que la fonction YEAR est dépréciée dans les versions récentes du moteur
option=Parce qu'appliquer une fonction à la colonne rend le prédicat non sargable : le moteur doit l'évaluer sur chaque ligne au lieu d'utiliser un index
option=Parce que les deux formes ne retournent pas le même ensemble de lignes
correct=1
success=Correct : les deux formes retournent le même ensemble, mais seule la seconde laisse la colonne seule d'un côté de la comparaison, donc utilisable par une recherche d'index.
retry=Relisez le passage sur la sargabilité : ce qui compte est la présence ou l'absence d'une fonction appliquée à la colonne filtrée.
:::

## Exercice guidé

Ouvrez le scénario `sql-orders-date-range-001` dans `/sql-lab`, puis procédez ainsi.

1. Écrivez l'ordre logique d'évaluation de votre requête avant de la taper.
2. Exprimez le filtre de date sous forme d'intervalle sargable, avec une borne haute stricte.
3. Ajoutez un `ORDER BY` total, puis prédisez le nombre de lignes attendu.
4. Exécutez, validez contre la référence, puis réinitialisez la session.

## Exercice autonome

Écrivez la requête qui retourne, pour chaque catégorie de produit, le nombre de produits en stock et
le prix moyen, en ne gardant que les catégories comptant au moins deux produits disponibles.

Décidez avant d'écrire : quelles conditions vont dans `WHERE` et lesquelles dans `HAVING`, comment
vous traitez un prix absent, quelles colonnes vous projetez, et ce qui rend votre ordre total.

## Débogage

Un ticket indique : « Le filtre "clients non désactivés" ne retourne aucune ligne depuis l'import de
mardi. »

1. **Symptôme** : résultat vide, aucune erreur, alors que la requête n'a pas changé.
2. **Hypothèse** : un `NOT IN` porte sur une sous-requête qui contient désormais une valeur absente.
3. **Preuve** : exécutez la sous-requête seule et comparez `COUNT(*)` à `COUNT(Colonne)`. Un écart
   confirme la présence de valeurs absentes introduites par l'import.
4. **Prévention** : remplacez `NOT IN` par `NOT EXISTS`, et ajoutez la contrainte `NOT NULL` qui aurait
   empêché l'import fautif.

## Entretien

Question posée à voix haute : *quelle est la différence entre `WHERE` et `HAVING` ?*

Une réponse solide situe les deux clauses dans l'ordre logique d'évaluation plutôt que de réciter une
définition, donne un exemple de condition qui ne peut aller que dans l'une, et mentionne l'effet sur le
volume traité lorsqu'une condition de ligne est placée par erreur dans `HAVING`.

## Résumé

- L'ordre d'écriture n'est pas l'ordre d'évaluation ; tout le reste s'en déduit.
- Les alias naissent dans `SELECT` : indisponibles dans `WHERE`, disponibles dans `ORDER BY`.
- `WHERE` filtre des lignes, `HAVING` filtre des groupes.
- Une fonction appliquée à la colonne filtrée interdit l'usage d'un index.
- Sans `ORDER BY` total, l'ordre n'est pas garanti — et la pagination devient fausse.

## Cartes de révision

Question : pourquoi `NOT IN` sur une sous-requête contenant une valeur absente ne retourne-t-il rien ?
Réponse attendue : la comparaison à un inconnu produit un inconnu, que `WHERE` ne conserve pas ;
`NOT EXISTS` n'a pas ce comportement.

Question : que signifie qu'un prédicat est sargable ? Réponse attendue : la colonne apparaît seule
d'un côté de la comparaison, donc une recherche d'index reste possible.

## Test de maîtrise

Sans relire, écrivez la requête qui retourne les clients actifs ayant passé au moins une commande en
2026, avec leur nombre de commandes, triées du plus grand au plus petit. Justifiez la répartition
entre `WHERE` et `HAVING`, la forme sargable du filtre de date, et ce qui rend votre ordre total.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
