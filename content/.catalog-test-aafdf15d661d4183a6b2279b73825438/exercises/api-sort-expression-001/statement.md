# Normaliser une expression de tri en ordre total

Implémentez `Submission.NormalizeSort` avec la signature fournie. La fonction traduit le tri demandé
par un client en un ordre que la base peut exécuter sans risque et qui pagine de façon stable.

## Les deux entrées

`expression` est ce que le client a écrit : des termes séparés par des virgules. Un terme s'écrit de
deux façons, et les deux sont acceptées :

```text
"total desc, createdAt asc"      terme suivi de son sens
"-total, createdAt"              tiret de tête pour l'ordre décroissant
```

Sans sens explicite ni tiret, le terme est croissant.

`allowed` est la liste blanche des champs triables, séparés par des virgules. **Son premier champ est
le tri par défaut.** Les noms se comparent sans tenir compte de la casse, mais le résultat rend
toujours l'orthographe de la liste blanche.

## Les règles

**Un champ absent de la liste blanche est refusé** par `ArgumentException`. Il n'est jamais ignoré en
silence : un client dont le tri disparaît sans rien dire croit lire des données triées et prend des
décisions dessus.

**Le champ `id` est toujours triable**, même absent de la liste blanche : c'est lui qui rend l'ordre
total.

**L'ordre rendu est toujours total.** Si `id` ne figure pas parmi les termes du client, il est ajouté
en dernière position, croissant. S'il y figure, il garde la place que le client lui a donnée et rien
n'est ajouté.

**Un champ répété ne compte qu'une fois**, à sa première occurrence : les suivantes ne changent rien
à l'ordre et n'ont pas à apparaître.

Une expression vide ou faite de blancs rend le tri par défaut, c'est-à-dire le premier champ de la
liste blanche en ordre croissant, suivi du départage.

Un sens qui n'est ni croissant ni décroissant est refusé par `ArgumentException`. Une entrée absente
lève `ArgumentNullException`.

## Le résultat

Chaque terme s'écrit `champ:asc` ou `champ:desc`, et les termes sont joints par une virgule sans
espace.

```text
NormalizeSort("total desc", "total,createdAt,name")  →  "total:desc,id:asc"
```

## Avant d'écrire

Prédisez quatre cas : un tri simple, un tri vide, un champ hors liste blanche, et un client qui trie
déjà par `id`. Nommez ce qui arrive à la pagination quand deux lignes portent le même total et que
rien ne les départage.
