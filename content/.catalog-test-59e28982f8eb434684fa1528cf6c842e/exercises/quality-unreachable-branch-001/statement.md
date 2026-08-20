# Repérer la branche qu'aucune valeur n'atteindra

Implémentez `Submission.FirstUnreachableCondition` avec la signature fournie. La fonction analyse une
chaîne de conditions écrites l'une après l'autre et désigne la première qui ne pourra jamais être
évaluée à vrai.

## Ce que décrit la chaîne

`chain` est une suite de conditions séparées par une barre verticale. Elles portent toutes sur la même
variable entière et s'enchaînent comme une cascade de tests : la deuxième n'est évaluée que si la
première est fausse, la troisième que si les deux premières sont fausses, et ainsi de suite.

Chaque condition s'écrit avec un opérateur suivi d'un nombre entier, sans espace :

| Écriture | Valeurs satisfaites |
|---|---|
| `<n` | tout ce qui est strictement inférieur à `n` |
| `<=n` | tout ce qui est inférieur ou égal à `n` |
| `>n` | tout ce qui est strictement supérieur à `n` |
| `>=n` | tout ce qui est supérieur ou égal à `n` |
| `==n` | la seule valeur `n` |

```text
chain = "<10|<5"
```

## Ce qu'il faut trouver

Une condition est **inatteignable** lorsque toute valeur entière qui la satisfait satisfait déjà au
moins une condition écrite avant elle. La cascade s'arrête alors avant d'y parvenir, quelle que soit
l'entrée : la branche est du code mort.

Dans l'exemple ci-dessus, `<5` est inatteignable, parce que toute valeur inférieure à cinq est déjà
inférieure à dix.

La fonction rend le **rang** de la première condition inatteignable, compté à partir de un. Si toutes
sont atteignables, elle rend zéro.

Le domaine considéré est celui des entiers signés sur trente-deux bits, bornes comprises. Une
condition que ce domaine ne peut satisfaire — parce qu'elle exige une valeur au-delà de ses limites —
est elle-même inatteignable.

## Les refus

Une chaîne vide, un opérateur inconnu ou un nombre illisible lèvent `ArgumentException`. Une chaîne
absente lève `ArgumentNullException`.

## Avant d'écrire

Prédisez quatre cas : deux conditions qui se partagent le domaine sans se recouvrir, deux conditions
dont la seconde est incluse dans la première, une égalité couverte par une inégalité antérieure, et
deux inégalités de même sens dans l'ordre où la seconde reste atteignable. Nommez ce que vous
représentez pour savoir ce qui est déjà couvert.
