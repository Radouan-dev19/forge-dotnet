# Décider l'ouverture d'une porte de déploiement sur pièces

Implémentez `Submission.GateDecision` avec la signature fournie. Une porte de déploiement ne juge pas
l'humeur du pipeline : elle exige des preuves nommées, et elle décide sur pièces. Votre fonction
reçoit le rapport des contrôles et la liste des preuves exigées, et rend la décision motivée.

## Les formats

Le rapport : des entrées `contrôle=statut` séparées par des points-virgules, statuts `ok`, `ko` ou
`pending`. La liste des exigences : des noms séparés par des virgules, dans l'ordre de priorité que la
porte affiche à l'équipe.

## La décision

- `refused|nom` — au moins une preuve exigée est en échec ; le nom est la première en échec **dans
  l'ordre des exigences**. Un refus l'emporte sur toute attente : inutile d'attendre une preuve quand
  une autre condamne déjà le déploiement ;
- `waiting|nom` — aucune preuve exigée n'est en échec, mais au moins une est en attente ou n'a jamais
  été rapportée ; le nom est la première concernée. Une preuve absente du rapport n'est pas un refus :
  le contrôle n'a simplement pas encore parlé ;
- `open` — toutes les preuves exigées sont vertes.

Un contrôle rapporté mais non exigé n'influence jamais la décision, même en échec : la porte applique
sa liste, pas le tableau de bord entier.

```text
GateDecision("tests=ok;coverage=pending;scan=ok", "tests,coverage")  →  "waiting|coverage"
GateDecision("tests=ko;coverage=ok", "tests,coverage")               →  "refused|tests"
GateDecision("tests=ok;style=ko", "tests")                           →  "open"
```

## Les refus

`ArgumentException` pour un rapport vide ou illisible, un statut hors des trois connus, un contrôle
rapporté deux fois, une liste d'exigences vide ou contenant un nom vide — une porte sans exigences
n'est pas ouverte, elle est mal configurée.

## Avant d'écrire

Prédisez la décision quand la première exigence est en attente et la seconde en échec, puis quand une
exigence n'apparaît nulle part dans le rapport. Dites ce que chaque réponse déclenche côté équipe.
