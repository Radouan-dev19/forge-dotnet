# Estimer le temps de résorption d'un arriéré de file

Implémentez `Submission.DrainMinutes` avec la signature fournie. Une file en retard pose à
l'astreinte une question simple et chiffrable : dans combien de temps l'arriéré sera-t-il résorbé ?
La réponse ne dépend ni du courtier ni du langage — c'est une soustraction et une division — et
pourtant elle est presque toujours calculée faux, parce qu'on oublie que les arrivées continuent
pendant le drainage.

## Le calcul

La fonction reçoit l'arriéré (messages en attente), le débit d'arrivée et le débit de consommation,
en messages par minute :

- arriéré nul → `0` : il n'y a rien à résorber, quel que soit le rapport des débits ;
- consommation inférieure ou égale aux arrivées → `-1` : le débit net est nul ou négatif, l'arriéré
  stagne ou grossit, la question n'a pas de réponse finie ;
- sinon → le quotient **plafond** de l'arriéré par le débit net (consommation moins arrivées) : la
  dernière minute entamée se paie entière.

```text
DrainMinutes(600, 40, 100)   →  10      (débit net 60)
DrainMinutes(500, 100, 100)  →  -1      (débit net nul)
DrainMinutes(0, 50, 10)      →  0
```

Le troisième exemple mérite un mot : l'arriéré est nul, donc la réponse est zéro — même si le
rapport des débits annonce que la file va grossir. La fonction répond à la question posée, pas à la
suivante ; la surveillance du débit net est un autre signal.

## Les refus

`ArgumentOutOfRangeException` pour un arriéré, des arrivées ou une consommation négatifs — un débit
négatif ne mesure rien. La consommation nulle est acceptée : un consommateur arrêté est un état réel,
et la réponse est simplement l'impossibilité.

## Avant d'écrire

Prédisez la durée pour un arriéré de sept messages avec deux arrivées et cinq consommations par
minute, puis pour le même arriéré quand la consommation ne dépasse les arrivées que d'un message par
minute. Dites ce que le second résultat suggère à l'astreinte — et pourquoi doubler les consommateurs
ne double presque jamais le débit net.
