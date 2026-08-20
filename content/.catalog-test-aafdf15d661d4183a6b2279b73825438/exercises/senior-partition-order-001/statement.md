# Repérer les clés dont l'ordre n'est plus garanti par le partitionnement

Implémentez `Submission.OrderingRisks` avec la signature fournie. Un journal partitionné ne promet
l'ordre des messages **qu'à l'intérieur d'une partition** : deux messages de la même clé, routés vers
la même partition, arrivent dans l'ordre d'émission ; routés vers deux partitions, ils arrivent dans
l'ordre que le hasard des consommateurs décide. Votre fonction audite un journal de routage et relève
les clés qui ont perdu cette garantie.

## Le format du journal

Des affectations `clé:partition` séparées par des points-virgules, dans l'ordre d'émission.

## Ce qu'il faut produire

Les clés apparues sur **plus d'une partition** — celles dont l'ordre n'est plus garanti — triées par
ordre ordinal, jointes par des virgules ; la chaîne vide si le routage est sain. Deux clés
différentes sur la même partition ne posent aucun problème : elles ne partagent aucun ordre à
préserver.

```text
OrderingRisks("ord-1:p0;ord-2:p1;ord-1:p0")            →  ""
OrderingRisks("ord-1:p0;ord-1:p2")                      →  "ord-1"
OrderingRisks("a:p0;b:p1;a:p1;c:p2;b:p1")               →  "a"
```

Cette dérive a des causes connues : un producteur qui route à la ronde au lieu de router par clé, un
repartitionnement qui a changé la fonction de hachage, un correctif qui a modifié la casse de la clé.
Le symptôme, lui, est toujours le même — des états qui arrivent dans le désordre pour certaines
entités — et il ne se voit ni dans les horodatages ni dans le contenu des messages : seul le journal
de routage le montre.

## Les refus

`ArgumentException` pour un journal vide ou blanc, une affectation sans ses deux champs, ou un champ
vide.

## Avant d'écrire

Prédisez le rapport d'un journal où une clé visite trois partitions, puis d'un journal où toutes les
clés partagent l'unique partition. Dites pourquoi le second cas préserve tous les ordres — et ce
qu'il sacrifie à la place.
