# Retrier les remarques de revue selon la force de leur preuve

Implémentez `Submission.TriagedSeverities` avec la signature fournie. Une revue de code produit deux
familles d'erreurs de triage, et elles ne coûtent pas pareil : le vrai défaut classé anodin part en
production ; la préférence personnelle classée bloquante gèle une fusion et use la confiance. Votre
fonction retrie chaque remarque en confrontant la sévérité **revendiquée** à la force de sa
**preuve**.

## Le format du descriptif

Des remarques `nom:revendiquée:preuve` séparées par des points-virgules :

- `revendiquée` — `blocker`, `major` ou `minor` : ce que le relecteur a marqué ;
- `preuve` — `reproduces` (un cas d'échec concret est fourni), `theoretical` (le scénario est
  plausible mais personne ne l'a produit) ou `preference` (une affaire de goût, même écrite en
  capitales).

## Le triage

- `reproduces` → la revendication est honorée telle quelle : un défaut reproduit vaut ce que le
  relecteur en dit ;
- `theoretical` → le `blocker` se plafonne à `major` ; le reste est honoré — un scénario que
  personne n'a produit ne gèle pas une fusion à lui seul, mais il mérite son rang ;
- `preference` → `minor`, toujours : c'est le faux positif qui coûte — classé bloquant, il achète
  deux jours d'attente avec du goût personnel.

Rendez `nom=sévérité` joints par des points-virgules, dans l'ordre du descriptif.

```text
TriagedSeverities("null-deref:blocker:reproduces")                  →  "null-deref=blocker"
TriagedSeverities("naming:blocker:preference")                       →  "naming=minor"
TriagedSeverities("race:blocker:theoretical;style:minor:preference") →  "race=major;style=minor"
```

Le triage ne promeut jamais : une remarque reproduite revendiquée mineure reste mineure — le
relecteur a vu le défaut de près, sa modestie a valeur d'information.

## Les refus

`ArgumentException` pour un descriptif vide, une remarque sans ses trois champs, une sévérité ou une
preuve hors vocabulaire, ou un nom de remarque répété.

## Avant d'écrire

Prédisez le verdict d'une condition de concurrence théorique revendiquée bloquante, et dites ce que
son plafonnement à majeure déclenche en pratique : qui doit produire quoi pour qu'elle bloque ?
