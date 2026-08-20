# Désigner les modules qui bloquent la fusion sous le plancher de couverture

Implémentez `Submission.CoverageBlockers` avec la signature fournie. Votre équipe impose un plancher
de couverture de branches par module : une demande de fusion n'entre pas tant qu'un module mesuré
reste en dessous. La fonction reçoit, module par module, le nombre de branches couvertes et le nombre
total de branches, plus le plancher en pourcentage entier.

## Ce qu'il faut produire

Rendez les indices des modules qui bloquent, dans l'ordre croissant. Un module bloque quand sa part
couverte est **strictement inférieure** au plancher.

```text
CoverageBlockers([80, 45, 60], [100, 60, 60], 80)  →  [1]
CoverageBlockers([0, 7], [0, 10], 70)              →  []
```

## La comparaison qui ne triche pas

La tentation est de calculer un pourcentage flottant puis de le comparer au plancher. C'est exactement
là que la porte fuit : un module à 79,96 pour cent, arrondi à un chiffre près quelque part sur le
chemin de calcul, peut se présenter comme un quatre-vingts tout rond. La décision doit se
poser en arithmétique entière — la part couverte atteint le plancher quand le produit du couvert par
cent atteint le produit du plancher par le total. Prévoyez des dépôts réels : les produits peuvent
dépasser ce qu'un entier de trente-deux bits contient.

## Le module sans branche

Un module dont le total de branches est zéro n'a rien à couvrir : il passe. Le bloquer reviendrait à
punir un module de ne contenir aucune décision, et pousserait l'équipe à écrire du code mort pour
nourrir la mesure.

## Les refus

`ArgumentException` quand les deux tableaux n'ont pas la même longueur, quand un compte est négatif,
ou quand un module déclare plus de branches couvertes que de branches existantes — une telle mesure
est corrompue et ne doit pas produire de décision. `ArgumentOutOfRangeException` quand le plancher
sort de zéro à cent.

## Avant d'écrire

Prédisez la décision pour un module à quatre branches couvertes sur cinq avec un plancher à
quatre-vingts, puis pour un module à 799 branches couvertes sur 1 000. Dites lequel des deux un
calcul flottant arrondi aurait laissé passer à tort.
