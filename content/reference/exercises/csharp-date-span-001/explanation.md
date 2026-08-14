# Explication

Compter les jours entre deux dates a l'air d'une soustraction ; l'exercice existe parce que
c'est une soustraction *plus une convention*, et que la convention fait toute la valeur.

D'abord l'outil. `DateOnly.DayNumber` projette chaque date sur un entier — son rang depuis une
origine fixe — et transforme le problème calendaire en arithmétique pure : la différence des
rangs est l'écart en jours, exact à travers les mois de longueurs différentes et les années
bissextiles, parce que c'est le calendrier lui-même qui a produit les rangs. L'alternative qui
boucle de date en date en incrémentant un compteur donne le même résultat en temps linéaire au
lieu de constant ; elle se justifie quand chaque jour doit être *examiné* — comme dans le
comptage des jours ouvrés voisin — jamais quand seul l'écart importe. Choisir entre projection
arithmétique et parcours selon que l'on a besoin des jours eux-mêmes ou seulement de leur
nombre : c'est la première transposition de l'exercice.

Ensuite la convention, dans les deux mots du titre. « Inclusif » signifie que les deux bornes
comptent : du premier au premier juillet, un jour — d'où le `+ 1` après la différence. Ce plus
un est le hors-par-un archétypal : la version sans lui répond zéro pour un même jour et se
trompe d'une unité *partout*, ce qui la rend presque plausible en lecture rapide. Les cas cachés
posent la borne dégénérée — même date aux deux bouts — précisément pour rendre l'écart visible ;
une facturation à la journée, une réservation d'hôtel, un décompte de congés reproduisent tous
cette question, et la réponse change le prix.

Enfin l'intervalle inversé. Le contrat choisit zéro — « aucun jour dans cet intervalle » —
plutôt qu'une exception ou un nombre négatif. Le zéro se défend bien ici : l'appelant qui
agrège des durées peut sommer sans filtrer, et l'inversion accidentelle ne fabrique pas de
durée négative qui polluerait un total. L'exception se défendrait tout autant dans un contexte
où l'inversion révèle un bug amont. Comme toujours, la valeur de l'exercice n'est pas la
convention retenue mais son écriture explicite : la garde en tête de fonction est la phrase du
contrat devenue code, et le cas de test posé dessus l'empêche de dériver.

Le coût est constant — deux lectures de rang, une soustraction. Rien à optimiser, tout à
spécifier : c'est la signature des problèmes de dates, où les bugs ne viennent jamais du calcul
mais des mots « inclus », « strictement » et « inversé » laissés sans définition.
