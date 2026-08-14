# Explication

La notation postfixée — les opérandes d'abord, l'opérateur ensuite — a une propriété qui
explique sa longévité dans les machines virtuelles et les calculatrices : elle s'évalue sans
parenthèses, sans priorités et sans regarder en avant, avec une pile pour toute mémoire.
L'exercice fait construire cet évaluateur minimal, et sa seule vraie chausse-trape tient en deux
lignes.

Le principe d'abord : chaque nombre rencontré s'empile ; chaque opérateur consomme les *deux
derniers* nombres en attente, calcule, et rempile le résultat, qui redevient un opérande comme
un autre. Cette uniformité — le résultat intermédiaire n'a aucun statut spécial — est ce qui
rend l'évaluation composable : `2 3 + 4 *` déroule vingt sans qu'aucune priorité n'ait été
consultée, parce que l'ordre des tokens *est* l'ordre d'évaluation. Comprendre cela, c'est
comprendre pourquoi les compilateurs traduisent les expressions infixes vers cette forme avant
de les exécuter.

La chausse-trape maintenant : l'ordre du dépilage. Le sommet de la pile est le dernier nombre
empilé, donc l'opérande *droit* — la solution dépile `right` puis `left`, dans cet ordre nommé.
Avec l'addition et la multiplication, opérations commutatives, l'inversion serait invisible ;
c'est exactement pour cela que le contrat le fixe et que la solution le commente — le jour où la
soustraction entre au catalogue, `left - right` et `right - left` divergent, et l'évaluateur
écrit avec les bons noms survit au changement quand l'autre produit des signes inversés. Écrire
le code d'aujourd'hui avec les distinctions dont demain aura besoin, même quand elles sont
encore indifférentes : c'est une forme d'honnêteté du nommage que l'exercice entraîne.

Le contrat assume ses limites, et les nommer fait partie de la leçon : deux opérateurs
seulement, des expressions bien formées — pas de garde contre la pile vide ni les tokens
inconnus, un opérateur non reconnu tombant dans la branche de la multiplication. C'est un choix
pédagogique annoncé dans l'énoncé, pas un oubli : l'évaluateur robuste — validation des tokens,
arité vérifiée, message d'erreur positionné — est l'étape suivante naturelle, et savoir dire ce
qui manque vaut autant que savoir l'écrire. Le `TryParse` sert déjà de trieur nombre-contre-
opérateur, la forme sans exception du discernement de tokens ; les nombres négatifs en
bénéficient au passage, `-3` étant un token analysable.

Le coût est linéaire : chaque token est traité une fois, la pile monte et descend au rythme des
opérandes en attente. La transposition dépasse l'arithmétique : files d'instructions, moteurs de
règles, interpréteurs de filtres — partout où une expression doit s'évaluer sans analyseur
syntaxique complet, la forme postfixée et sa pile restent l'outil le plus simple qui marche.
