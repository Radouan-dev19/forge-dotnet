# Modèle relationnel et contraintes

## Objectif observable

À la fin de cette leçon, vous saurez lire un schéma relationnel et en déduire les cardinalités,
placer une règle métier dans une contrainte plutôt que dans du code applicatif, et prédire le
comportement d'une comparaison portant sur une valeur absente.

## Prérequis

- Avoir lu `async-fundamentals-001` et savoir propager une annulation de bout en bout.
- Savoir lire une instruction `CREATE TABLE` et distinguer une colonne d'une ligne.

## Intuition

Une base relationnelle n'est pas un entrepôt de lignes : c'est un ensemble de règles que le moteur
fait respecter **quoi qu'il arrive**. Un code applicatif protège une règle tant qu'il est le seul à
écrire. Une contrainte la protège aussi contre le script de reprise, l'import de nuit, la correction
manuelle en production et le service voisin.

C'est la différence entre une convention et une garantie.

## Explication

**Le schéma du laboratoire, qui servira dans toutes les leçons SQL :**

```sql
Customers  (CustomerId PK, Name UNIQUE, City, IsActive)
Products   (ProductId PK, Name, Category, Price CHECK (Price >= 0), Stock CHECK (Stock >= 0))
Orders     (OrderId PK, CustomerId FK -> Customers, OrderDate, Status, Total CHECK (Total >= 0), DataVersion)
OrderLines (OrderLineId PK, OrderId FK -> Orders, ProductId FK -> Products, Quantity CHECK (Quantity > 0), UnitPrice CHECK (UnitPrice >= 0))
```

Ce schéma se lit comme un texte. Un client a plusieurs commandes, une commande appartient à un seul
client : c'est une relation « un à plusieurs », et elle se reconnaît au fait que la clé étrangère est
portée par le côté « plusieurs ». Une commande a plusieurs lignes ; une ligne référence un produit.
Entre commandes et produits, la relation est « plusieurs à plusieurs », **matérialisée** par la table
`OrderLines` — qui porte en plus ses propres attributs, quantité et prix unitaire.

**La clé primaire identifie, et rien d'autre.** Elle doit être stable et non métier. Utiliser le nom
du client comme clé primaire semble économique jusqu'au premier changement de raison sociale : il faut
alors mettre à jour toutes les lignes qui la référencent. Une clé technique n'a pas ce défaut. Le
`UNIQUE` sur `Name` exprime séparément la règle d'unicité métier, sans en faire une identité.

**La clé étrangère garantit qu'une référence pointe quelque part.** Sans elle, rien n'empêche une
commande de désigner un client inexistant — et ce genre de ligne orpheline apparaît toujours, tôt ou
tard, par un import ou une suppression. Le comportement en cascade se décide explicitement : refuser
la suppression d'un client qui a des commandes est presque toujours le bon choix, car supprimer
silencieusement l'historique de facturation est rarement l'intention.

**`CHECK` déplace une règle métier dans la donnée.** `Quantity > 0` dit qu'une ligne de commande de
zéro article n'existe pas. Écrite en C#, cette règle protège tant que le C# est le seul chemin
d'écriture. Écrite en contrainte, elle protège toujours. La question à se poser pour chaque règle :
*si quelqu'un écrivait directement en base, cette règle devrait-elle encore tenir ?* Si oui, c'est une
contrainte.

**`NULL` n'est pas une valeur : c'est l'absence d'information.** C'est la source d'erreur numéro un en
SQL, parce que la logique devient ternaire — vrai, faux, **inconnu**.

`WHERE City = NULL` ne retourne jamais rien, même sur les lignes où `City` est nulle : comparer à un
inconnu donne un inconnu, et `WHERE` ne garde que le vrai. La forme correcte est `IS NULL`. De même,
`WHERE City <> 'Paris'` **exclut** les lignes où `City` est nulle, ce qui surprend presque toujours :
on voulait « toutes celles qui ne sont pas Paris », on obtient « toutes celles dont on sait qu'elles
ne sont pas Paris ».

Les agrégats suivent une autre convention : `COUNT(*)` compte les lignes, `COUNT(City)` compte les
valeurs non nulles, et `SUM` ignore les nulles. Deux comptages qui diffèrent sur la même table
révèlent immédiatement la présence de valeurs absentes.

