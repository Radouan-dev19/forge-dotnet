# Explication

Les sommes de fenêtres glissantes sont le deuxième grand échange calcul-contre-structure du
catalogue, après les sommes préfixes : au lieu de recalculer chaque fenêtre depuis zéro — coût
taille-fois-nombre-de-fenêtres —, on fait *glisser* une somme unique : l'élément entrant
s'ajoute, l'élément sortant se retire, et chaque fenêtre coûte deux opérations quelle que soit
sa largeur. La reformulation « la fenêtre suivante, c'est la précédente plus un bord moins
l'autre » est l'idée entière ; le reste est de la comptabilité d'indices.

Cette comptabilité mérite d'être déroulée une fois, car c'est elle que les cas cachés éprouvent.
Le nombre de fenêtres est la longueur moins la taille plus un — l'allocation du résultat
l'encode, et se tromper d'une unité ici décale ou tronque toute la sortie. Le retrait du sortant
commence quand `i >= size` : à cet instant, la somme couvre taille plus un éléments et
l'excédent est `values[i - size]`. La publication commence un cran plus tôt, quand
`i >= size - 1` : la première fenêtre complète se termine à cet indice, et sa position dans le
résultat est `i - size + 1`. Trois seuils voisins et distincts — c'est le nid à hors-par-un le
plus dense du catalogue, et l'énoncé demande précisément d'écrire ces trois moments avant de
coder : entrant, sortant, publication.

Les bornes d'entrée relèvent de la convention : une taille non positive ou supérieure à la
longueur ne définit aucune fenêtre, et le contrat rend le tableau vide — le « rien » des
collections — plutôt qu'une exception. La fenêtre exactement égale au tableau rend une seule
somme : le cas caché posé là vérifie l'inclusivité des trois seuils d'un coup. Les valeurs
négatives traversent sans traitement : une somme glissante peut décroître, et le glissement
additif ne suppose rien sur les signes.

Une honnêteté de domaine : ce glissement par addition-soustraction vaut pour les agrégats
*inversibles* — somme, moyenne, comptage. Le maximum glissant, lui, ne se « retire » pas : ôter
le sortant d'un maximum ne dit pas qui le remplace, et la structure requise devient une file
monotone — cousine de la pile monotone du voisin. Savoir classer un agrégat — inversible ou non —
avant de choisir la technique est la vraie compétence transportable.

Le coût final : une passe, deux opérations par élément, un tableau de sortie — linéaire là où le
naïf multipliait. La transposition est immédiate et fréquente : moyennes mobiles de mesures,
débits par tranche, charges sur les dernières n requêtes — tout tableau de bord temps réel fait
glisser une fenêtre, et celui qui la recalcule à chaque tick finit dans le profileur.
