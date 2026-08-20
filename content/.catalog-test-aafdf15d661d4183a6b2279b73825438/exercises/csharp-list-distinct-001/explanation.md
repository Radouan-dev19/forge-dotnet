# Explication

Dédupliquer *sans réordonner* : la deuxième moitié du titre est celle qui décide de toute
l'implémentation, et c'est elle que l'énoncé souligne en demandant quelle collection répond à la
question « déjà vu ? ».

La réponse tient dans le duo de collections, chacune à son poste. La liste résultat porte
l'*ordre* — chaque valeur y entre à sa première apparition et n'en bouge plus. L'ensemble `seen`
porte la *mémoire* — il répond en temps constant moyen à « cette valeur est-elle déjà passée ? ».
Aucune des deux ne sait faire le travail de l'autre : un ensemble seul perdrait l'ordre des
premières apparitions, une liste seule répondrait à « déjà vu ? » en la parcourant, ce qui
rendrait l'algorithme quadratique. Le duo mémoire-rapide plus sortie-ordonnée est un motif à
part entière, qu'on retrouve dans les caches avec journal d'insertion et les files de
déduplication d'événements.

Le détail le plus élégant de la solution est `if (seen.Add(value))`. `Add` sur un ensemble fait
deux choses en un appel : il insère si absent, et il *répond* — vrai si l'insertion a eu lieu,
faux si la valeur était déjà là. Cette réponse remplace le couple « contient ? puis ajoute »,
qui interroge la table deux fois et laisse un interstice où l'on peut se tromper. Connaître les
opérations qui rendent leur verdict — `Add` des ensembles, `TryGetValue` des dictionnaires,
`TryParse` des conversions — et les préférer aux paires question-puis-action, c'est un pli
d'écriture qui condense le code et supprime des branches.

Le contrat précise le reste. L'ordre conservé est celui des *premières* apparitions :
`[3, 1, 3, 2, 1]` devient `[3, 1, 2]`, jamais `[1, 2, 3]` — le cas caché dont l'entrée est
volontairement non triée réfute la déduplication par tri, qui serait la solution de facilité.
La liste rendue est *neuve*, y compris pour une entrée vide : l'appelant reçoit toujours un
objet à lui, jamais une référence partagée vers sa propre liste — et l'entrée n'est pas
modifiée, ce que le harnais vérifie. `null` reste une faute d'appel, signalée par
`ArgumentNullException` via l'assistant `ThrowIfNull`.

Le coût : un parcours, une interrogation-insertion par élément, soit un temps linéaire moyen et
un espace proportionnel au nombre de valeurs distinctes. C'est l'échange espace-contre-temps
déjà rencontré dans la détection de paires, appliqué cette fois à la conservation d'ordre.

La transposition est quotidienne : premiers passages d'utilisateurs, premières occurrences de
codes d'erreur, désabonnements à ne traiter qu'une fois — partout où « unique » doit cohabiter
avec « dans l'ordre d'arrivée », ce duo de collections est la réponse canonique.
