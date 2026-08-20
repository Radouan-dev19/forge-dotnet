# Explication

Compter les pairs est le comptage conditionnel dans sa forme nue — un parcours, un prédicat, un
compteur — et sa seule vraie difficulté est de définir « pair » sur *tout* le domaine des
entiers, pas seulement sur les petits positifs des exemples.

Le prédicat retenu est `value % 2 == 0`, et il faut savoir pourquoi il est correct partout. Zéro
est pair — son reste par deux est nul — et le contrat le nomme parce que l'intuition « pair,
c'est-à-dire divisible en deux parts » hésite parfois devant lui. Les négatifs sont le piège
plus sérieux : en C#, le reste hérite du signe du dividende, donc moins quatre modulo deux vaut
zéro, mais moins trois modulo deux vaut *moins un*, pas un. Le prédicat écrit avec `== 0` est
insensible à ce détail — zéro n'a pas de signe — alors que son jumeau « impair », écrit
naïvement `value % 2 == 1`, est faux sur tous les impairs négatifs. Comprendre pourquoi l'un des
deux prédicats est robuste et l'autre non vaut plus que l'exercice entier : c'est le genre de
connaissance qui évite un bug de production sur des soldes ou des écarts signés. La variante
`value % 2 != 0` ou la comparaison de bits `(value & 1) == 0` sont d'autres écritures robustes ;
la solution garde la plus lisible.

Le squelette autour du prédicat suit le régime commun du catalogue. `null` est une faute d'appel
signalée nommément — pas de collection n'est pas une collection sans pairs. Le tableau vide rend
zéro par simple non-exécution de la boucle : aucun cas spécial, le compteur initialisé à zéro
*est* la réponse du cas vide, et c'est une élégance à remarquer — les bonnes initialisations
absorbent les bornes. L'entrée n'est jamais écrite, seulement lue.

Les cas cachés se déduisent du contrat : un tableau avec zéro dedans, des pairs et impairs
négatifs mêlés, le tableau sans aucun pair qui rend zéro, et une disposition différente de
l'exemple pour réfuter la réponse figée. Le coût est linéaire en temps, constant en espace —
c'est la borne basse d'un comptage, chaque case devant être examinée.

La transposition est double. D'abord le motif : parcours-prédicat-compteur est la forme manuelle
de `Count(predicate)` en LINQ, et savoir passer de l'une à l'autre rend les deux lisibles.
Ensuite la leçon d'arithmétique : tout prédicat écrit avec un modulo doit être éprouvé sur les
négatifs avant d'être cru — la moitié des « bugs de parité » rencontrés en maintenance viennent
de cette ligne-là.
