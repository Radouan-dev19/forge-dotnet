# Rendre le verdict de rejeu d'un message livré à nouveau

Implémentez `Submission.ReplayVerdict` avec la signature fournie. Une messagerie sérieuse promet
**au moins une** livraison — jamais exactement une. Le consommateur reçoit donc des doublons par
conception, et sa première responsabilité est de décider, message par message, ce que ce doublon
mérite. Votre fonction rend ce verdict à partir du registre des traitements.

## Les formats

Le registre : des entrées `id:empreinte:statut` séparées par des points-virgules, statut `done` ou
`failed` — l'empreinte résume la charge utile traitée. Un registre **vide** (chaîne vide) est
légitime : c'est l'état de tout consommateur neuf. La livraison : `id:empreinte`.

## Le verdict

- identifiant inconnu du registre → `process|first-delivery` ;
- identifiant connu, empreinte **différente** → `reject|payload-mismatch` : un identifiant recyclé
  pour un autre contenu ne s'applique jamais, quel que soit le statut enregistré ;
- identifiant connu, même empreinte, traitement précédent `failed` → `retry|previous-failure` ;
- identifiant connu, même empreinte, traitement précédent `done` → `skip|already-applied`.

```text
ReplayVerdict("m1:h1:done", "m2:h2")    →  "process|first-delivery"
ReplayVerdict("m1:h1:done", "m1:h1")    →  "skip|already-applied"
ReplayVerdict("m1:h1:failed", "m1:h1")  →  "retry|previous-failure"
```

L'ordre des questions compte : la charge se vérifie **avant** le statut. Retenter un contenu qui
n'est pas celui enregistré appliquerait l'opération recyclée — la corruption même que le registre
existe pour empêcher.

## Les refus

`ArgumentException` pour une entrée de registre sans ses trois champs, un statut hors vocabulaire,
un identifiant enregistré deux fois — un registre qui se contredit ne juge plus rien —, ou une
livraison sans ses deux champs.

## Avant d'écrire

Prédisez les quatre verdicts d'une même livraison contre quatre registres différents, puis dites
lequel des quatre gestes n'est pas idempotent lui-même — et pourquoi cela n'est pas un problème pour
le mécanisme.
