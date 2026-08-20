# Explication

Une somme filtrée combine deux gestes élémentaires — sélectionner, puis réduire — et l'exercice
vaut par la précision qu'il exige sur chacun.

La sélection d'abord. « Positif » est un mot ambigu en français courant et précis en
mathématiques : la solution retient le sens strict, `value > 0`, qui exclut zéro. Pour la somme,
inclure zéro ne changerait pas le résultat — c'est l'élément neutre — mais la comparaison écrite
dit le contrat, et le jour où le même filtre servira à *compter* les éléments retenus, la
différence entre `>` et `>=` deviendra visible dans le résultat. Écrire la comparaison qui
correspond à l'énoncé, même quand une variante donnerait le même total, c'est refuser de laisser
le hasard décider d'un comportement futur. Les négatifs, eux, sont simplement ignorés : ni
soustraits, ni transformés en valeur absolue — deux réécritures créatives que les cas cachés,
qui mélangent signes et zéros, réfutent immédiatement.

La réduction ensuite, avec le mot clé qui fâche : `checked`. Une somme d'entiers dont on ne
contrôle ni le nombre ni l'amplitude finit un jour par dépasser la capacité du type, et
l'arithmétique par défaut de C# s'enroule alors sans un bruit — le total devient négatif ou
simplement faux, et il se propage. `checked` convertit ce mensonge en `OverflowException` levée
à l'instant du dépassement, sur la ligne fautive. Pour un agrégat financier ou un compteur, la
panne franche est préférable au chiffre plausible ; c'est un réglage d'une ligne qui change la
nature des pannes possibles, et le prendre en réflexe coûte moins cher que de l'ajouter après
l'incident.

Le régime d'erreurs complète le contrat : `null` signale une faute d'appel par
`ArgumentNullException`, tandis que le tableau vide rend zéro — la somme d'aucun terme est le
neutre de l'addition, pas une anomalie. Distinguer « pas de collection » de « collection sans
élément retenu » est une frontière que les cas cachés éprouvent des deux côtés : un tableau
entièrement négatif rend zéro lui aussi, par épuisement du filtre et non par cas spécial.

Le coût est un parcours linéaire, un accumulateur, aucune allocation — la version LINQ
`values.Where(v => v > 0).Sum()` dit la même chose en style requête, sans le `checked`
explicite ; la boucle garde ici l'avantage de montrer où la vérification s'applique.

La transposition est celle de tous les agrégats conditionnels : total des paiements encaissés en
ignorant les remboursements, volume utile en ignorant les rebuts. Trois questions à poser chaque
fois — quelle inégalité exactement, que vaut l'agrégat vide, que se passe-t-il au débordement —
et cet exercice est leur gabarit.
