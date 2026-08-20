# Mesurer la vitesse de combustion d'un budget d'erreur

Implémentez `Submission.BurnRate` avec la signature fournie. Le budget d'erreur restant dit combien
il reste ; il ne dit pas **à quelle vitesse ça part**. La vitesse de combustion — le rapport entre le
taux d'échec observé et le taux toléré par l'objectif — est le nombre qui déclenche les alertes
sérieuses : à un, le budget se consume exactement sur la fenêtre ; à quatorze, il part en deux jours
au lieu d'un mois.

## Le calcul

La fonction reçoit le volume de requêtes de la fenêtre, les échecs observés et l'objectif en
pourcentage. Elle rend :

```text
taux observé ÷ taux toléré
```

où le taux observé vaut `échecs ÷ volume` et le taux toléré `(100 − objectif) ÷ 100`, le tout en
décimal exact, **plancher au centième** — un chiffre d'alerte ne se flatte pas.

```text
BurnRate(10000, 20, 99.9)  →  2       (0,2 % observé pour 0,1 % toléré)
BurnRate(10000, 10, 99.9)  →  1       (combustion nominale)
BurnRate(10000, 5, 99.9)   →  0.5     (le budget s'épargne)
```

C'est ce nombre que les alertes multi-fenêtres comparent : une combustion de quatorze sur une heure
**et** de deux sur six heures réveille quelqu'un — le premier seuil dit l'intensité, le second confirme
que ce n'est pas un pic.

## Les refus

`ArgumentOutOfRangeException` pour une fenêtre sans requête — un taux sur zéro n'existe pas —, ou un
objectif hors de zéro exclu à **cent exclu** : l'objectif parfait ne tolère rien, son taux toléré est
nul et la division n'a pas de sens — ce cas se gouverne au budget restant, pas à la vitesse.
`ArgumentException` pour des échecs négatifs ou supérieurs au volume.

## Avant d'écrire

Prédisez la combustion d'une fenêtre de trois cents requêtes dont une seule échoue, sous trois
neufs. Puis dites pourquoi la vitesse se calcule sur plusieurs fenêtres à la fois — que rate une
alerte à fenêtre unique, courte ou longue ?
