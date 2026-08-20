# Désigner le travail qui bloque un pipeline à partir de son journal

Implémentez `Submission.FirstBlockingJob` avec la signature fournie. Un pipeline vient de finir en
rouge et l'équipe regarde un mur de statuts. Votre fonction lit le journal et répond à la seule
question utile à cet instant : **quel travail faut-il ouvrir en premier, et pourquoi**.

## Le format du journal

Des entrées `travail=statut` séparées par des points-virgules, dans l'ordre chronologique. Les statuts
possibles : `ok`, `failed`, `skipped`, `canceled`. Un travail peut apparaître plusieurs fois — chaque
relance consigne une nouvelle entrée — et son **verdict final est sa dernière entrée** : une relance
qui réussit efface l'échec qui l'a précédée.

## Ce qu'il faut produire

Le nom du travail bloquant suivi d'une barre verticale et de son verdict final :

- le premier travail — en ordre de première apparition — dont le verdict final est `failed` ;
- à défaut, le premier dont le verdict final est `canceled` : sans échec, l'annulation est la cause,
  pas la conséquence ;
- à défaut, `none` : rien à corriger, même si le journal contient des rouges effacés par des relances.

```text
FirstBlockingJob("restore=ok;build=failed;test=canceled;publish=skipped")  →  "build|failed"
FirstBlockingJob("restore=failed;restore=ok;build=ok;test=ok")             →  "none"
```

Le premier exemple illustre la hiérarchie : l'annulation du test est une victime de l'échec de la
construction, pas un problème à corriger. Le second illustre la consolidation : l'échec de la
restauration a été effacé par sa relance réussie.

## Les refus

`ArgumentException` pour un journal vide, une entrée sans signe égal ou avec un nom vide, ou un statut
hors des quatre connus.

## Avant d'écrire

Prédisez la réponse pour un journal où un travail échoue puis est annulé à la relance, et pour un
journal où deux travaux différents portent un échec final. Dites, dans chaque cas, ce que votre
réponse fait gagner à la personne d'astreinte.
