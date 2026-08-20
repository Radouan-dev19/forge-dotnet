# Router un message en échec : relance ou lettres mortes

Implémentez `Submission.PoisonVerdict` avec la signature fournie. La file des lettres mortes est la
soupape d'une messagerie : ce qui ne peut pas être traité en sort, avec sa raison, au lieu de tourner
en boucle devant les messages sains. Votre fonction décide, pour un message dont le traitement vient
de se conclure, la sortie qu'il mérite.

## Le format du message

Quatre attributs `clé=valeur` séparés par des points-virgules, dans un ordre quelconque :

- `payload` — `ok` ou `malformed` (impossible à désérialiser) ;
- `error` — l'issue du traitement : `none`, `transient` (réseau, verrou, indisponibilité passagère)
  ou `permanent` (règle métier violée, référence inexistante) ;
- `attempts` — le rang de la tentative qui vient de s'achever ;
- `max` — le budget de tentatives.

## Le routage

Quatre questions, dans cet ordre strict :

1. `payload=malformed` → `dead-letter|malformed-payload` : rejouer un message illisible reproduit
   l'échec à l'identique et fabrique la boucle empoisonnée — il sort immédiatement, budget intact ;
2. `error=none` → `ack|processed` ;
3. `error=permanent` → `dead-letter|non-retryable` : une règle métier violée le restera à la
   millième tentative ;
4. `error=transient` : `attempts` strictement sous `max` → `requeue|budget-remaining` ; sinon
   → `dead-letter|budget-exhausted`.

```text
PoisonVerdict("payload=ok;error=transient;attempts=1;max=5")  →  "requeue|budget-remaining"
PoisonVerdict("payload=ok;error=transient;attempts=5;max=5")  →  "dead-letter|budget-exhausted"
```

Chaque sortie vers les lettres mortes garde **sa** raison : l'exploitant qui dépile la file traite
différemment un lot de charges illisibles — un producteur à corriger —, des erreurs définitives — des
données à réparer — et des budgets épuisés — un aval à examiner.

## Les refus

`ArgumentException` pour un attribut illisible, répété, manquant ou hors vocabulaire, des tentatives
non positives, un maximum non positif, ou des tentatives au-delà du maximum — un tel relevé ne
décrit aucun message réel.

## Avant d'écrire

Prédisez la sortie d'un message illisible dont l'erreur déclarée est `none`, et dites pourquoi la
première question prime. Puis expliquez ce que perdrait l'exploitant si les trois chemins vers les
lettres mortes partageaient la même raison.
