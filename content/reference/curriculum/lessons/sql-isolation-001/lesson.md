# Isolation et anomalies observables

## Objectif observable

À la fin de cette leçon, vous saurez reconnaître les quatre anomalies de concurrence à leur symptôme,
choisir un niveau d'isolation en nommant l'anomalie que vous acceptez, et dire précisément ce que
`NOLOCK` échange.

## Prérequis

- Avoir lu `sql-cte-transactions-001` et savoir délimiter une transaction.
- Savoir ouvrir deux sessions distinctes sur la même base.

## Intuition

Seul, un programme voit toujours des données cohérentes. À plusieurs, les lectures d'une session
s'entrelacent avec les écritures d'une autre, et certains entrelacements produisent des résultats
qu'aucune exécution séquentielle n'aurait pu donner.

Le niveau d'isolation n'est pas un réglage de performance : c'est la **liste des anomalies que vous
acceptez**. Le choisir, c'est décider ce que votre métier tolère.

## Explication

**Quatre anomalies, quatre symptômes distincts.**

La *lecture sale* : une session lit une valeur écrite par une transaction non encore validée. Si
celle-ci est annulée, la première a travaillé sur une donnée qui n'a jamais existé.

La *lecture non répétable* : une session lit la même ligne deux fois dans la même transaction et
obtient deux valeurs différentes, parce qu'une autre l'a modifiée entre-temps. Symptôme typique : un
total affiché en en-tête qui ne correspond pas au détail affiché juste en dessous.

Le *fantôme* : une session exécute deux fois la même requête d'ensemble et obtient un nombre de lignes
différent, parce qu'une autre a inséré une ligne satisfaisant le critère. La différence avec la
précédente est essentielle — il ne s'agit pas d'une ligne modifiée mais d'une ligne **apparue**.

La *mise à jour perdue* : deux sessions lisent la même valeur, calculent chacune un nouveau résultat
et écrivent. La seconde écrase la première, dont la modification disparaît sans trace. C'est le cas du
contre-exemple de `sql-cte-transactions-001`.

**Les niveaux, du plus permissif au plus strict.** `READ UNCOMMITTED` autorise tout, y compris la
lecture sale. `READ COMMITTED` — le défaut sur SQL Server — interdit la lecture sale mais autorise
lecture non répétable et fantômes. `REPEATABLE READ` ajoute la garantie qu'une ligne relue n'a pas
changé, mais laisse passer les fantômes. `SERIALIZABLE` interdit les quatre, au prix de verrous
d'intervalle et donc d'une contention forte.

`SNAPSHOT` occupe une place à part et mérite d'être connu : au lieu de verrouiller, il donne à chaque
transaction une vue figée de la base à son instant de départ, en conservant les versions antérieures
des lignes. Les lecteurs ne bloquent plus les écrivains ni l'inverse. Le prix se déplace : espace de
version consommé, et conflits détectés à la validation plutôt qu'évités par attente.

**La règle de choix.** Partez du défaut, `READ COMMITTED`, et ne montez que lorsque vous pouvez nommer
l'anomalie qui vous gêne et le cas métier concerné. Un rapport financier qui doit être cohérent d'un
bout à l'autre justifie `SNAPSHOT` ou `REPEATABLE READ` ; l'affichage d'une liste de produits n'en a
aucun besoin.

Monter le niveau « pour être sûr » est un mauvais réflexe : `SERIALIZABLE` sur un chemin fréquent
produit des interblocages, et un interblocage est plus difficile à diagnostiquer qu'une lecture non
répétable.

**Ce que `NOLOCK` échange réellement.** L'indicateur `WITH (NOLOCK)` équivaut à `READ UNCOMMITTED` sur
la table concernée. Il est souvent ajouté pour « accélérer un rapport », et ce qu'on achète est mal
compris : au-delà des lectures sales, un parcours sans verrou peut **manquer des lignes existantes**
ou en **lire deux fois**, si une réorganisation de pages survient pendant la lecture. Le total d'un
rapport peut donc être faux sans qu'aucune transaction n'ait été annulée.

