# CTE et transactions atomiques

## Objectif observable

À la fin de cette leçon, vous saurez découper une requête complexe en étapes nommées sans dégrader
son plan, et vous saurez délimiter une transaction pour qu'elle protège une invariante métier sans
tenir de verrous plus longtemps que nécessaire.

## Prérequis

- Avoir lu `sql-aggregations-subqueries-001` et savoir énoncer le grain d'un résultat.
- Savoir exécuter une requête dans une session `/sql-lab`.

## Intuition

Une expression de table commune — CTE — donne un **nom** à une étape intermédiaire. Elle ne crée
aucune table et ne stocke rien : elle rend lisible une requête qui, écrite en sous-requêtes
imbriquées, deviendrait illisible en trois niveaux.

Une transaction, elle, répond à une question toute différente : *quelles écritures doivent réussir ou
échouer ensemble ?* Ce n'est pas une optimisation, c'est la protection d'une invariante métier contre
une interruption au milieu.

## Explication

**La CTE fixe le grain avant la projection.** Le motif le plus utile : calculer d'abord un agrégat au
bon niveau, le nommer, puis s'en servir. C'est exactement ce qui évite la multiplication de lignes vue
dans `sql-joins-001`. Une requête à trois étapes se lit alors de haut en bas comme un raisonnement,
au lieu de se dérouler de l'intérieur vers l'extérieur.

Plusieurs CTE s'enchaînent, séparées par des virgules, et chacune peut référencer les précédentes.
C'est ce qui permet de construire un calcul par paliers vérifiables : on peut exécuter la requête en
s'arrêtant à chaque palier pour contrôler le grain et la cardinalité.

**Ce que la CTE n'est pas.** Elle n'est pas une table temporaire : rien n'est matérialisé, et le
moteur intègre généralement sa définition dans le plan global. Conséquence importante : une CTE
référencée **deux fois** peut être **évaluée deux fois**. Si son calcul est coûteux et qu'elle sert
plusieurs fois, une table temporaire réelle est parfois préférable — c'est un arbitrage à mesurer, pas
une règle.

Elle n'est pas non plus une frontière d'optimisation : écrire une CTE ne « force » pas le moteur à
l'exécuter en premier.

**La CTE récursive parcourt une hiérarchie.** Avec `WITH ... AS (ancre UNION ALL partie récursive)`,
on remonte ou descend une arborescence stockée par identifiant de parent. Deux précautions
s'imposent : une condition d'arrêt réelle, et une borne de profondeur explicite. Une donnée
comportant un cycle ferait tourner la récursion indéfiniment — c'est le même risque que celui vu dans
`structures-trees-001`, et il se traite ici avec une option de récursion maximale.

**Une transaction protège une invariante, pas une performance.** La question à poser est toujours la
même : *si le processus s'arrêtait juste après cette instruction, l'état resterait-il cohérent ?* Si
la réponse est non, les instructions concernées appartiennent à la même transaction.

Décrémenter un stock et créer une ligne de commande sont indissociables : l'un sans l'autre laisse une
incohérence permanente. En revanche, insérer deux commandes de clients différents n'a aucune raison de
partager une transaction.

**La portée doit être la plus courte possible.** Une transaction tient des verrous ; tant qu'elle est
ouverte, les autres sessions attendent. Trois règles pratiques : ne jamais ouvrir une transaction
avant d'avoir toutes les données nécessaires, ne jamais y inclure un appel externe — service web,
envoi de courriel, lecture de fichier — et faire toutes les validations possibles avant de l'ouvrir.

Le pire cas est la transaction ouverte pendant qu'on attend une réponse d'un service tiers : le délai
réseau devient un délai de verrouillage pour toute l'application.

**Gérer l'échec explicitement.** Sans `TRY / CATCH`, une erreur au milieu peut laisser la transaction
ouverte, avec ses verrous. Le motif sûr : bloc `TRY` contenant `BEGIN TRANSACTION` et `COMMIT`, bloc
`CATCH` testant `XACT_STATE()` avant de faire `ROLLBACK`, puis relance de l'erreur. Tester l'état est
nécessaire car certaines erreurs rendent la transaction non validable, et un `ROLLBACK` aveugle
échouerait alors à son tour.

