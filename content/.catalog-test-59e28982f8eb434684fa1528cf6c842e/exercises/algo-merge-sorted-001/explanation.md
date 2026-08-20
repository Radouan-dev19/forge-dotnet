# Explication

La fusion de deux tableaux triés est le cœur du tri fusion, mais elle vit aussi seule, partout où
deux flux ordonnés doivent n'en devenir qu'un : deux journaux datés, deux listes de résultats
paginées, deux relevés à réconcilier. L'algorithme se résume en une phrase — comparer les deux
têtes, consommer la plus petite, avancer l'index correspondant — et toute la difficulté loge dans
trois décisions de bord que cette phrase ne dit pas.

La première : avancer *exactement* l'index de la valeur consommée. L'erreur classique avance les
deux index après une comparaison, comme si consommer une tête vidait l'autre. Le résultat saute
alors des valeurs, mais reste trié — c'est ce qui rend le bug sournois, car un contrôle visuel
rapide n'y voit rien. Seule la longueur du résultat, ou une valeur attendue manquante, le
trahit ; les cas cachés comptent là-dessus.

La deuxième : l'épuisement asymétrique. Quand une source est vide, l'autre doit être recopiée
jusqu'au bout. La solution traite ce drainage dans la même boucle, par une garde qui donne la
priorité à la source restante — `j >= right.Length` force la gauche, et le `i < left.Length` dans
la branche droite protège l'accès symétrique. On peut préférer deux boucles de drainage séparées
après la boucle principale : c'est équivalent et parfois plus lisible ; l'important est que le
cas « une source vide dès le départ » fonctionne, ce que les cas cachés éprouvent avec un tableau
d'entrée vide.

La troisième : l'égalité. `left[i] <= right[j]` et non `<` — à valeurs égales, la gauche passe
d'abord. Sur des entiers, les deux choix rendent le même tableau ; le contrat exige pourtant le
« ou égal », parce qu'il conserve les doublons dans un ordre déterminé et documente la stabilité
de la fusion. Le jour où les éléments porteront un identifiant ou une date, ce caractère près
décidera si deux exécutions rendent la même chose.

Le coût est la raison d'être de l'algorithme : chaque élément est regardé et écrit une seule
fois, donc un temps linéaire en la somme des longueurs — fusionner puis trier naïvement coûterait
un logarithme de plus pour rien, et surtout ignorerait une information que l'appelant a déjà
payée : ses deux entrées sont triées. Exploiter une propriété garantie par le contrat au lieu de
la recalculer, c'est la transposition la plus générale de cet exercice, et elle vaut pour les
index de bases de données comme pour les flux d'événements.