**`NOT NULL` est une décision de modélisation.** Chaque colonne nullable est une question laissée
ouverte : que signifie l'absence ici ? Si la réponse est « rien, c'est toujours renseigné », alors la
colonne doit être `NOT NULL` — et le moteur empêchera l'oubli. Les colonnes nullables sans raison sont
la première cause de logique défensive inutile dans le code applicatif.

**Le type reflète le domaine.** `decimal` pour un montant, pour la raison vue dans
`reference-types-001` : l'arithmétique décimale exacte. `date` plutôt que `datetime` quand l'heure n'a
pas de sens. Une longueur de chaîne bornée, qui documente l'attente et protège du stockage aberrant.

## Exemple commenté

La différence entre une règle en code et une règle en contrainte :

```sql
-- La règle vit dans la donnée : elle tient pour tout écrivain, y compris un script de reprise.
CREATE TABLE dbo.OrderLines
(
    OrderLineId int         NOT NULL CONSTRAINT PK_OrderLines PRIMARY KEY,
    OrderId     int         NOT NULL CONSTRAINT FK_OrderLines_Orders REFERENCES dbo.Orders (OrderId),
    ProductId   int         NOT NULL CONSTRAINT FK_OrderLines_Products REFERENCES dbo.Products (ProductId),
    Quantity    int         NOT NULL CONSTRAINT CK_OrderLines_Quantity CHECK (Quantity > 0),
    UnitPrice   decimal(10,2) NOT NULL CONSTRAINT CK_OrderLines_UnitPrice CHECK (UnitPrice >= 0)
);
```

Nommer les contraintes n'est pas cosmétique : le message d'erreur cite le nom. Un journal qui
affiche `CK_OrderLines_Quantity` se diagnostique immédiatement, là où un nom généré par le moteur
n'apprend rien.

La logique ternaire, sur les données du laboratoire :

```sql
-- Ne retourne JAMAIS de ligne : comparer à un inconnu produit un inconnu.
SELECT CustomerId FROM dbo.Customers WHERE City = NULL;

-- Forme correcte.
SELECT CustomerId FROM dbo.Customers WHERE City IS NULL;

-- Exclut aussi les villes absentes, ce qui n'est presque jamais l'intention.
SELECT CustomerId FROM dbo.Customers WHERE City <> 'Paris';

-- Intention réellement exprimée : « pas Paris, y compris quand la ville est inconnue ».
SELECT CustomerId FROM dbo.Customers WHERE City IS NULL OR City <> 'Paris';
```

## Contre-exemple et erreur fréquente

```sql
CREATE TABLE dbo.Orders
(
    OrderId    int          NOT NULL PRIMARY KEY,
    CustomerId int          NULL,            -- Aucune clé étrangère, et nullable sans raison.
    OrderDate  nvarchar(50) NULL,            -- Une date stockée en texte.
    Status     nvarchar(50) NULL,            -- Aucune contrainte sur les valeurs admises.
    Total      float        NULL             -- Un montant en virgule flottante binaire.
);
```

Cinq décisions, cinq problèmes durables.

`CustomerId` sans clé étrangère autorise les commandes orphelines : le premier import mal ordonné en
crée, et plus personne ne peut recalculer un chiffre d'affaires fiable. Nullable de surcroît, alors
qu'une commande sans client n'a pas de sens métier.

`OrderDate` en texte interdit toute comparaison chronologique correcte : `'2026-1-9' < '2026-10-02'`
est vrai en ordre lexicographique et faux en ordre de dates, selon le format. Un index sur cette
colonne ne sert à rien pour une plage.

`Status` sans contrainte accueille `Paid`, `paid`, `PAID` et `Payé` dans la même colonne. Six mois
plus tard, aucune requête de comptage n'est fiable.

`Total` en `float` produit des écarts de centimes à l'agrégation, pour la raison vue en
`reference-types-001`.

Et l'absence de `NOT NULL` généralisée oblige tout le code appelant à traiter des cas d'absence qui
n'ont aucune signification métier.

## Vérification de compréhension