**Le laboratoire annule systématiquement.** Dans `/sql-lab`, chaque exécution est encadrée par une
transaction annulée après coup : vous pouvez écrire des `UPDATE` et observer leurs effets sans jamais
modifier durablement le jeu de données. C'est ce qui rend les scénarios rejouables — et c'est aussi
une bonne illustration du mécanisme.

## Exemple commenté

Trois paliers nommés, chacun vérifiable séparément :

```sql
-- Palier 1 : grain « commande ». On calcule le montant réel à partir des lignes.
WITH LineTotals AS
(
    SELECT   l.OrderId,
             SUM(l.Quantity * l.UnitPrice) AS LineTotal
    FROM     dbo.OrderLines AS l
    GROUP BY l.OrderId
),
-- Palier 2 : grain « client ». On remonte d'un niveau, sans multiplier les lignes.
CustomerTotals AS
(
    SELECT   o.CustomerId,
             COUNT(*)        AS OrderCount,
             SUM(t.LineTotal) AS Revenue
    FROM     dbo.Orders AS o
    JOIN     LineTotals AS t ON t.OrderId = o.OrderId
    WHERE    o.Status = 'Paid'
    GROUP BY o.CustomerId
)
-- Palier 3 : projection finale, enrichie du libellé.
SELECT   c.Name, ct.OrderCount, ct.Revenue
FROM     CustomerTotals AS ct
JOIN     dbo.Customers  AS c ON c.CustomerId = ct.CustomerId
ORDER BY ct.Revenue DESC, c.Name;
```

Une transaction correctement délimitée :

```sql
BEGIN TRY
    -- Toutes les validations possibles ont eu lieu AVANT : la transaction reste courte.
    BEGIN TRANSACTION;

    UPDATE dbo.Products
    SET    Stock = Stock - 2
    WHERE  ProductId = 4 AND Stock >= 2;      -- La condition évite un stock négatif.

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50001, 'Stock insuffisant pour le produit demandé.', 1;
    END

    INSERT dbo.OrderLines (OrderLineId, OrderId, ProductId, Quantity, UnitPrice)
    VALUES (7, 5, 4, 2, 30);

    COMMIT TRANSACTION;                        -- Les deux effets sont désormais indissociables.
END TRY
BEGIN CATCH
    -- XACT_STATE différent de zéro : une transaction est encore active ou non validable.
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;                                     -- L'erreur d'origine remonte intacte.
END CATCH;
```

Le `WHERE Stock >= 2` combiné au test de `@@ROWCOUNT` est le point important : il rend l'opération
sûre même si une autre session modifie le stock entre-temps, sans avoir besoin d'un verrou explicite.

## Contre-exemple et erreur fréquente

```sql
BEGIN TRANSACTION;

SELECT @stock = Stock FROM dbo.Products WHERE ProductId = 4;   -- Lecture…

-- ... appel à un service de paiement externe depuis le code applicatif ...
-- La transaction reste ouverte pendant toute la latence réseau.

IF @stock >= 2
BEGIN
    UPDATE dbo.Products SET Stock = @stock - 2 WHERE ProductId = 4;  -- Écriture d'une valeur lue avant.
    INSERT dbo.OrderLines (OrderLineId, OrderId, ProductId, Quantity, UnitPrice)
    VALUES (7, 5, 4, 2, 30);
END

COMMIT TRANSACTION;                                             -- Aucun traitement d'erreur.
```

Quatre défauts, du plus coûteux au plus discret.

L'appel externe **à l'intérieur** de la transaction transforme la latence du service de paiement en
durée de verrouillage. Si le service met dix secondes, toutes les sessions qui touchent ce produit
attendent dix secondes. Si le service ne répond jamais, les verrous ne sont jamais relâchés.

`Stock = @stock - 2` écrit une valeur calculée à partir d'une lecture antérieure. Deux sessions
concurrentes peuvent lire la même valeur et écrire le même résultat : une des deux décrémentations est
perdue. C'est la mise à jour perdue, traitée en détail dans `sql-isolation-001`. La forme
`Stock = Stock - 2` avec une condition de garde évite le problème.

Aucun `TRY / CATCH` : une erreur entre le `BEGIN` et le `COMMIT` laisse la transaction ouverte avec
ses verrous jusqu'à l'expiration de la session.

