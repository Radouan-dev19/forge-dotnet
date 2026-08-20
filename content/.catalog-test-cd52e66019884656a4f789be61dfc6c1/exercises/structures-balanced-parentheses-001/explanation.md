# Explication

Valider des parenthèses est l'exercice canonique de la pile — et la solution n'en utilise pas,
ce qui est précisément la leçon : savoir reconnaître quand une pile entière se réduit à un
compteur.

La version générale empile chaque ouvrant et dépile à chaque fermant, en vérifiant la
correspondance des types : indispensable dès qu'il y a plusieurs sortes de délimiteurs, car
`([)]` doit être refusé. Avec *un seul* type de parenthèse, l'information que la pile
transporte — combien d'ouvertures attendent leur fermeture — tient dans un entier : la
profondeur. Empiler devient incrémenter, dépiler devient décrémenter, et la pile d'objets
disparaît au profit d'un espace constant. Cette réduction structure-vers-compteur n'est pas une
astuce locale : c'est un raisonnement sur ce que la structure *représente*, à refaire chaque
fois qu'une pile ne sert qu'à compter.

L'équilibre se vérifie alors par deux conditions, et il faut les deux — c'est le cœur du
contrat. La première est *locale et immédiate* : jamais de profondeur négative. Un fermant qui
arrive sans ouvrant en attente — `)(` en est le cas minimal — rend le texte irréparable
sur-le-champ, et la solution retourne faux sans lire la suite : aucune ouverture future ne peut
racheter une fermeture déjà orpheline. La seconde est *globale et finale* : profondeur nulle
après le dernier caractère. Un texte qui se termine avec des ouvertures en attente — `((` — a
survécu à tout le parcours sans jamais fauter localement, et n'est pourtant pas équilibré. Les
implémentations fautives oublient l'une des deux : celle qui ne teste que la fin accepte `)(` —
la profondeur y finit à zéro ! — et celle qui ne teste que le négatif accepte `((`. Les cas
cachés posent exactement ces deux textes, parce qu'ils départagent les trois implémentations
possibles à eux seuls.

Les caractères qui ne sont pas des parenthèses traversent sans effet — le contrat valide la
structure, pas le contenu — et la chaîne vide est équilibrée par vacuité : profondeur nulle,
aucune faute locale.

Le coût est linéaire avec sortie précoce, l'espace constant — la version à pile paierait de
l'espace proportionnel à la profondeur pour le même verdict.

La transposition est double. Côté structure : imbrications de balises, blocs de code, sections
de configuration — le même compteur valide toute imbrication homogène, et la vraie pile revient
dès que les types de blocs se mélangent. Côté méthode : distinguer les invariants *locaux*
(vérifiables au fil de l'eau, avec sortie précoce) des invariants *globaux* (vérifiables
seulement à la fin) est une grille de lecture qui s'applique à tous les validateurs de flux.
