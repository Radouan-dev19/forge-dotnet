# Déclencher l'alerte sur la persistance, pas sur le pic

Implémentez `Submission.FirstAlertIndex` avec la signature fournie. Une alerte qui sonne sur chaque
pic isolé finit acquittée sans être lue — et la vraie panne passe avec le bruit. La parade classique
est la **persistance** : n'alerter que lorsque la mesure reste au niveau ou au-dessus du seuil pendant
un nombre d'échantillons consécutifs convenu.

## Ce qu'il faut produire

La fonction reçoit la série des taux d'erreur mesurés, le seuil, et la longueur de série exigée.
Rendez l'indice — à partir de zéro — de l'échantillon qui **complète** la première série qualifiante :
c'est l'instant où l'alerte part. Rendez `-1` quand la fenêtre entière passe sans déclenchement, y
compris pour une fenêtre vide.

```text
FirstAlertIndex([0, 9, 2, 8, 7, 9, 1], 5, 3)   →  5
FirstAlertIndex([12, 0, 15, 0, 20], 10, 2)     →  -1
FirstAlertIndex([4, 4, 4], 4, 1)               →  0
```

Le premier exemple se lit ainsi : le 9 isolé ne suffit pas, la série 8, 7, 9 atteint trois
échantillons consécutifs, et l'alerte part sur le 9 final — indice 5. Le deuxième montre trois pics
violents mais jamais consécutifs : c'est du bruit, l'alerte se tait.

## Les règles de la série

Un échantillon **au seuil exact compte** : le contrat dit au niveau ou au-dessus. Un échantillon sous
le seuil remet la série à zéro, même après une longue montée — la persistance ne se cumule pas à
travers les accalmies. Et l'alerte part à l'échantillon qui complète la série, pas à celui qui la
commence : avant lui, rien ne distinguait cette montée d'un pic de plus.

## Les refus

`ArgumentOutOfRangeException` pour une longueur de série exigée inférieure à un ou un seuil négatif.
`ArgumentException` pour un taux d'erreur négatif — une telle mesure est corrompue. La fenêtre vide,
elle, est légitime : pas de mesure, pas d'alerte.

## Avant d'écrire

Prédisez l'indice pour une exigence de un — chaque dépassement alerte — puis pour une exigence plus
longue que la fenêtre. Dites lequel des deux réglages produit la fatigue d'alerte décrite plus haut.
