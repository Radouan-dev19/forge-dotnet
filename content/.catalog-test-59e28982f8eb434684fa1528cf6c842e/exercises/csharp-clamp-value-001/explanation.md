# Explication

Borner une valeur est une opération de trois lignes que la bibliothèque standard fournit déjà —
`Math.Clamp` — et c'est précisément pour cela qu'elle fait un bon exercice : quand on réécrit un
outil connu, chaque écart avec l'original est une décision qu'il faut savoir défendre.

La première décision est la validation des bornes, et elle passe *avant* tout calcul. Un appel
avec `minimum > maximum` ne décrit aucun intervalle : il n'y a pas de bonne réponse à calculer,
seulement une faute d'appel à signaler. La tentation de « réparer » — échanger silencieusement
les bornes — produit un comportement défini sur une entrée qui n'aurait jamais dû exister, et
masque le bug amont qui a inversé les arguments. `Math.Clamp` lève dans ce cas, et la solution
fait de même : la cohérence avec la bibliothèque est un argument en soi, car elle rend le
comportement prévisible pour quiconque connaît l'original. Les cas cachés appellent avec des
bornes inversées et attendent l'exception, pas une valeur plausible.

La deuxième décision est l'inclusivité des bornes. Une valeur égale au minimum ou au maximum est
*dans* l'intervalle et passe inchangée — les comparaisons strictes `<` et `>` l'encodent. Écrire
`<=` à la place renverrait la borne pour une valeur qui y est déjà, ce qui est indolore ici mais
révèle une lecture approximative du contrat ; sur des types à égalité non triviale, la différence
deviendrait observable. Les cas de test posés exactement sur les bornes départagent les
écritures, comme toujours quand un intervalle est en jeu.

La structure en trois sorties — sous le plancher, au-dessus du plafond, sinon inchangée — épuise
les cas par construction : chaque valeur tombe dans exactement une branche, et l'ordre des tests
n'a même pas d'importance une fois les bornes validées, contrairement aux classements par
tranches où les gardes s'enchaînent. Une seule expression imbriquée de `Math.Min` et `Math.Max`
ferait le même travail en une ligne ; elle est plus dense et n'offre aucun endroit où poser la
validation des bornes. Le choix des trois `if` est un choix de lisibilité assumé.

Le coût est constant. La transposition est plus riche qu'il n'y paraît : plafonner une taille de
page demandée par un client, brider un volume de réessais, contenir un pourcentage de
progression entre zéro et cent. Chaque fois, la même trilogie — valider l'intervalle, décider
l'inclusivité, laisser passer l'intérieur inchangé — et le même piège : réparer en silence ce
qui aurait dû être refusé à voix haute.
