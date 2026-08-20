# Tracer un solde dans un grand livre hérité

Vous heritez d'un calcul de solde ecrit par quelqu'un d'autre, sans documentation. Implementez
`Submission.LegacyBalance` avec la signature fournie, en reconstituant sa regle a partir des cas.

## Le format

`ledger` est une suite d'entrees separees par des points-virgules. Chaque entree est `credit:MONTANT`,
`debit:MONTANT`, ou le mot `void`. Les blancs autour d'une entree ne comptent pas et les segments
vides sont ignores. Les montants sont ecrits en culture invariante.

```text
ledger = "credit:100;debit:30;void"
```

## La regle héritée

Le solde part de zero. Une entree `credit:A` ajoute `A`, une entree `debit:A` retranche `A`. Le mot
`void` est la particularite du code hérité : il **annule l'effet de la derniere entree appliquee**,
credit ou debit, en le retranchant du solde. Un `void` qui ne suit aucune entree appliquee est **sans
effet** : ce n'est pas une remise a zero du solde.

Une entree mal formee, un montant illisible ou un type d'entree inconnu levent `ArgumentException`.
Un corps absent leve `ArgumentNullException`.

## Avant d'ecrire

Predisez le solde pour `credit:50;void`, pour `credit:10;debit:5;void`, et pour un `void` seul.
Ecrivez d'abord l'hypothese que vous testez sur ce que `void` annule, avant de coder.