Sur le schéma du laboratoire, dites quelle table matérialise la relation « plusieurs à plusieurs », et
pourquoi elle porte des attributs propres.

:::quiz
id=sql-relational-constraints-001-check
question=Une table Customers contient des lignes dont la colonne City est nulle. Que retourne la requête `SELECT CustomerId FROM dbo.Customers WHERE City <> 'Paris'` ?
option=Tous les clients dont la ville n'est pas Paris, y compris ceux dont la ville est inconnue
option=Uniquement les clients dont la ville est renseignée et différente de Paris : les villes absentes sont exclues
option=Toutes les lignes de la table, car la comparaison est toujours vraie pour une valeur absente
correct=1
success=Correct : comparer à une valeur absente produit un résultat inconnu, et la clause WHERE ne conserve que le vrai. Il faut ajouter explicitement le cas IS NULL.
retry=Relisez le passage sur la logique ternaire : la comparaison ne renvoie ni vrai ni faux, et WHERE ne garde que le vrai.
:::

## Exercice guidé

Ouvrez le scénario `sql-active-customers-001` dans `/sql-lab`, puis procédez ainsi.

1. Lisez le schéma visible et écrivez les cardinalités entre les quatre tables avant toute requête.
2. Écrivez la requête et prédisez le nombre de lignes attendu.
3. Exécutez, puis validez contre le résultat de référence.
4. Modifiez volontairement la condition pour inclure une comparaison à une valeur absente, et
   observez la différence de cardinalité.

## Exercice autonome

Concevez le schéma d'un module d'avoirs : un avoir est rattaché à une commande, porte un montant et
un motif, et ne peut jamais dépasser le total de la commande.

Décidez avant d'écrire : les clés, les colonnes nullables et leur signification, les contraintes
`CHECK`, ce qui se passe à la suppression d'une commande, et quelles règles resteront nécessairement
dans le code applicatif plutôt qu'en contrainte. Justifiez ce dernier point.

## Débogage

Un ticket indique : « Le chiffre d'affaires par client ne correspond pas au total général. »

1. **Symptôme** : la somme des sous-totaux est inférieure au total.
2. **Hypothèse** : des commandes référencent un client inexistant ou nul, et disparaissent au
   regroupement.
3. **Preuve** : comparez `COUNT(*)` et `COUNT(CustomerId)` sur la table des commandes, puis cherchez
   les commandes dont le client n'existe pas. Un écart confirme l'hypothèse.
4. **Prévention** : ajoutez la clé étrangère manquante et passez la colonne en `NOT NULL` après
   correction des données, plutôt que de compenser dans chaque requête.

## Entretien

Question posée à voix haute : *quand mettez-vous une règle métier en contrainte plutôt qu'en code ?*

Une réponse solide donne un critère opposable : la règle doit-elle tenir même si quelqu'un écrit
directement en base ? Elle reconnaît aussi les limites — une règle qui dépend d'un appel externe ou
d'un contexte utilisateur ne peut pas descendre dans le schéma — et cite le coût d'une contrainte à
l'écriture.

## Résumé

- Une contrainte est une garantie ; une règle en code est une convention.
- La clé étrangère est portée par le côté « plusieurs » de la relation.
- Une clé primaire identifie et ne doit pas être métier.
- `NULL` est une absence : la logique devient ternaire et `WHERE` ne garde que le vrai.
- Chaque colonne nullable est une question de modélisation laissée ouverte.

## Cartes de révision

Question : pourquoi `WHERE Status <> 'Paid'` exclut-il les lignes dont le statut est absent ? Réponse
attendue : la comparaison produit un résultat inconnu, que `WHERE` ne conserve pas.

Question : quelle différence entre `COUNT(*)` et `COUNT(Colonne)` ? Réponse attendue : le premier
compte les lignes, le second uniquement les valeurs non nulles de la colonne.

## Test de maîtrise

Sans relire, écrivez le `CREATE TABLE` d'une table de règlements rattachés à une commande. Justifiez
chaque `NOT NULL`, chaque `CHECK`, le comportement à la suppression de la commande, et donnez la
requête qui détecterait des règlements orphelins si la clé étrangère avait été oubliée.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
