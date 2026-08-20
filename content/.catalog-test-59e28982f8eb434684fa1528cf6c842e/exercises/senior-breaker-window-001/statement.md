# Décider la transition d'un disjoncteur depuis sa fenêtre mesurée

Implémentez `Submission.BreakerDecision` avec la signature fournie. Un disjoncteur ne devine pas : il
décide depuis des mesures — une fenêtre d'appels quand il est fermé, un refroidissement quand il est
ouvert, des sondes quand il est demi-ouvert. Votre fonction reçoit le relevé de l'état courant et
rend la transition motivée.

## Le format du relevé

Des paires `mesure=valeur` séparées par des points-virgules, dont `state` — `closed`, `open` ou
`half-open`. Chaque état exige **exactement** ses mesures :

- `closed` : `calls`, `failures`, `minimum-calls`, `max-rate` (pourcentage entier de 0 à 100) ;
- `open` : `elapsed`, `cooldown` (secondes) ;
- `half-open` : `probes`, `probe-failures`, `required-probes`.

## La décision

Rendez `transition|raison` :

- fermé : sous `minimum-calls` appels → `stay-closed|insufficient-data` — un taux mesuré sur trois
  appels ne prouve rien, et un disjoncteur nerveux coupe des services sains ; sinon, si le produit
  `failures × 100` dépasse **strictement** `max-rate × calls` → `open|rate-exceeded` ; sinon
  `stay-closed|healthy` ;
- ouvert : `elapsed` a atteint `cooldown` → `half-open|probe-allowed` ; sinon `stay-open|cooling` ;
- demi-ouvert : toute sonde en échec → `open|probe-failed`, sans attendre le compte ; sinon, si
  `probes` atteint `required-probes` → `closed|probes-passed` ; sinon `stay-half-open|probing`.

```text
BreakerDecision("state=closed;calls=50;failures=26;minimum-calls=20;max-rate=50")
  →  "open|rate-exceeded"
BreakerDecision("state=closed;calls=10;failures=9;minimum-calls=20;max-rate=50")
  →  "stay-closed|insufficient-data"
```

Le taux se compare en produits d'entiers larges : le pourcentage flottant arrondi ouvrirait ou
retiendrait le circuit sur une erreur de représentation, et une fenêtre de deux milliards d'appels
déborde le trente-deux bits.

## Les refus

`ArgumentException` pour une paire illisible, un état hors des trois connus, une mesure répétée,
manquante ou étrangère à l'état, une valeur non numérique ou négative, plus d'échecs que d'appels,
plus d'échecs de sonde que de sondes, ou un taux hors de zéro à cent.

## Avant d'écrire

Prédisez la décision d'une fenêtre fermée dont le taux tombe exactement sur le maximum toléré, puis
celle d'un demi-ouvert dont toutes les sondes exigées sont passées sauf une, en échec. Dites pourquoi
la garde de volume se vérifie avant le taux et jamais l'inverse.