Il ne s'agit donc pas d'un compromis « données légèrement périmées contre vitesse », mais de
« résultats potentiellement faux contre absence d'attente ». Sur un rapport financier, ce n'est pas
acceptable ; `SNAPSHOT` obtient l'absence d'attente sans ce défaut.

**L'alternative applicative : la concurrence optimiste.** Plutôt que de tenir des verrous, on lit une
valeur de version en même temps que la donnée, et on la vérifie à l'écriture. Si elle a changé,
l'écriture est refusée et l'appelant décide. La colonne `DataVersion` de la table `Orders` du
laboratoire existe pour cela, et c'est ce mécanisme qu'`ef-core-data-access-001` reprend côté .NET.

C'est souvent la bonne réponse : elle place la décision de conflit dans le métier, là où elle a du
sens, au lieu de la laisser au moteur.

## Exemple commenté

La mise à jour perdue, observée sur deux sessions du laboratoire :

```sql
-- Session A                                  -- Session B
BEGIN TRANSACTION;                            BEGIN TRANSACTION;
SELECT Stock FROM dbo.Products                SELECT Stock FROM dbo.Products
WHERE ProductId = 4;   -- lit 5               WHERE ProductId = 4;   -- lit 5 également

UPDATE dbo.Products SET Stock = 5 - 2         UPDATE dbo.Products SET Stock = 5 - 3
WHERE ProductId = 4;   -- écrit 3             WHERE ProductId = 4;   -- écrit 2
COMMIT;                                       COMMIT;
-- Résultat final : 2. Cinq unités ont été réservées, deux seulement ont été déduites.
```

Aucune erreur n'est levée. Le stock est simplement faux.

Deux corrections, selon l'endroit où l'on veut placer la décision :

```sql
-- 1. Écriture relative avec garde : le moteur calcule à partir de la valeur courante.
--    @@ROWCOUNT à zéro signale que la garde a joué, sans verrou explicite.
UPDATE dbo.Products
SET    Stock = Stock - 2
WHERE  ProductId = 4 AND Stock >= 2;

-- 2. Concurrence optimiste : la version lue doit être encore en place à l'écriture.
UPDATE dbo.Orders
SET    Status = 'Paid', DataVersion = DataVersion + 1
WHERE  OrderId = 5 AND DataVersion = 3;   -- 3 est la version lue plus tôt.
```

La différence entre lecture non répétable et fantôme, sur le même jeu :

```sql
-- Session A, en READ COMMITTED
BEGIN TRANSACTION;
SELECT COUNT(*) FROM dbo.Orders WHERE Status = 'Open';   -- 2

-- Session B insère une commande ouverte et valide.

SELECT COUNT(*) FROM dbo.Orders WHERE Status = 'Open';   -- 3 : un FANTÔME est apparu.
COMMIT;
```

En `REPEATABLE READ`, une ligne déjà lue n'aurait pas pu changer, mais ce fantôme serait quand même
apparu : seul `SERIALIZABLE` ou `SNAPSHOT` l'empêche.

## Contre-exemple et erreur fréquente

```sql
-- « Le rapport était lent, on a ajouté NOLOCK partout et c'est réglé. »
SELECT   c.Name, SUM(o.Total) AS Revenue
FROM     dbo.Customers AS c WITH (NOLOCK)
JOIN     dbo.Orders     AS o WITH (NOLOCK) ON o.CustomerId = c.CustomerId
WHERE    o.Status = 'Paid'
GROUP BY c.Name;
```

Le rapport est effectivement plus rapide, et il est désormais potentiellement faux — d'une manière
qui ne se voit pas.

Il peut inclure des commandes issues de transactions qui seront annulées : du chiffre d'affaires qui
n'existera jamais. Il peut aussi, à cause d'une réorganisation de pages pendant le parcours, **omettre
des lignes existantes** ou en **compter deux fois**. Aucun message n'est émis, et le total paraît
plausible.

Le vrai problème n'a d'ailleurs pas été traité : si le rapport était lent, c'est probablement un index
manquant ou un prédicat non sargable, sujets de `sql-index-plans-001`. `NOLOCK` a masqué le symptôme
de contention et introduit un défaut d'exactitude.

Si l'objectif était de ne pas bloquer les écrivains, `SNAPSHOT` l'obtient sans sacrifier la
cohérence — c'est la réponse correcte à la question posée.

