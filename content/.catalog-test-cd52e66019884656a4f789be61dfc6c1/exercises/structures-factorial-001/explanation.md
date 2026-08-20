# Explication

La factorielle est le premier exercice récursif du catalogue, et son titre dit où porte
l'attention : *définir un cas de base*. Une récursion se juge sur trois clauses — où elle
s'arrête, comment elle progresse, et ce qu'elle coûte — et la solution répond aux trois en peu
de lignes.

Le cas de base couvre zéro *et* un, et le zéro est le piège nommé par l'énoncé : la factorielle
de zéro vaut un — c'est le produit vide, neutre de la multiplication — et une implémentation
dont le cas de base commence à un lèverait ou bouclerait sur zéro selon son écriture. Le
`value <= 1` regroupe les deux ancres en une garde. La progression, elle, tient dans
`Factorial(value - 1)` : chaque appel travaille sur une valeur *strictement* plus petite, et
comme le domaine est borné en bas par la validation, la chaîne d'appels atteint le cas de base
en un nombre fini de pas. Ce couple — ancre atteignable, réduction stricte — est à la récursion
ce que l'invariant est à la boucle : la preuve de terminaison, à savoir énoncer avant d'écrire.

La borne haute du domaine est la décision la plus intéressante : douze, parce que la factorielle
de treize dépasse la capacité d'un `int`. Plutôt que de laisser le débordement produire un
nombre faux, le contrat refuse l'entrée *avant* le calcul — la validation encode une propriété
arithmétique du type de retour. Et le `checked` sur la multiplication reste là malgré tout :
c'est la ceinture avec les bretelles, qui transformerait en exception franche toute erreur
future sur la borne — si quelqu'un « élargit » le domaine sans changer le type, le mensonge
silencieux reste impossible. Défense en profondeur sur quatre lignes.

Faut-il la récursion, d'ailleurs ? Une boucle qui accumule le produit ferait le même travail
sans consommer de pile — et sur douze niveaux au maximum, la profondeur est ici négligeable,
bornée par le même douze que le domaine. La récursion est choisie parce qu'elle est le *sujet* :
la factorielle est sa définition mathématique transcrite, et l'exercice apprend à lire une
définition récursive comme un programme. Dans du code réel, la règle pratique est inverse — la
boucle par défaut, la récursion quand la structure du problème est elle-même récursive, comme
les arbres des exercices voisins.

Les cas cachés éprouvent les quatre frontières : zéro rend un, douze passe, treize lève, moins
un lève. Le coût est linéaire en appels et en pile. La transposition : toute définition « f de n
s'exprime par f de n moins un » — cumuls, déroulés, chaînes de délégation — se code par ce
gabarit ancre-réduction-borne, et se prouve par les trois mêmes clauses.
