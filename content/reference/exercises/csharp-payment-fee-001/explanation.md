# Explication

Des frais de paiement à deux régimes : l'exercice paraît être une branche et une multiplication,
et l'énoncé glisse la vraie question dans sa deuxième phrase — cette branche *représente* deux
implémentations d'une politique. Comprendre ce que cela veut dire change la façon d'écrire le
code aujourd'hui et de le faire évoluer demain.

La solution isole la décision dans une ligne : `rate = isCard ? 0.015m : 0m`. Tout le reste —
validation, multiplication, arrondi — est *commun* aux deux régimes. Cette séparation
décision-puis-mécanique est la version miniature d'un patron plus grand : quand les politiques
se multiplieront (virement, prélèvement, portefeuille électronique), le booléen deviendra une
interface dont chaque implémentation portera son taux ou sa formule, et la mécanique commune ne
bougera pas. Écrire dès maintenant la branche de façon à ce qu'elle *ressemble* à ce futur — une
donnée qui varie, un calcul qui ne varie pas — est ce qui rend la migration triviale le jour
venu. La version qui duplique le calcul dans chaque branche du `if`, elle, devra être démêlée
d'abord.

Les décisions d'argent suivent, et elles ne se négocient pas. Le montant négatif est refusé
avant tout calcul : des frais sur un paiement négatif ne signifient rien, et un remboursement
est une opération distincte, pas un signe moins passé en douce. Le taux s'applique en `decimal`
de bout en bout — les frais sont de l'argent, et l'argent ne se calcule pas en flottant
binaire. L'arrondi est unique, en sortie, au centime, avec sa règle nommée :
`AwayFromZero`, l'arrondi commercial, et non le défaut bancaire de .NET qui arrondit les milieux
vers le pair. Sur un montant qui produit un demi-centime exact de frais, les deux règles
divergent d'un centime — le cas caché posé là départage les implémentations qui ont laissé le
défaut décider.

Le régime gratuit mérite son mot : le taux zéro produit des frais de zéro, et c'est un *calcul*,
pas un court-circuit. Passer par la même multiplication et le même arrondi garantit que les deux
régimes rendent le même *type* de résultat — un montant arrondi au centime — et qu'aucune
branche ne dérive un jour vers sa propre convention. L'uniformité du chemin de sortie est une
protection contre les divergences futures.

Le coût est constant. La transposition est double : côté conception, chercher dans chaque
branche ce qui est décision et ce qui est mécanique, et ne dupliquer que la première ; côté
argent, exiger le trio décimal-arrondi unique-règle nommée dans tout code de frais, de taxe ou
de remise — les trois mêmes exigences que pour un total de ligne, parce que c'est le même
métier.