## Vérification de compréhension

Distinguez en une phrase la lecture non répétable du fantôme, puis dites quel niveau empêche l'une
mais pas l'autre.

:::quiz
id=sql-isolation-001-check
question=Que risque-t-on réellement en ajoutant NOLOCK à un rapport financier ?
option=Des données légèrement périmées, mais un total toujours exact
option=Des lectures de transactions non validées, et un parcours qui peut omettre ou compter deux fois des lignes existantes
option=Un risque d'interblocage accru entre les sessions de lecture
correct=1
success=Correct : au-delà de la lecture sale, un parcours sans verrou peut manquer ou dupliquer des lignes lors d'une réorganisation de pages. Le total peut être faux sans aucune annulation.
retry=Relisez le passage sur ce que NOLOCK échange : le compromis n'est pas « périmé contre rapide », mais « potentiellement faux contre sans attente ».
:::

## Exercice guidé

Ouvrez le scénario `sql-concurrency-candidates-001` dans `/sql-lab`, puis procédez ainsi.

1. Écrivez, avant toute requête, laquelle des quatre anomalies le scénario met en jeu.
2. Reproduisez la lecture puis l'écriture, et prédisez le résultat final.
3. Corrigez par écriture relative avec garde, puis par vérification de version, et comparez les deux.
4. Validez contre la référence, puis réinitialisez la session.

## Exercice autonome

Un service réserve des places de formation. Deux utilisateurs réservent la dernière place en même
temps.

Décidez avant d'écrire : quelle anomalie est en jeu, quel niveau d'isolation vous retenez et pourquoi,
si vous préférez une garde à l'écriture ou une vérification de version, ce que voit l'utilisateur
perdant, et pourquoi vous ne montez pas jusqu'à `SERIALIZABLE`.

## Débogage

Un ticket indique : « Le stock affiché devient parfois négatif alors qu'une contrainte l'interdit. »

1. **Symptôme** : la contrainte tient — sinon l'écriture échouerait — mais des réservations sont
   perdues, et le stock ne correspond plus aux commandes.
2. **Hypothèse** : deux sessions lisent la même valeur et écrivent chacune un résultat calculé, la
   seconde écrasant la première.
3. **Preuve** : rejouez deux sessions en parallèle sur le même produit et comparez le stock final à la
   somme des quantités réservées. Un écart confirme la mise à jour perdue.
4. **Prévention** : passer à une écriture relative avec garde, vérifier `@@ROWCOUNT`, et ajouter un
   test de concurrence rejouant deux sessions simultanées.

## Entretien

Question posée à voix haute : *à quel niveau d'isolation tourne votre application, et pourquoi ?*

Une réponse solide connaît le défaut du moteur employé, nomme l'anomalie que ce défaut laisse passer,
et donne un cas précis où l'équipe a dû monter — ou a choisi la concurrence optimiste plutôt que
l'isolation. Une réponse faible cite `SERIALIZABLE` comme le choix le plus sûr sans mentionner la
contention.

## Résumé

- Lecture sale, lecture non répétable, fantôme, mise à jour perdue : quatre symptômes distincts.
- Un niveau d'isolation est la liste des anomalies acceptées, pas un réglage de vitesse.
- `SNAPSHOT` supprime l'attente entre lecteurs et écrivains sans sacrifier l'exactitude.
- `NOLOCK` peut omettre ou dupliquer des lignes : le résultat devient faux, pas seulement périmé.
- La concurrence optimiste place la décision de conflit dans le métier.

## Cartes de révision

Question : quelle différence entre une lecture non répétable et un fantôme ? Réponse attendue : la
première porte sur une ligne modifiée, le second sur une ligne apparue dans l'ensemble.

Question : pourquoi `Stock = Stock - 2` avec garde résout-il la mise à jour perdue ? Réponse attendue :
le moteur calcule à partir de la valeur courante au moment de l'écriture, pas d'une valeur lue avant.

## Test de maîtrise

Sans relire, décrivez le scénario complet d'une mise à jour perdue sur une réservation, nommez
l'anomalie, proposez deux corrections de natures différentes, et justifiez le niveau d'isolation que
vous retiendriez pour le rapport de suivi associé.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
