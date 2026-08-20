# Trouver le premier test qui dépend de ce qu'un autre a laissé

Implémentez `Submission.FirstLeakingTest` avec la signature fournie. La fonction lit la trace des
accès d'une suite à une base partagée et désigne le premier test qui ne tient plus tout seul.

## Le format de la trace

`trace` décrit les tests dans leur ordre d'exécution, séparés par une barre verticale. Chaque test
s'écrit `nom:opérations`, les opérations étant séparées par des virgules :

| Écriture | Signification |
|---|---|
| `+clé` | le test insère cette clé dans la base partagée |
| `-clé` | le test supprime cette clé |
| `?clé` | le test lit cette clé et s'appuie sur elle pour conclure |

```text
trace = "createsOrder:+order1,?order1,-order1|readsOrder:?order1"
```

Les opérations d'un même test s'appliquent dans l'ordre écrit, et la base garde son état d'un test au
suivant.

## Ce qu'on appelle une fuite

Un test **fuit** lorsqu'il lit une clé qui est présente dans la base sans que ce test l'ait
lui-même insérée pendant son propre passage. Il réussit alors parce qu'un test antérieur a oublié de
nettoyer, et il échouera dès qu'on l'exécutera seul, dans un autre ordre, ou en parallèle.

Deux situations ressemblent à une fuite et n'en sont pas :

- lire une clé **absente** de la base n'est pas une fuite : c'est une lecture qui échouera, ou une
  assertion volontairement négative ;
- lire une clé que le test vient d'insérer lui-même n'est pas une fuite, **même si** un test antérieur
  avait déjà laissé cette clé : le test se suffit à lui-même.

La fonction rend le nom du **premier** test qui fuit, une seule fois, quel que soit le nombre de clés
concernées. Une suite isolée rend une **chaîne vide**.

## Les refus

Un segment sans deux-points, ou une opération dont le premier caractère n'est aucun des trois
marqueurs, lève `ArgumentException`. Une trace absente lève `ArgumentNullException`.

## Avant d'écrire

Prédisez quatre cas : une suite propre, une suite où le premier test ne nettoie pas, un test qui lit
une clé absente, et un test qui réinsère une clé déjà laissée par un autre. Nommez ce qui change quand
la suite est exécutée dans l'ordre inverse.