Enfin, rien ne garantit que le stock reste positif : la condition est testée sur une variable, pas
appliquée à l'écriture.

## Vérification de compréhension

Pour « décrémenter un stock puis créer la ligne de commande », dites pourquoi les deux instructions
partagent une transaction, et ce qu'il ne faut surtout pas y inclure.

:::quiz
id=sql-cte-transactions-001-check
question=Pourquoi ne faut-il jamais placer un appel à un service externe à l'intérieur d'une transaction ouverte ?
option=Parce que le moteur SQL interdit techniquement les appels réseau pendant une transaction
option=Parce que la latence du service devient une durée de verrouillage pour toutes les autres sessions
option=Parce que la transaction serait automatiquement annulée à la première attente réseau
correct=1
success=Correct : une transaction tient des verrous tant qu'elle est ouverte. Y inclure une attente externe transforme un délai réseau en contention généralisée.
retry=Relisez les trois règles de portée d'une transaction, et ce qui se passe pour les autres sessions pendant qu'elle reste ouverte.
:::

## Exercice guidé

Ouvrez le scénario `sql-monthly-cte-001` dans `/sql-lab`, puis procédez ainsi.

1. Écrivez le grain de chaque palier avant de taper la requête.
2. Construisez la CTE, puis exécutez-la seule pour vérifier sa cardinalité.
3. Ajoutez la projection finale, prédisez le nombre de lignes, exécutez et validez.
4. Écrivez ensuite un `UPDATE` volontairement fautif et observez que la session l'annule : c'est le
   mécanisme de la transaction du laboratoire.

## Exercice autonome

Écrivez la séquence transactionnelle qui enregistre un règlement : elle crée la ligne de règlement,
met à jour le statut de la commande, et refuse si le montant dépasse le reste dû.

Décidez avant d'écrire : les instructions qui doivent partager la transaction, celles qui doivent
rester en dehors, la condition de garde qui rend l'opération sûre en concurrence, et le traitement
d'erreur.

## Débogage

Un ticket indique : « L'application se bloque par intermittence sur la validation de commande, et se
débloque au bout de trente secondes. »

1. **Symptôme** : attente longue puis reprise, sans erreur — la durée correspond à un délai
   d'expiration.
2. **Hypothèse** : une transaction reste ouverte pendant un appel externe et fait attendre les autres
   sessions.
3. **Preuve** : mesurez la durée entre l'ouverture et la validation de la transaction, et cherchez tout
   appel réseau dans cet intervalle. Une durée corrélée à la latence du service confirme.
4. **Prévention** : sortir l'appel externe de la transaction, et ajouter une assertion qui échoue si
   la durée d'une transaction dépasse un seuil.

## Entretien

Question posée à voix haute : *comment décidez-vous des frontières d'une transaction ?*

Une réponse solide part de l'invariante métier plutôt que du code : quelles écritures laisseraient un
état incohérent si l'on s'arrêtait entre elles ? Elle ajoute les contraintes de durée et de verrous,
et cite l'appel externe comme le cas à exclure absolument.

## Résumé

- Une CTE nomme une étape et fixe le grain avant projection ; elle ne matérialise rien.
- Référencée deux fois, elle peut être évaluée deux fois.
- Une transaction protège une invariante, pas une performance.
- Toute validation possible se fait avant l'ouverture ; aucun appel externe à l'intérieur.
- `TRY / CATCH` avec test d'état avant annulation, puis relance de l'erreur d'origine.

## Cartes de révision

Question : quelle question détermine si deux instructions doivent partager une transaction ? Réponse
attendue : l'état resterait-il cohérent si le processus s'arrêtait entre les deux ?

Question : pourquoi `Stock = Stock - 2` est-il préférable à `Stock = @valeurLue - 2` ? Réponse
attendue : la seconde forme perd la décrémentation d'une session concurrente qui aurait lu la même
valeur.

## Test de maîtrise

Sans relire, écrivez la requête à trois paliers nommés qui retourne le chiffre d'affaires mensuel par
ville, puis la séquence transactionnelle qui annule une commande en restituant le stock. Justifiez le
grain de chaque palier, les frontières de la transaction et la condition de garde retenue.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
