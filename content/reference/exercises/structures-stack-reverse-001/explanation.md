# Explication

Inverser une chaîne avec une pile : l'exercice impose l'outil, et cette imposition est le
sujet — la pile n'est pas le chemin le plus court vers l'inversion, c'est le chemin qui fait
*comprendre* pourquoi elle inverse.

Une pile restitue dans l'ordre inverse de l'insertion : dernier entré, premier sorti. Empiler
tous les caractères d'un texte puis les récolter donne donc le texte retourné — l'inversion
n'est pas un algorithme appliqué à la pile, elle est la *nature* de la pile rendue visible. La
solution condense les deux temps en deux expressions : le constructeur `Stack<char>(text)`
empile le texte caractère par caractère, et `ToArray` restitue du sommet vers le fond — l'ordre
de dépilage — si bien que le tableau obtenu est déjà l'inverse, prêt pour `new string`. Il faut
savoir que ce comportement de `ToArray` — sommet d'abord — est documenté et contractuel, sinon
le code semble magique ; l'écrire en boucle explicite — empiler, puis dépiler en construisant —
donne le même résultat en montrant la mécanique, et c'est une variante parfaitement acceptable.

Les clauses du contrat sont discrètes : les espaces sont des caractères comme les autres et
voyagent avec le reste — « sans perdre les espaces », dit l'énoncé, ce qui réfute les inversions
qui découpent en mots — et la chaîne vide traverse en chaîne vide, pile vide comprise. Le `null`
reste une faute d'appel signalée nommément. Les chaînes étant immuables en C#, la non-mutation
de l'entrée est ici gratuite — aucune écriture n'est même possible — et le résultat est
nécessairement une chaîne neuve.

L'honnêteté technique impose la même réserve que pour le palindrome voisin : l'inversion opère
sur des unités UTF-16, pas des graphèmes. Un émoji encodé en paire de substituts ou un accent
combinant sortira cassé d'une inversion caractère par caractère. Pour l'alphabet de l'exercice,
c'est exact ; pour du texte arbitraire, il faudrait inverser des éléments de texte — et savoir
que cette frontière existe est un livrable de l'exercice autant que le code.

Côté coût : deux copies — la pile, puis le tableau — pour un travail linéaire. La version
directe sans pile — recopier le texte de la fin vers le début — ferait une seule allocation ;
elle est préférable en production et *moins* instructive ici, ce qui résume l'arbitrage
pédagogique de l'énoncé.

La transposition de la pile-qui-inverse est réelle : défaire des opérations dans l'ordre
inverse de leur application — annulation, déroulage de transactions, fermeture de ressources
imbriquées — repose exactement sur cette propriété. L'inversion de chaîne en est la
démonstration la plus courte.
