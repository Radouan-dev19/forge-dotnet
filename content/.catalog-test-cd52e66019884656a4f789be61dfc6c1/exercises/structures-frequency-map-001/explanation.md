# Explication

Compter des entiers dans un dictionnaire à clés *textuelles* : la conversion est imposée par la
signature, et c'est elle qui porte la leçon la moins connue de l'exercice — formater un nombre
est une opération culturelle, même quand on ne s'en doute pas.

`value.ToString()` sans argument formate selon la culture du fil d'exécution. Pour des entiers,
le piège est plus étroit que pour les décimaux — pas de séparateur de milliers dans les petites
valeurs — mais il existe : certaines cultures utilisent un *signe moins différent* du tiret
ASCII. Deux machines configurées différemment produiraient alors des clés distinctes pour la
même valeur négative, et un dictionnaire sérialisé ici ne se relirait pas là. La culture
invariante fige le format une fois pour toutes : une clé est la même sur le poste du développeur,
le serveur de production et la machine de test. La règle générale mérite d'être dite : toute
conversion nombre-vers-texte destinée à une *clé*, un fichier ou un protocole se fait en
invariant ; la culture de l'utilisateur se réserve à l'affichage. Le comparateur `Ordinal` du
dictionnaire complète la cohérence — des clés déjà canoniques se comparent binairement.

Le cumul suit le motif à une interrogation : `TryGetValue` lit le compte courant — zéro par
défaut si la clé est neuve — et l'écriture indexée dépose le compte incrémenté. « Incrémenter
une seule entrée », dit l'énoncé : c'est le refus du doublon de clés que produirait une
insertion conditionnelle mal écrite, et le cas caché aux valeurs répétées vérifie que trois
occurrences donnent bien une entrée à trois, pas trois entrées.

Les frontières sont celles du domaine : les valeurs négatives produisent des clés avec leur
signe — `-3` est une clé légitime, distincte de `3`, et le cas caché mixte le vérifie — et le
tableau vide rend un dictionnaire vide, par la boucle qui ne tourne pas. L'entrée n'est jamais
modifiée : un comptage lit.

Le coût est linéaire, une interrogation de table par élément ; l'espace est proportionnel au
nombre de valeurs distinctes. On pourrait discuter la signature elle-même — un dictionnaire
d'entier vers entier éviterait les conversions — mais elle est le contrat : les clés textuelles
sont fréquentes dès qu'un résultat traverse une frontière JSON ou un journal, et l'exercice
entraîne exactement ce passage.

La transposition tient en une checklist de frontière : qui lira ces clés, sur quelle machine,
dans quel format ? Invariant pour les machines, culturel pour les humains — et jamais l'inverse.
Le jour où deux environnements « comptent différemment la même donnée », c'est presque toujours
cette ligne de conversion qu'il faut aller relire.
