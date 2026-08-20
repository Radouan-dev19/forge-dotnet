# Qualifier l'état final d'une saga depuis son journal d'événements

Implémentez `Submission.SagaVerdict` avec la signature fournie. « La saga a échoué » est une
étiquette, pas un état : entre l'échec et le repos, il y a la compensation, et c'est elle que
l'astreinte a besoin de qualifier. Votre fonction lit le journal d'événements d'une saga terminée et
rend l'un des trois états qui commandent l'action.

## Le format du journal

Des événements `étape:issue` séparés par des points-virgules, en ordre chronologique. Les issues :
`ok` (étape accomplie), `fail` (l'échec qui interrompt la saga — il n'y en a qu'un, elle s'arrête
là), `compensated` (une étape accomplie a été défaite).

## Le verdict

- aucun échec au journal → `completed` ;
- un échec, et **chaque** étape accomplie avant lui porte sa compensation → `compensated` : la saga
  est revenue au repos, rien à faire ;
- un échec, et il reste des étapes accomplies non défaites → `stuck|étape`, où l'étape nommée est la
  **dernière accomplie encore debout** — celle que l'ordre inverse de compensation défait en
  premier, donc la prochaine action de l'astreinte.

```text
SagaVerdict("reserve:ok;charge:ok;ship:fail;charge:compensated;reserve:compensated")
  →  "compensated"
SagaVerdict("reserve:ok;charge:ok;ship:fail;charge:compensated")
  →  "stuck|reserve"
SagaVerdict("reserve:ok;charge:ok;ship:ok")
  →  "completed"
```

## Les refus

`ArgumentException` pour un journal vide, un événement illisible, une issue hors vocabulaire, une
étape accomplie deux fois, un second échec — la saga s'est arrêtée au premier —, ou une compensation
qui ne correspond à aucune étape accomplie ou qui survient **sans échec en amont** : compenser ce qui
n'a pas échoué n'est pas de la prudence, c'est un journal qui raconte une histoire impossible.

## Avant d'écrire

Prédisez le verdict d'un journal réduit à un échec immédiat, sans aucune étape accomplie. Puis dites
pourquoi le blocage nomme la dernière étape debout et non la première : qu'est-ce que l'autre choix
ferait faire à l'astreinte ?
