# Explication

Analyser le document et compter seulement les éléments numériques d'un tableau racine.

Compter par analyse de texte est tentant et faux : une chaîne contenant des chiffres, une clé numérique ou un nombre dans un objet imbriqué seraient tous comptés. Passer par un analyseur donne un type à chaque élément, et c'est ce type — pas la forme du texte — qui décide.

Une racine qui n'est pas un tableau n'est pas une erreur ici : le contrat retourne zéro, ce qui rend la fonction utilisable sur une entrée dont on ne connaît pas la forme. Le document analysé détient des ressources et doit être libéré, y compris lorsqu'un retour anticipé intervient. Le coût est linéaire dans la taille du texte.
