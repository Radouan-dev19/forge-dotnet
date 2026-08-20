# Argumenter le découpage d'un module depuis son profil de couplage

Implémentez `Submission.SplitVerdict` avec la signature fournie. « Faut-il en faire un service ? »
est la question d'architecture la plus posée et la plus mal répondue, parce qu'elle se tranche à la
mode ou à l'intuition. Votre fonction la tranche depuis un profil de couplage — quatre attributs
observables — et rend la décision motivée.

## Le format du profil

Quatre attributs `clé=valeur` séparés par des points-virgules, dans un ordre quelconque :

- `shared-data` — les données partagées avec le reste du système : `none`, `read-only` ou
  `read-write` ;
- `transaction` — le module participe-t-il à une transaction avec d'autres modules : `shared` ou
  `independent` ;
- `team` — l'équipe qui le fait évoluer : `same` ou `different` de celle du reste ;
- `cadence` — son rythme de livraison souhaité : `same` ou `different`.

## La décision

Une cascade en deux temps — l'interdit technique d'abord, la motivation ensuite :

1. `shared-data=read-write` **ou** `transaction=shared` → `keep-together|shared-invariants` :
   découper des invariants partagés fabrique une transaction distribuée, le problème le plus dur du
   distribué, pour ne résoudre qu'un problème d'organisation ;
2. `team=different` **et** `cadence=different` → `split|independent-evolution` ;
3. `cadence=different` seule → `split|release-pressure` ;
4. `team=different` seule → `split|team-autonomy` ;
5. sinon → `keep-together|no-forcing-function` : sans force motrice, le monolithe modulaire n'est
   pas un échec, c'est l'option la moins chère.

```text
SplitVerdict("shared-data=none;transaction=independent;team=different;cadence=different")
  →  "split|independent-evolution"
SplitVerdict("shared-data=read-only;transaction=independent;team=same;cadence=same")
  →  "keep-together|no-forcing-function"
```

La lecture seule de données partagées n'interdit pas : elle se sert par réplication ou par cache,
sans transaction commune. C'est l'écriture partagée qui verrouille.

## Les refus

`ArgumentException` pour un attribut illisible, répété, manquant ou hors vocabulaire.

## Avant d'écrire

Prédisez le verdict d'un module aux données partagées en écriture réclamé par une équipe distincte à
cadence distincte, et dites ce que la cascade répond à cette équipe : que faut-il changer d'abord
pour que le découpage devienne possible ?
