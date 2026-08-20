# Explication

Cet exercice compose deux opérations connues — dédupliquer, trier — et toute sa valeur est dans
les deux mots du contrat qui gouvernent la composition : *avant*, et *une fois*.

Dédupliquer avant de trier, d'abord. Les deux ordres donnent le même tableau final, et c'est
justement pour cela que le choix doit venir d'un argument, pas du hasard. Le tri est l'opération
chère — son coût croît plus que linéairement — tandis que la déduplication est linéaire en
moyenne. Réduire le volume *avant* l'opération chère : sur un tableau d'un million d'entrées
dont mille valeurs distinctes, trier mille éléments au lieu d'un million n'est pas une nuance.
Ce principe — placer les opérations qui réduisent le volume en amont des opérations coûteuses —
est exactement celui qui gouvernera plus tard l'ordre des clauses dans une requête LINQ traduite
en SQL : filtrer avant de joindre, projeter avant de matérialiser. L'exercice l'installe sur
quatre lignes.

Matérialiser exactement une fois, ensuite. La chaîne `Distinct().OrderBy().ToArray()` reste une
*description* jusqu'au `ToArray` final, qui exécute tout d'un trait et fige le résultat. La
version qui intercale des `ToArray` intermédiaires — dédupliquer, figer, trier, figer — produit
le même contenu avec des allocations en plus et, surtout, elle révèle une incompréhension de
l'exécution différée : chaque matérialisation coupe la chaîne et paie un tableau complet. À
l'inverse, *aucune* matérialisation serait un autre défaut dans ce contrat : retourner
l'énumérable paresseux ferait recalculer la déduplication et le tri à chaque parcours de
l'appelant. Une fois — en fin de chaîne — est le point d'équilibre, et il se généralise : on
matérialise aux frontières, jamais au milieu.

Un mot sur `Distinct` : il conserve la première occurrence de chaque valeur et travaille avec un
ensemble interne, en espace proportionnel au nombre de valeurs distinctes. Ici son ordre de
conservation est indifférent, puisque le tri suit ; le jour où le tri disparaît du contrat,
cet ordre devient observable et documenté.

Les bornes sont celles du régime commun : `null` est une faute d'appel signalée nommément, le
tableau vide traverse et rend un tableau vide neuf, l'entrée n'est jamais modifiée — la chaîne
LINQ ne touche pas sa source, et le harnais le vérifie. Les cas cachés mêlent doublons dispersés,
valeurs négatives et tableau déjà sans doublon, et réfutent la sortie recopiée de l'exemple.

La transposition tient en une question à poser devant toute chaîne de transformations : quelles
étapes réduisent le volume, lesquelles le paient, et où la chaîne se fige-t-elle ? Bien ordonner
la réponse est souvent toute la différence entre un traitement instantané et un traitement qui
« rame », à code par ailleurs identique.
