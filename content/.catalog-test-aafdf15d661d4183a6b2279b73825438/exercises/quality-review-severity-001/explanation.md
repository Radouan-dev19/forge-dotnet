# Explication

Classer une remarque de revue de code : sécurité, correction, ou suggestion. L'arbre tient en
deux questions ordonnées, et l'énoncé demande ce que des remarques *non hiérarchisées* font
perdre à l'auteur — c'est la question de culture d'équipe que cette petite fonction
matérialise.

La réponse d'abord : sans hiérarchie, l'auteur reçoit vingt remarques du même poids — un
renommage suggéré à côté d'une faille d'injection — et il fait ce que tout humain fait devant
une liste plate : il traite dans l'ordre, ou au plus facile. La faille attend derrière les
renommages, la revue se rejoue trois fois, et la frustration monte des deux côtés. La
hiérarchie explicite change le contrat : un bloqueur de sécurité arrête tout, un bloqueur de
correction empêche la fusion, une suggestion est un cadeau que l'auteur peut décliner. Chaque
niveau porte sa conséquence — c'est la conséquence, pas l'étiquette, qui fait la valeur du
classement.

L'ordre des deux questions encode la doctrine. La sécurité prime : un défaut qui expose des
données ou des droits passe devant tout, y compris devant un défaut de correction — d'où la
première garde, et le verdict distinct `security-blocker` plutôt qu'un simple bloqueur : la
remarque de sécurité déclenche d'autres réflexes — un second relecteur, une vérification
d'historique, parfois une rotation de secrets — et son étiquette doit la rendre filtrable. Le
cas où les deux indicateurs sont vrais est le verrou de cette priorité : sécurité gagne, et le
cas caché posé dessus le fige. La correction vient ensuite — le code fait-il ce qu'il
prétend ? — et tout le reste est suggestion : style, préférence, idée d'amélioration — utile,
jamais bloquant.

Ce classement à trois niveaux a une vertu discrète : il protège aussi le *relecteur*. Étiqueter
« suggestion » ce qui n'est qu'un goût personnel oblige à distinguer son goût d'un défaut — et
les revues où tout est bloquant sont celles où le relecteur n'a pas fait ce tri. La sévérité
est un engagement du relecteur autant qu'une information pour l'auteur.

Le domaine d'entrée est fini — quatre combinaisons, énumérées avant de coder comme l'énoncé
l'exige — et les cas les couvrent toutes. Le coût est constant.

La transposition est le vocabulaire de revue lui-même, à installer dans une équipe : trois
niveaux nommés, une conséquence par niveau, la sécurité toujours en tête. Et la version
personnelle du réflexe : en écrivant une remarque, se demander « quelle étiquette, donc quelle
conséquence ? » avant de la poster. Une revue dont chaque remarque porte sa sévérité se traite
en une passe ; c'est l'un des accélérateurs d'équipe les moins coûteux qui existent.
